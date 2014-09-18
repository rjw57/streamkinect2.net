using Microsoft.Kinect;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StreamKinect2
{
    public class DepthFrameHandlerArgs
    {
        public UInt16[] FrameData;
        public int      Width;
        public int      Height;
    }

    public delegate void DepthFrameHandler(IDevice device, DepthFrameHandlerArgs args);

    [Flags]
    public enum DeviceOutputFlags
    {
        None    = 0x00,
        Depth   = 0x01,
    }

    public interface IDevice
    {
        event DepthFrameHandler DepthFrame;

        string UniqueId { get; }

        DeviceOutputFlags ActiveOutputs { get; set; }
    }

    public class KinectDevice : IDevice
    {
        public event DepthFrameHandler DepthFrame;

        private KinectSensor m_sensor;

        public KinectDevice()
        {
            m_sensor = KinectSensor.GetDefault();
        }

        public string UniqueId
        {
            get { return m_sensor.UniqueKinectId; }
        }

        public DeviceOutputFlags ActiveOutputs
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }

    public class SimulatedKinectDevice : IDevice, IDisposable
    {
        public event DepthFrameHandler DepthFrame;

        private string m_uniqueId;
        private Task m_depthFrameTask;
        private bool m_depthFrameTaskShouldExit;

        public SimulatedKinectDevice()
        {
            // the unique ID is simply a GUID
            m_uniqueId = Guid.NewGuid().ToString();

            m_depthFrameTask = null;
            m_depthFrameTaskShouldExit = false;
        }

        public string UniqueId
        {
            get { return m_uniqueId; }
        }

        private void DepthThread()
        {
            var args = new DepthFrameHandlerArgs
            {
                Width = 1920, Height = 1080,
                FrameData = new UInt16[1920 * 1080],
            };

            while (!m_depthFrameTaskShouldExit)
            {
                var then = System.DateTime.Now;
                for (int y = 0; y < args.Height; y++)
                    for (int x = 0; x < args.Width; x++)
                    {
                        args.FrameData[x + (y * args.Width)] = (UInt16)(
                            1024 * (1.2 + Math.Sin(x * 0.1) * Math.Sin(0.3 + (y + 0.2 * x) * 0.2))
                        );
                    }

                DepthFrame(this, args);
                var now = System.DateTime.Now;

                var delta = now - then;
                Thread.Sleep((int)(Math.Max(0, 1000 / 60 - delta.TotalMilliseconds)));
            }
        }

        public DeviceOutputFlags ActiveOutputs
        {
            get
            {
                DeviceOutputFlags flags = DeviceOutputFlags.None;
                if ((m_depthFrameTask != null) && !m_depthFrameTask.IsCanceled)
                {
                    flags |= DeviceOutputFlags.Depth;
                }
                return flags;
            }
            set
            {
                DeviceOutputFlags currentOutputs = ActiveOutputs;
                DeviceOutputFlags changedOutputs = currentOutputs ^ value;

                if ((changedOutputs & DeviceOutputFlags.Depth) != 0)
                {
                    // Either we need to stop or start depth output
                    if ((value & DeviceOutputFlags.Depth) != 0)
                    {
                        // Start
                        m_depthFrameTaskShouldExit = false;
                        m_depthFrameTask = Task.Factory.StartNew(DepthThread);
                    }
                    else
                    {
                        // Stop
                        m_depthFrameTaskShouldExit = true;
                        m_depthFrameTask.Wait(300);
                        m_depthFrameTask = null;
                        m_depthFrameTaskShouldExit = false;
                    }
                }
            }
        }

        public void Dispose()
        {
            ActiveOutputs = DeviceOutputFlags.None;
        }
    }
}