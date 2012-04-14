using System.Threading;

namespace VVVV.Nodes.OpenCV
{
	public abstract class AbstractInstance
	{
		protected string Status;

		public abstract void Close();
		protected abstract void Process();
		public abstract void Initialise();
	}
}
