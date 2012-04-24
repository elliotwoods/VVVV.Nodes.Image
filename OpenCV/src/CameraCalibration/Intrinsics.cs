using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.Utils.VMath;
using Emgu.CV;

namespace VVVV.Nodes.OpenCV
{
	public class Intrinsics
	{
		public IntrinsicCameraParameters intrinsics;

		public Intrinsics(IntrinsicCameraParameters intrinsics)
		{
			this.intrinsics = intrinsics;
		}

		public Matrix4x4 Matrix
		{
			get
			{
				Matrix4x4 m = new Matrix4x4();

				m[0, 0] = intrinsics.IntrinsicMatrix[0, 0];
				m[1, 1] = intrinsics.IntrinsicMatrix[1, 1];
				m[2, 0] = intrinsics.IntrinsicMatrix[0, 2];
				m[2, 1] = intrinsics.IntrinsicMatrix[1, 2];
				m[2, 2] = 1;
				m[2, 3] = 1;
				m[3, 3] = 0;

				return m;
			}
		}
	}
}
