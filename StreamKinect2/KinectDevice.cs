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

        public KinectDeviceDepthFrameSource(DepthFrameReader depthFrameReader)
        {
            this.depthFrameReader = depthFrameReader;
            this.depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;
            this.isRunning = false;
        }

        private void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            // TODO: handle depth frame
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
