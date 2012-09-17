#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using OptiTrackNET;

using VVVV.Nodes.OpenCV;

#endregion usings

namespace VVVV.Nodes.OptiTrack
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "OptiTrack", Help = "List OptiTrack camera devices", Tags = "")]
	#endregion PluginInfo
	public class CameraListNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInRefresh;

		[Output("Device")]
		ISpread<Camera> FPinOutCameras;

		[Import]
		ILogger FLogger;

		bool FFirstRun = true;
		OptiTrackNET.Context FContext = new OptiTrackNET.Context();

		#endregion fields & pins

		[ImportingConstructor]
		public CameraListNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInRefresh[0] || FFirstRun)
			{
				Refresh();
				FFirstRun = false;
			}
		}

		void Refresh()
		{
			var Cameras = Context.Cameras;
			FPinOutCameras.SliceCount = 0;

			foreach (var Camera in Cameras)
			{
				FPinOutCameras.Add(Camera);
			}
		}
	}
}
