using Lz4Net;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StreamKinect2
{
    internal class KinectDeviceDepthFrameSource : IDepthFrameSource
    {
        public event DepthFrameHandler DepthFrame;

        private DepthFrameReader m_depthFrameReader;
        private bool m_isRunning;
        private FrameDescription m_depthFrameDescription;
        private ushort[] m_depthPixels;

        public KinectDeviceDepthFrameSource(DepthFrameReader depthFrameReader)
        {
            this.m_depthFrameReader = depthFrameReader;
            this.m_depthFrameDescription = depthFrameReader.DepthFrameSource.FrameDescription;
            this.m_depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;
            this.m_isRunning = false;

            this.m_depthPixels =  new UInt16[m_depthFrameDescription.Width * m_depthFrameDescription.Height];
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
                        if ((this.m_depthFrameDescription.Width * this.m_depthFrameDescription.Height) == (depthBuffer.Size / this.m_depthFrameDescription.BytesPerPixel))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            maxDepth = depthFrame.DepthMaxReliableDistance;

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                        }
                    }
                }
            }
        }

        // This function required /unsafe due to the direct pointer access below:
        private void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            unsafe
            {
                // depth frame data is a 16 bit value
                UInt16* frameData = (UInt16*)depthFrameData;

                // convert depth to a visual representation
                for (int i = 0; i < (int)(depthFrameDataSize / this.m_depthFrameDescription.BytesPerPixel); ++i)
                {
                    // Get the depth for this pixel
                    UInt16 depth = frameData[i];

                    // To convert to a byte, we're mapping the depth value to the byte range.
                    // Values outside the reliable depth range are mapped to 0 (black).
                    m_depthPixels[i] = (depth >= minDepth && depth <= maxDepth) ? depth : (UInt16)0;
                }
            }

            if (m_isRunning)
            {
                var depthFrameArgs = new DepthFrameHandlerArgs
                {
                    Width = this.m_depthFrameDescription.Width,
                    Height = this.m_depthFrameDescription.Height,
                    FrameData = m_depthPixels,
                };

                if (DepthFrame != null)
                {
                    DepthFrame(this, depthFrameArgs);
                }
            }
        }

        public bool IsRunning
        {
            get { return this.m_isRunning; }
        }

        public void Start()
        {
            this.m_isRunning = true;
        }

        public void Stop()
        {
            this.m_isRunning = false;
        }

        public void Dispose()
        {
            // NOP
        }
    }

    public class KinectDevice : IDevice
    {
        public static KinectDevice DefaultDevice = CreateDefaultDevice();

        private KinectSensor m_sensor;
        private DepthFrameReader m_depthFrameReader;
        private KinectDeviceDepthFrameSource m_depthFrameSource;

        private KinectDevice(KinectSensor sensor)
        {
            this.m_sensor = sensor;
            this.m_sensor.IsAvailableChanged += OnSensorIsAvailableChanged;
            this.m_depthFrameReader = this.m_sensor.DepthFrameSource.OpenReader();
            this.m_depthFrameSource = new KinectDeviceDepthFrameSource(this.m_depthFrameReader);
            this.m_sensor.Open();
        }

        private void OnSensorIsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            Trace.WriteLine("Sensor availability changed: " + m_sensor.IsAvailable);
        }

        public string UniqueId
        {
            get { return this.m_sensor.UniqueKinectId; }
        }

        public IDepthFrameSource DepthFrameSource
        {
            get { return this.m_depthFrameSource; }
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
