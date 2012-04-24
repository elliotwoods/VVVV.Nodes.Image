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

using OpenNI;
using System.Runtime.InteropServices;

using VVVV.Nodes.OpenCV;

#endregion usings

namespace VVVV.Nodes.OpenCV.OpenNI
{
	enum ImageNodeMode { RGB, IR };

	#region PluginInfo
	[PluginInfo(Name = "Images", Category = "OpenCV", Version = "OpenNI", Help = "OpenNI Image generator", Tags = "")]
	#endregion PluginInfo
	public class ImageNode : IPluginEvaluate, IDisposable
	{
        class ImageInstance
        {
            public string Status = "";

            public bool EnableWorld = true;
            public bool EnableRGB = true;

            private OpenNIState FState;
            public OpenNIState State
            {
                set
                {
                    if (FState == value)
                        return;
                    FState = value;
                    FState.Initialised += new EventHandler(FState_Initialised);
                    Initialise();
                }
            }

            void FState_Initialised(object sender, EventArgs e)
            {
                Initialise();
            }

            ImageGenerator FImageGenerator;
            IRGenerator FIRGenerator;

            public CVImageOutput Image = new CVImageOutput();
            public CVImageOutput Depth = new CVImageOutput();
            public CVImageOutput World = new CVImageOutput();

            public ImageNodeMode Mode = ImageNodeMode.RGB;

            Point3D[] FProjective = new Point3D[640 * 480];

            private void Initialise()
            {
                try
                {
                    Size size = new Size(640, 480);
                    string messages = "";

                    if (Mode == ImageNodeMode.RGB)
                    {
                        FImageGenerator = new ImageGenerator(FState.Context);
                        MapOutputMode imageMode = new MapOutputMode();
                        imageMode.XRes = 640;
                        imageMode.YRes = 480;
                        imageMode.FPS = 30;
                        FImageGenerator.MapOutputMode = imageMode;
                        Image.Image.Initialise(size, TColorFormat.RGB8);

                        if (FState.DepthGenerator.AlternativeViewpointCapability.IsViewpointSupported(FImageGenerator))
                        {
                            FState.DepthGenerator.AlternativeViewpointCapability.SetViewpoint(FImageGenerator);
                        }
                        else
                        {
                            messages += "AlternativeViewportCapability not supported\n";
                        }
                        FImageGenerator.StartGenerating();
                    }
                    else
                    {
                        FIRGenerator = new IRGenerator(FState.Context);
                        Image.Image.Initialise(size, TColorFormat.L16);
                        FIRGenerator.StartGenerating();
                    }

                    Depth.Image.Initialise(size, TColorFormat.L16);
                    World.Image.Initialise(size, TColorFormat.RGB32F);

                    for (int x = 0; x < 640; x++)
                        for (int y = 0; y < 480; y++)
                        {
                            FProjective[x + y * 640].X = x;
                            FProjective[x + y * 640].Y = y;
                        }

                    FState.Update += new EventHandler(FState_Update);

                    Status = "OK";
                }
                catch (StatusException e)
                {
                    Status = e.Message;
                }
            }

            void FState_Update(object sender, EventArgs e)
            {
                Update();
            }

            private unsafe void Update()
            {
                if (EnableRGB)
                {

                    if (Mode == ImageNodeMode.RGB)
                    {
                        byte* rgbs = (byte*)FImageGenerator.ImageMapPtr.ToPointer();
                        byte* rgbd = (byte*)Image.Image.Data.ToPointer();

                        for (int i = 0; i < 640 * 480; i++)
                        {
                            rgbd[2] = rgbs[0];
                            rgbd[1] = rgbs[1];
                            rgbd[0] = rgbs[2];
                            rgbs += 3;
                            rgbd += 3;
                        }
                    }
                    else if (Mode == ImageNodeMode.IR)
                    {
                        Image.Image.SetPixels(FIRGenerator.IRMapPtr);
                    }
                    Image.Send();
                }

                Depth.Image.SetPixels(FState.DepthGenerator.DepthMapPtr);
                Depth.Send();

                if (EnableWorld)
                {
                    fillWorld();
                    World.Send();
                }
            }

            private unsafe void fillWorld()
            {
                float* xyz = (float*)World.Data.ToPointer();
                ushort* d = (ushort*)Depth.Data.ToPointer();

                for (int i = 0; i < 640 * 480; ++i)
                    FProjective[i].Z = *d++;

                Point3D[] xyzp = FState.DepthGenerator.ConvertProjectiveToRealWorld(FProjective);

                for (int i = 0; i < 640 * 480; ++i, xyz += 3)
                {
                    xyz[0] = xyzp[i].X / 1000.0f;
                    xyz[1] = xyzp[i].Y / 1000.0f;
                    xyz[2] = xyzp[i].Z / 1000.0f;
                }
            }
        }

		[DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		#region fields & pins
		[Input("Context")]
		ISpread<OpenNIState> FPinInContext;

		[Input("Mode")]
		ISpread<ImageNodeMode> FPinInMode;

        [Input("Enable RGB", IsSingle = true, DefaultValue = 1, Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<bool> FPinInEnableRGB;

        [Input("Enable World", IsSingle = true, DefaultValue = 1, Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<bool> FPinInEnableWorld;

		[Output("Image")]
		ISpread<CVImageLink> FPinOutImageImage;

		[Output("Depth")]
		ISpread<CVImageLink> FPinOutImageDepth;

		[Output("World")]
		ISpread<CVImageLink> FPinOutImageWorld;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

        Spread<ImageInstance> FInstances = new Spread<ImageInstance>(0);
		#endregion fields & pins

		[ImportingConstructor]
		public ImageNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
            SpreadMax = FPinInContext.SliceCount;

            CheckSliceCount(SpreadMax);

            if (FPinInEnableWorld.IsChanged)
                for (int i = 0; i < SpreadMax; i++)
                    FInstances[i].EnableWorld = FPinInEnableWorld[i];

            if (FPinInEnableRGB.IsChanged)
                for (int i = 0; i < SpreadMax; i++)
                    FInstances[i].EnableRGB = FPinInEnableRGB[i];

            for (int i = 0; i < SpreadMax; i++)
                FInstances[i].State = FPinInContext[i];

            for (int i=0; i<SpreadMax; i++)
                FPinOutStatus[i] = FInstances[i].Status;
		}

        void CheckSliceCount(int SpreadMax)
        {
            if (SpreadMax == FInstances.SliceCount)
                return;

            for (int i = FInstances.SliceCount; i < SpreadMax; i++)
                FInstances.Add(new ImageInstance());
            for (int i = SpreadMax; i < FInstances.SliceCount; i++)
            {
                FInstances[i] = null;
                FInstances.RemoveAt(i);
            }

            FPinOutImageDepth.SliceCount = SpreadMax;
            FPinOutImageImage.SliceCount = SpreadMax;
            FPinOutImageWorld.SliceCount = SpreadMax;
            FPinOutStatus.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                FPinOutImageDepth[i] = FInstances[i].Depth.Link;
                FPinOutImageImage[i] = FInstances[i].Image.Link;
                FPinOutImageWorld[i] = FInstances[i].World.Link;
            }
        }
	}
}
