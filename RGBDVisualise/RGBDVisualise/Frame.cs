#region usings
using System;
using System.IO;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;

using SlimDX;
using SlimDX.Direct3D9;
using System.Collections.Generic;
using VVVV.Utils.SlimDX;
#endregion usings

namespace VVVV.Nodes.RGBDToolkit
{
	class Frame : IDisposable
	{
		public int[] Indices { get; private set; }
		public Vector3[] Vertices { get; private set; }
		public Vector2[] TexCoords { get; private set; }
		public string Status;
		public bool Loaded;

		public Frame(string filename)
		{
			Load(filename);
		}

		unsafe void Load(string filename)
		{
			try
			{
				byte[] buffer = File.ReadAllBytes(filename);

				if (buffer.Length == 0)
					throw (new Exception("File load failed for " + filename));

				fixed (byte* rawFixed = &buffer[0])
				{
					byte* raw = rawFixed;

					int indicesCount = *(int*)raw;
					raw += sizeof(int);
					Indices = new int[indicesCount];
					for (int i = 0; i < indicesCount; i++)
					{
						Indices[i] = *(int*)raw;
						raw += sizeof(int);
					}

					int verticesCount = *(int*)raw;
					raw += sizeof(int);
					Vertices = new Vector3[verticesCount];
					for (int i = 0; i < verticesCount; i++)
					{
						Vertices[i] = *(Vector3*)raw;
						raw += sizeof(Vector3);
					}

					int texCoordsCount = *(int*)raw;
					raw += sizeof(int);
					TexCoords = new Vector2[texCoordsCount];
					for (int i = 0; i < texCoordsCount; i++)
					{
						TexCoords[i] = *(Vector2*)raw;
						raw += sizeof(Vector2);
					}
				}

				FormatVertices();
				FindNormals();

				Status = "OK";
				Loaded = true;
			}
			catch (Exception e)
			{
				Status = e.Message;
				Loaded = false;
			}
		}

		public TexturedNormalVertex[] FormattedVertices;
		
		public static VertexElement[] VertexDeclaration
		{
			get
			{
				return TexturedNormalVertex.VertexElements;
			}
		}

		private void FormatVertices()
		{
			FormattedVertices = new TexturedNormalVertex[this.Vertices.Length];

			for (int i = 0; i < this.Vertices.Length; i++)
			{
				FormattedVertices[i].Position = Vertices[i];
				FormattedVertices[i].TextureCoordinate = TexCoords[i];
			}
		}

		private void FindNormals()
		{
			for (int face = 0; face < Indices.Length; face+=3)
			{
				Vector3 v0 = Vertices[Indices[face + 0]];
				Vector3 v1 = Vertices[Indices[face + 1]];
				Vector3 v2 = Vertices[Indices[face + 2]];

				Vector3 normal = CrossProduct(v2 - v0, v1 - v0);
				normal.Normalize();

				FormattedVertices[Indices[face + 0]].Normal += normal;
				FormattedVertices[Indices[face + 1]].Normal += normal;
				FormattedVertices[Indices[face + 2]].Normal += normal;
			}

			foreach (var vertex in FormattedVertices)
				vertex.Normal.Normalize();
		}

		private Vector3 CrossProduct(Vector3 vec1, Vector3 vec2)
		{
			return new Vector3((vec1.Y * vec2.Z) - (vec2.Y * vec1.Z),
					   (vec1.Z * vec2.X) - (vec2.Z * vec1.X),
					   (vec1.X * vec2.Y) - (vec2.X * vec1.Y));
		}

		public void Dispose()
		{
			Indices = null;
			Vertices = null;
			FormattedVertices = null;
		}
	}
}