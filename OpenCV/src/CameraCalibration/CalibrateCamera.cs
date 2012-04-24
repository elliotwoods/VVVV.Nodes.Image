#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenCV
{
	#region PluginInfo
	[PluginInfo(Name = "CalibrateCamera", Category = "OpenCV", Help = "Finds intrinsics for a single camera", Tags = "")]
	#endregion PluginInfo
	public class CalibrateCameraNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Object Points")]
		ISpread<Vector3D> FPinInObject;

		[Input("Image Points")]
		ISpread<Vector2D> FPinInImage;

		[Input("Resolution", IsSingle=true)]
		ISpread<Vector2D> FPinInSensorSize;

		[Input("CV_CALIB_USE_INTRINSIC_GUESS", IsSingle = true)]
		ISpread<bool> FPinInIntrinsicGuess;

		[Input("CV_CALIB_FIX_ASPECT_RATIO", IsSingle = true)]
		ISpread<bool> FPinInFixAspectRatio;

		[Input("CV_CALIB_FIX_PRINCIPAL_POINT", IsSingle = true)]
		ISpread<bool> FPinInFixPincipalPoint;

		[Input("CV_CALIB_ZERO_TANGENT_DIST", IsSingle = true)]
		ISpread<bool> FPinInZeroTangent;

		[Input("CV_CALIB_FIX_FOCAL_LENGTH", IsSingle = true)]
		ISpread<bool> FPinInFixFocalLength;

		[Input("CV_CALIB_FIX_KI", IsSingle = true)]
		ISpread<bool> FPinInFixDistortion;

		[Input("CV_CALIB_RATIONAL_MODEL", IsSingle = true)]
		ISpread<bool> FPinInRationalModel;

		[Input("Do", IsBang=true, IsSingle=true)]
		ISpread<bool> FPinInDo;

		[Output("Intrinsics")]
		ISpread<Intrinsics> FPinOutIntrinsics;

		[Output("Extrinsics Per Board")]
		ISpread<Extrinsics> FPinOutExtrinsics;

		[Output("View per board")]
		ISpread<Matrix4x4> FPinOutView;

		[Output("Projection")]
		ISpread<Matrix4x4> FPinOutProjection;

		[Output("Reprojection Error")]
		ISpread<Double> FPinOutError;

		[Output("Success")]
		ISpread<bool> FPinOutSuccess;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public CalibrateCameraNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInDo[0])
			{
				int nPointsPerImage = FPinInObject.SliceCount;
				if (nPointsPerImage == 0)
				{
					FStatus[0] = "Insufficient points";
					return;
				}
				int nImages = FPinInImage.SliceCount / nPointsPerImage;

				MCvPoint3D32f[][] objectPoints = new MCvPoint3D32f[nImages][];
				PointF[][] imagePoints = new PointF[nImages][];
				Size imageSize = new Size( (int) FPinInSensorSize[0].x, (int) FPinInSensorSize[0].y);
				CALIB_TYPE flags = new CALIB_TYPE();
				IntrinsicCameraParameters intrinsicParam = new IntrinsicCameraParameters();
				ExtrinsicCameraParameters[] extrinsicsPerView;
				GetFlags(out flags);

				if (FPinInIntrinsicGuess[0])
				{
					Matrix<double> mat = intrinsicParam.IntrinsicMatrix;
					mat[0, 0] = FPinInSensorSize[0].x;
					mat[1, 1] = FPinInSensorSize[0].y;
					mat[0, 2] = FPinInSensorSize[0].x / 2.0d;
					mat[1, 2] = FPinInSensorSize[0].y / 2.0d;
					mat[2, 2] = 1;

				}

				imagePoints = MatrixUtils.ImagePoints(FPinInImage, nPointsPerImage);

				for (int i=0; i<nImages; i++)
				{
					objectPoints[i] = new MCvPoint3D32f[nPointsPerImage];

					for (int j=0; j<nPointsPerImage; j++)
					{
						objectPoints[i] = MatrixUtils.ObjectPoints(FPinInObject);
					}
				}

				try
				{
					FPinOutError[0] = CameraCalibration.CalibrateCamera(objectPoints, imagePoints, imageSize, intrinsicParam, flags, out extrinsicsPerView);

					Intrinsics intrinsics = new Intrinsics(intrinsicParam);
					FPinOutIntrinsics[0] = intrinsics;
					FPinOutProjection[0] = intrinsics.Matrix;

					FPinOutExtrinsics.SliceCount = nImages;
					FPinOutView.SliceCount = nImages;
					for (int i = 0; i < nImages; i++)
					{
						Extrinsics extrinsics = new Extrinsics(extrinsicsPerView[i]);
						FPinOutExtrinsics[i] = extrinsics;
						FPinOutView[i] = extrinsics.Matrix;
					}

					FPinOutSuccess[0] = true;
					FStatus[0] = "OK";
				}
				catch (Exception e)  {
					FPinOutSuccess[0] = false;
					FStatus[0] = e.Message;
				}
			}

		}

		private void GetFlags(out CALIB_TYPE flags)
		{
			flags = 0;
			if (FPinInIntrinsicGuess[0])
				flags |= CALIB_TYPE.CV_CALIB_USE_INTRINSIC_GUESS;

			if (FPinInFixAspectRatio[0])
				flags |= CALIB_TYPE.CV_CALIB_FIX_ASPECT_RATIO;

			if (FPinInFixPincipalPoint[0])
				flags |= CALIB_TYPE.CV_CALIB_FIX_PRINCIPAL_POINT;

			if (FPinInZeroTangent[0])
				flags |= CALIB_TYPE.CV_CALIB_ZERO_TANGENT_DIST;
 
			if (FPinInFixFocalLength[0])
				flags |= CALIB_TYPE.CV_CALIB_FIX_FOCAL_LENGTH;
 
			if (FPinInFixDistortion[0])
				flags |=  (CALIB_TYPE.CV_CALIB_FIX_K1 | CALIB_TYPE.CV_CALIB_FIX_K2 | CALIB_TYPE.CV_CALIB_FIX_K3 | CALIB_TYPE.CV_CALIB_FIX_K4 | CALIB_TYPE.CV_CALIB_FIX_K5 | CALIB_TYPE.CV_CALIB_FIX_K6);

			if (FPinInRationalModel[0])
				flags |= CALIB_TYPE.CV_CALIB_RATIONAL_MODEL;
		}
	}
}
