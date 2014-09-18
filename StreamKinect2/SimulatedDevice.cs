using System;
using System.Threading;
using System.Threading.Tasks;

namespace StreamKinect2
{

    public class SimulatedDepthFrameSource : IDepthFrameSource
    {
        private Task m_task;
        private bool m_taskShouldExit;

        public event DepthFrameHandler DepthFrame;

        public SimulatedDepthFrameSource()
        {
            m_task = null;
            m_taskShouldExit = false;
        }

        public bool IsRunning
        {
            get { return (m_task != null) && !m_task.IsCanceled; }
        }

        public void Start()
        {
            m_taskShouldExit = false;
            m_task = Task.Factory.StartNew(TaskLoop);
        }

        public void Stop()
        {
            // Stop
            m_taskShouldExit = true;
            m_task.Wait(300);
            m_task = null;
            m_taskShouldExit = false;
        }

        public void Dispose()
        {
            if (IsRunning) { Stop(); }
        }

        private void TaskLoop()
        {
            var args = new DepthFrameHandlerArgs
            {
                Width = 1920,
                Height = 1080,
                FrameData = new UInt16[1920 * 1080],
            };

            while (!m_taskShouldExit)
            {
                var then = System.DateTime.Now;
                for (int y = 0; y < args.Height; y++)
                    for (int x = 0; x < args.Width; x++)
                    {
                        args.FrameData[x + (y * args.Width)] = (UInt16)(x & ((1 << 12) - 1));
                    }

                DepthFrame(this, args);
                var now = System.DateTime.Now;

                var delta = now - then;
                Thread.Sleep((int)(Math.Max(0, 1000 / 60 - delta.TotalMilliseconds)));
            }
        }

    }

    public class SimulatedDevice : IDevice
    {
        private string m_uniqueId;
        private SimulatedDepthFrameSource m_depthFrameSource;

        public SimulatedDevice()
        {
            // the unique ID is simply a GUID
            m_uniqueId = Guid.NewGuid().ToString();

            m_depthFrameSource = new SimulatedDepthFrameSource();
        }

        public string UniqueId
        {
            get { return m_uniqueId; }
        }

        public IDepthFrameSource DepthFrameSource
        {
            get { return m_depthFrameSource; }
        }
    }
}