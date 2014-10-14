using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamKinect2
{
    public class KinectDevice : IDevice
    {
        public static KinectDevice DefaultDevice = CreateDefaultDevice();
        private KinectSensor sensor;

        private KinectDevice(KinectSensor sensor)
        {
            this.sensor = sensor;
        }

        public string UniqueId
        {
            get { return this.sensor.UniqueKinectId; }
        }

        public IDepthFrameSource DepthFrameSource
        {
            get { throw new NotImplementedException(); }
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
