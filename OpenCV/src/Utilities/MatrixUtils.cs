﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System.Drawing;
using Emgu.CV.Structure;

namespace VVVV.Nodes.OpenCV
{
	public class MatrixUtils
	{
		public static PointF[] ImagePoints(ISpread<Vector2D> input)
		{
			PointF[] imagePoints = new PointF[input.SliceCount];

			for (int i = 0; i < input.SliceCount; i++)
			{
				imagePoints[i].X = (float)input[i].x;
				imagePoints[i].Y = (float)input[i].y;
			}

			return imagePoints;
		}

		public static MCvPoint3D32f[] ObjectPoints(ISpread<Vector3D> input)
		{
			MCvPoint3D32f[] objectPoints = new MCvPoint3D32f[input.SliceCount];

			for (int i = 0; i < input.SliceCount; i++)
			{
				objectPoints[i].x = (float)input[i].x;
				objectPoints[i].y = (float)input[i].y;
				objectPoints[i].z = (float)input[i].z;
			}

			return objectPoints;
		}
	}
}
