using System;
using System.ComponentModel.Composition;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;


using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using VVVV.Nodes.OpenCV;
using System.Runtime.InteropServices;

using Gst;

namespace VVVV.Nodes.OpenCV.GStreamer
{
	public class VideoPlayerInstance : IGeneratorInstance
	{
		string FFilename = "http://www.xiph.org/vorbis/listen/compilation-ogg-q4.ogg";
		public string Filename
		{
			set
			{
				FFilename = value;
				ReInitialise();
			}
		}

		Element FElement;

		public VideoPlayerInstance()
		{
			
		}

		protected override bool Open()
		{
			Close();

			try
			{
				Application.Init();
				//FElement = Gst.ElementFactory.MakeFromUri(URIType.Src, FFilename, "player");
				//FElement.SetState(State.Playing);
				return true;
			}
			catch (Exception e)
			{
				Status = e.Message;
				return false;
			}
		}

        protected override void Close()
		{
			try
			{
				Status = "Closed";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoPlayer", Category = "OpenCV", Version = "GStreamer", Help = "Plays local or remote video using GStreamer", Author = "Elliot Woods", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoPlayerNode : IGeneratorNode<VideoPlayerInstance>
	{
		#region fields & pins
		[Input("Filename", StringType=StringType.URL, DefaultString="http://www.xiph.org/vorbis/listen/compilation-ogg-q4.ogg")]
		IDiffSpread<string> FPinInFilename;
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public VideoPlayerNode(IPluginHost host)
		{
		
		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInFilename.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Filename = FPinInFilename[i];

		}
	}
}
