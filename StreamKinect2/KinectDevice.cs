using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamKinect2
{
    internal class KinectDeviceDepthFrameSource : IDepthFrameSource
    {
        public event DepthFrameHandler DepthFrame;

        private DepthFrameReader depthFrameReader;
        private bool isRunning;
        private FrameDescription depthFrameDescription;
        private DepthFrameHandlerArgs depthFrameArgs;

        public KinectDeviceDepthFrameSource(DepthFrameReader depthFrameReader)
        {
            this.depthFrameReader = depthFrameReader;
            this.depthFrameDescription = depthFrameReader.DepthFrameSource.FrameDescription;
            this.depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;
            this.isRunning = false;

            this.depthFrameArgs = new DepthFrameHandlerArgs
            {
                Width = this.depthFrameDescription.Width,
                Height = this.depthFrameDescription.Height,
                FrameData = new UInt16[this.depthFrameDescription.Width * this.depthFrameDescription.Height],
            };
        }

        private void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if ((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                        }
                    }
                }
            }
        }

        private void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            //ushort* frameData = (ushort*)depthFrameData;

            if (DepthFrame != null) {
                DepthFrame(this, depthFrameArgs);
            }
        }

        public bool IsRunning
        {
            get { return this.isRunning; }
        }

        public void Start()
        {
            this.isRunning = true;
        }

        public void Stop()
        {
            this.isRunning = false;
        }

        public void Dispose()
        {
            // NOP
        }
    }

    public class KinectDevice : IDevice
    {
        public static KinectDevice DefaultDevice = CreateDefaultDevice();

        private KinectSensor sensor;
        private DepthFrameReader depthFrameReader;
        private KinectDeviceDepthFrameSource depthFrameSource;

        private KinectDevice(KinectSensor sensor)
        {
            this.sensor = sensor;
            this.depthFrameReader = this.sensor.DepthFrameSource.OpenReader();
            this.depthFrameSource = new KinectDeviceDepthFrameSource(this.depthFrameReader);
            this.sensor.Open();
        }

        public string UniqueId
        {
            get { return this.sensor.UniqueKinectId; }
        }

        public IDepthFrameSource DepthFrameSource
        {
            get { return this.depthFrameSource; }
        }

        private static KinectDevice CreateDefaultDevice()
        {
            var defaultSensor = KinectSensor.GetDefault();
            if(defaultSensor == null)
            {
                Trace.WriteLine("No hardware Kinect sensor found.");
                return null;
            }

            return new KinectDevice(defaultSensor);
        }
    }
}
