#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using System.Collections.Generic;

using OptiTrackNET;

using VVVV.Nodes.OpenCV;

#endregion usings

namespace VVVV.Nodes.OptiTrack
{
	public class VideoInInstance : IGeneratorInstance
	{
		Camera FCamera;
		public Camera Camera
		{
			set
			{
				if (value == FCamera)
					return;

				bool needsRestart = false;

				if (FCamera != null)
				{
					FCamera.Stop(true);
					needsRestart = true;
				}

				FCamera = value;

				if (needsRestart)
					this.Start();
			}
		}

		protected override bool Open()
		{
			try
			{
				if (FCamera == null)
					throw (new Exception("Cannot open camera, no device attached"));

				FCamera.Start();
				return true;
			}
			catch (Exception e)
			{
				this.Status = e.Message;
				return false;
			}
		}

		protected override void Close()
		{
			FCamera.Stop(true);
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FCamera.Size, TColorFormat.L8);
		}

		protected override void Generate()
		{
			return;
			var frame = FCamera.GetFrame();
			frame.CopyGrayscaleTo(FOutput.Data);
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "OptiTrack", Help = "Capture frames from camera device", Tags = "")]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		#region fields & pins

		[Input("Device")]
		IDiffSpread<Camera> FInCamera;

		#endregion

		[ImportingConstructor]
		public VideoInNode()
		{

		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool changed)
		{
			if (FInCamera.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Camera = FInCamera[i];
		}
	}
}