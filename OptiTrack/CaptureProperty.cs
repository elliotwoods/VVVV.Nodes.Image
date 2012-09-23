using NPCameraSDKDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OptiTrack
{
	public struct CaptureProperty
	{
		public CaptureProperty(bool AEC, int Exposure, bool AGC, int Framerate, int Intensity, StatusLEDs StatusLEDs, bool StatusLEDsEnabled, int Threshold)
		{
			this.AEC = AEC;
			this.Exposure = Exposure;
			this.AGC = AGC;
			this.Framerate = Framerate;
			this.Intensity = Intensity;
			this.StatusLEDs = StatusLEDs;
			this.StatusLEDsEnabled = StatusLEDsEnabled;
			this.Threshold = Threshold;
		}
		
		public bool AEC;
		public int Exposure;
		public bool AGC;

		public int Framerate;
		public int Intensity;
		public StatusLEDs StatusLEDs;
		public bool StatusLEDsEnabled;
		public int Threshold;
	}

	#region PluginInfo
	[PluginInfo(Name = "CaptureProperty",
				Category = "OptiTrack",
				Version = "",
				Help = "Set camera and tracking properties",
				Tags = "")]
	#endregion PluginInfo
	public class CapturePropertyNode : IPluginEvaluate
	{
		[Input("Auto Exposure")]
		IDiffSpread<bool> FInAEC;

		[Input("Exposure", DefaultValue=50)]
		IDiffSpread<int> FInExposure;

		[Input("Auto Gain")]
		IDiffSpread<bool> FInAGC;

		[Input("Framerate", DefaultValue = 120)]
		IDiffSpread<int> FInFramerate;

		[Input("Intensity", DefaultValue = 15)]
		IDiffSpread<int> FInIntensity;

		[Input("Status LEDs")]
		IDiffSpread<StatusLEDs> FInStatusLEDs;

		[Input("Status LEDs Enabled")]
		IDiffSpread<bool> FInStatusLEDsEnabled;

		[Input("Threshold", DefaultValue = 200)]
		IDiffSpread<int> FInThreshold;

		[Output("Capture Properties")]
		ISpread<CaptureProperty> FOutCaptureProperties;

		public void Evaluate(int SpreadMax)
		{
			if (FInAEC.IsChanged || FInExposure.IsChanged || FInAGC.IsChanged ||FInFramerate.IsChanged || FInIntensity.IsChanged || FInStatusLEDs.IsChanged || FInStatusLEDsEnabled.IsChanged || FInThreshold.IsChanged)
			{
				FOutCaptureProperties.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					FOutCaptureProperties[i] = new CaptureProperty(FInAEC[i], FInExposure[i], FInAGC[i], FInFramerate[i], FInIntensity[i], FInStatusLEDs[i], FInStatusLEDsEnabled[i], FInThreshold[i]);
				}
			}
		}
	}
}
