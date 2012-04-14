#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes.RGBDToolkit
{
	#region PluginInfo
	[PluginInfo(Name = "LoadFrame", Category = "RGBDToolkit", Help = "Load a raw binary frame from RGBDToolkit", Tags = "")]
	#endregion PluginInfo
	public class RGBDToolkitLoadFrameNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename)]
		IDiffSpread<string> FInput;

		[Input("Reload", IsBang = true, IsSingle = true)]
		ISpread<bool> FReload;

		[Output("Frame")]
		ISpread<Frame> FOutput;

		[Output("Indices count")]
		ISpread<int> FIndicesCount;

		[Output("Vertices count")]
		ISpread<int> FVerticesCount;

		[Output("TexCoords count")]
		ISpread<int> FTexCoordsCount;

		[Output("Loaded")]
		ISpread<bool> FLoaded;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		Frame FFrame;

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;
			FIndicesCount.SliceCount = SpreadMax;
			FVerticesCount.SliceCount = SpreadMax;
			FTexCoordsCount.SliceCount = SpreadMax;
			FLoaded.SliceCount = SpreadMax;
			FStatus.SliceCount = SpreadMax;

			if (FInput.IsChanged || FReload[0])
			{
				FOutput.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					Frame frame = new Frame(FInput[i]);
					FOutput[i] = frame;
					FIndicesCount[i] = frame.Loaded ? frame.Indices.Length : 0;
					FVerticesCount[i] = frame.Loaded ? frame.Vertices.Length : 0;
					FTexCoordsCount[i] = frame.Loaded ? frame.TexCoords.Length : 0;
					FLoaded[i] = frame.Loaded;
					FStatus[i] = frame.Status;
				}
			}

		}
	}
}
