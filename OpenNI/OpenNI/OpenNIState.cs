using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNI;
using System.Threading;

namespace VVVV.Nodes.OpenCV.OpenNI
{
    class OpenNIState
    {
        public string CreationInfo = "";
        public Context Context;
        /// <summary>
        /// We have a special instance here since we
        /// want the thread to always wait on this
        /// generator.
        /// </summary>
        public DepthGenerator DepthGenerator;
        
        private bool FRunning = false;
        public bool Running 
        {
            get
            {
                return FRunning;
            }
        }

        public string Status = "";

        public event EventHandler Initialised;
        public void OnInitialised()
        {
            if (Initialised == null)
                return;
            Initialised(this, EventArgs.Empty);
        }

        public event EventHandler Update;
        public void OnUpdate()
        {
            if (Update == null)
                return;
            Update(this, EventArgs.Empty);
        }

        private Thread FThread;
        public void Start()
        {
            FThread = new Thread(ThreadedFunction);
            FRunning = true;
            FThread.Start();
            OnInitialised();
        }

        public void Stop()
        {
            FRunning = false;
            FThread.Join();
            FThread = null;
        }

        private void ThreadedFunction()
        {
            while (this.Running)
            {
                try
                {
                    Context.WaitOneUpdateAll(DepthGenerator);
                    this.OnUpdate();
                    Status = "OK";
                }
                catch (Exception e)
                {
                    Status = e.Message;
                }
            }
        }
    }
}
