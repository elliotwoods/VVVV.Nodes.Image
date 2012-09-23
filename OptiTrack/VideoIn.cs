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

using NPCameraSDKDotNet;

using VVVV.Nodes.OpenCV;

#endregion usings

namespace VVVV.Nodes.OptiTrack
{
	public class VideoInInstance : IGeneratorInstance
	{
		public VideoInInstance()
		{
			this.FrameID = -1;
			this.Objects = new List<MObject>();
		}

		MCamera FCamera;
		public MCamera Camera
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

				//if no camera is given, try and find one
				if (FCamera == null)
				{
					FCamera = MCameraManager.GetCamera();
				}

				//if after all this we do have a working camera, initialise it!
				if (FCamera != null)
				{
					if (needsRestart)
						this.Start();
				}
			}
		}

		VideoMode FMode;
		public VideoMode Mode
		{
			set
			{
				FMode = value;
				this.Restart();
			}
		}

		bool FGetLatestFrame;
		public bool GetLatestFrame
		{
			set
			{
				this.FGetLatestFrame = value;
			}
		}

		public CaptureProperty CaptureProperty
		{
			set
			{
				try
				{
					FCamera.SetExposure(value.Exposure);
					FCamera.SetAEC(value.AEC);
					FCamera.SetAGC(value.AGC);
					FCamera.SetFrameRate(value.Framerate);
					FCamera.SetIntensity(value.Intensity);
					FCamera.SetLED(value.StatusLEDs, value.StatusLEDsEnabled);
					FCamera.SetThreshold(value.Threshold);
				}
				catch (Exception e)
				{
					this.Status = e.Message;
				}
			}
		}

		protected override bool Open()
		{
			try
			{
				if (FCamera == null)
					throw (new Exception("Cannot open camera, no device attached"));

				FCamera.SetVideoType(this.FMode);
				FCamera.Start();
				FCamera.FrameAvailable += FCamera_FrameAvailable;
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
			FCamera.FrameAvailable -= FCamera_FrameAvailable;
			FCamera.Stop(true);
		}

		void FCamera_FrameAvailable(object sender, EventArgs e)
		{
			this.Generate();
		}

		public override void Allocate()
		{
			try
			{
				if (!FCamera.IsValid())
					throw(new Exception("Camera not ready"));
				FOutput.Image.Initialise(new System.Drawing.Size((int)FCamera.Width(), (int)FCamera.Height()), TColorFormat.L8);
			}
			catch(Exception e)
			{
				this.ReAllocate(); // try again next frame
			}
		}

		public int FrameID { get; private set; }

		protected override void Generate()
		{
			var frame =  this.FGetLatestFrame ? FCamera.GetLatestFrame() : FCamera.GetFrame();
			if (frame != null)
			{
				if (frame.FrameID() != this.FrameID)
				{
					frame.CopyGrayScaleDataTo(FOutput.Data);
					this.FrameID = frame.FrameID();
					FOutput.Send();

					foreach (var Object in Objects)
					{
						Object.Dispose();
					}
					this.Objects.Clear();
					for (int i = 0; i < frame.ObjectCount(); i++)
					{
						this.Objects.Add(frame.Object(i).Clone());
					}
				}
				frame.Release();
			}
		}

		public override bool NeedsThread()
		{
			return true;
		}

		public List<MObject> Objects { get; private set; }
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "OptiTrack", Help = "Capture frames from camera device", Tags = "")]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		#region fields & pins

		[Input("Device")]
		IDiffSpread<MCamera> FInCamera;

		[Input("Mode")]
		IDiffSpread<VideoMode> FInMode;

		[Input("Get Latest Frame")]
		IDiffSpread<bool> FInGetLatestFrame;

		[Input("Capture Properties")]
		IDiffSpread<CaptureProperty> FInCaptureProperty;

		[Output("Frame ID")]
		ISpread<int> FOutFrameID;

		[Output("Objects")]
		ISpread<ISpread<MObject>> FOutObjects;

		#endregion

		[ImportingConstructor]
		public VideoInNode()
		{
			if (MCameraManager.AreCamerasInitialized())
			{
				MCameraManager.EnableDevelopment();
				MCameraManager.WaitForInitialization();
			}
		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool changed)
		{
			if (FInCamera.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Camera = FInCamera[i];

			if (FInMode.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Mode = FInMode[i];

			if (FInGetLatestFrame.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].GetLatestFrame = FInGetLatestFrame[i];

			if (FInCaptureProperty.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].CaptureProperty = FInCaptureProperty[i];

			FOutFrameID.SliceCount = InstanceCount;
			FOutObjects.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FOutFrameID[i] = FProcessor[i].FrameID;

				FOutObjects[i].SliceCount = FProcessor[i].Objects.Count;
				for (int j = 0; j < FProcessor[i].Objects.Count; j++)
				{
					FOutObjects[i][j] = FProcessor[i].Objects[j];
				}
			}
		}
	}
}