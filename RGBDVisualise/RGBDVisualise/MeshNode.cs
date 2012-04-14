#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using VVVV.Core.Logging;
using System.Collections.Generic;

using SlimDX.Direct3D9;
using SlimDX;

#endregion usings

namespace VVVV.Nodes.RGBDToolkit
{
	//NOTES : CODE TAKEN FROM VUX'S ASSIMP LOADER

	#region PluginInfo
	[PluginInfo(Name = "Mesh", Category = "RGBDToolkit", Help = "Create a mesh from an RGBDToolkit.Frame", Tags = "")]
	#endregion PluginInfo
	public class MeshNode : IPluginEvaluate, IPluginDXMesh, IDisposable
	{
		#region fields & pins
		[Input("Frame")]
		IDiffSpread<Frame> FInput;

		[Output("Status")]
		ISpread<string> FStatus;

		private IDXMeshOut FOutMesh;
		private Dictionary<Device, Mesh> FMeshes = new Dictionary<Device, Mesh>();
		bool FInvalidate = false;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		[ImportingConstructor()]
		public MeshNode(IPluginHost host)
        {
            host.CreateMeshOutput("Mesh", TSliceMode.Dynamic, TPinVisibility.True, out this.FOutMesh);
            this.FOutMesh.Order = 0;
        }

		Frame FFrame;

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInput.IsChanged)
			{
				if (FInput[0] == null)
				{
					FOutMesh.SliceCount = 0;
				}
				else
				{
					FOutMesh.SliceCount = SpreadMax;
				}
				FInvalidate = true;
			}
		}

		public void Dispose()
		{
			foreach (Device device in FMeshes.Keys)
				FMeshes[device].Dispose();
			FMeshes.Clear();
		}

		public Mesh GetMesh(IDXMeshOut ForPin, Device OnDevice)
		{
			if (this.FMeshes.ContainsKey(OnDevice))
			{
				return this.FMeshes[OnDevice];
			}
			else
			{
				return null;
			}
		}

		public void UpdateResource(IPluginOut ForPin, Device OnDevice)
		{
			if (this.FInvalidate || !this.FMeshes.ContainsKey(OnDevice))
			{
				//Destroy old mesh
				DestroyResource(ForPin, OnDevice, false);

				if (this.FInput[0] == null) { return; }

				List<Mesh> meshes = new List<Mesh>();

				Frame frame = FInput[0];
				if (frame.Loaded)
				{
					Mesh mesh = new Mesh(OnDevice, frame.Indices.Length / 3, frame.Vertices.Length, MeshFlags.Dynamic | MeshFlags.Use32Bit, Frame.VertexDeclaration);
				
					DataStream vS = mesh.LockVertexBuffer(LockFlags.Discard);
					DataStream iS = mesh.LockIndexBuffer(LockFlags.Discard);

					vS.WriteRange(frame.FormattedVertices);
					iS.WriteRange(frame.Indices);

					mesh.UnlockIndexBuffer();
					mesh.UnlockVertexBuffer();

					this.FMeshes.Add(OnDevice, mesh);
				}
			}   
		}

		public void DestroyResource(IPluginOut ForPin, Device OnDevice, bool OnlyUnManaged)
		{
			if (this.FMeshes.ContainsKey(OnDevice))
			{
				this.FMeshes[OnDevice].Dispose();
				this.FMeshes.Remove(OnDevice);
			}         
		}
	}
}
