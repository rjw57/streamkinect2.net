using Microsoft.Kinect;
using System;

namespace StreamKinect2
{
    public class DepthFrameHandlerArgs
    {
        public UInt16[] FrameData;
        public int      Width;
        public int      Height;
    }

    public delegate void DepthFrameHandler(IDevice device, DepthFrameHandlerArgs args);

    public interface IDevice
    {
        event DepthFrameHandler DepthFrame;

        string UniqueId { get; }
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
    }

    public class SimulatedKinectDevice : IDevice
    {
        public event DepthFrameHandler DepthFrame;

        private string m_uniqueId;

        public SimulatedKinectDevice()
        {
            // the unique ID is simply a GUID
            m_uniqueId = Guid.NewGuid().ToString();
        }

        public string UniqueId
        {
            get { return m_uniqueId; }
        }
    }
}