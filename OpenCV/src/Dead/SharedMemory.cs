#region usings

using System;
using System.ComponentModel.Composition;
using Emgu.CV.CvEnum;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using VVVV.Utils.SharedMemory;

#endregion usings

namespace VVVV.Nodes.OpenCV
{
	public class SharedMemoryInstance : IGeneratorInstance
	{
		Size FSize = new Size();
		TColourFormat FFormat = TColourFormat.UnInitialised;
		string FName = "#sharedimage";
		Segment FSegment;

		public int Width
		{
			set
			{
				if (value > 1 && value < 4096)
				{
					FSize.Width = value;
					FNeedsInit = true;
				}
			}
		}

		public int Height
		{
			set
			{
				if (value > 1 && value < 4096)
				{
					FSize.Height = value;
					FNeedsInit = true;
				}
			}
		}

		public string Name
		{
			set
			{
				FName = value;
				FNeedsInit = true;
			}
		}

		public TColourFormat Format
		{
			set
			{
				FFormat = value;
				FNeedsInit = true;
			}
		}

		int FDataLength = 0;

		public override void  Initialise()
		{
			if (FFormat == TColourFormat.UnInitialised)
				return;

			FDataLength = FSize.Width * FSize.Height * (int)ImageUtils.BytesPerPixel(FFormat);
			FOutput.Image.Initialise(FSize, FFormat);
			FSegment = new Segment(FName, SharedMemoryCreationFlag.Attach, FDataLength);
		}

		protected override void  Generate()
		{
			if (FFormat == TColourFormat.UnInitialised)
				return;
			FSegment.Lock();
			FOutput.Image.SetPixels(FSegment.GetNativePointer());
			FOutput.Send();
			FSegment.Unlock();
		}

        protected override void Close()
        { 
            
        }

        protected override void Open()
        {
            
        }
	}

	#region PluginInfo
	[PluginInfo(Name = "SharedMemory",
			  Category = "Image",
			  Version = "",
			  Help = "Receives a bock of shared memory as an image",
			  Tags = "")]
	#endregion PluginInfo
	public class SharedMemoryNode : IGeneratorNode<SharedMemoryInstance>
	{
		#region fields & pins

		[Input("Shared Memory Name", DefaultString="#sharedimage")]
		IDiffSpread<string> FPinInName;

		[Input("Width", MinValue=1, MaxValue=4096)]
		IDiffSpread<int> FPinInWidth;

		[Input("Height", MinValue=1, MaxValue=4096)]
		IDiffSpread<int> FPinInHeight;

		[Input("Format")]
		IDiffSpread<TColourFormat> FPinInFormat;

		[Import]
		ILogger FLogger;
		#endregion fields & pins

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInName.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Name = FPinInName[i];
			if (FPinInWidth.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Width = FPinInWidth[i];
			if (FPinInHeight.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Height = FPinInHeight[i];
			if (FPinInFormat.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Format = FPinInFormat[i];
		}
	}
}
