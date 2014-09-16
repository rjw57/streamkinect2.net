using System;

namespace StreamKinect2
{
    public class DepthFrameHandlerArgs
    {
        public UInt16[] FrameData;
        public int      Width;
        public int      Height;
    }

    public delegate void DepthFrameHandler(IKinectDevice device, DepthFrameHandlerArgs args);

    public interface IKinectDevice
    {
        event DepthFrameHandler DepthFrame;

        string UniqueId { get; }
    }
}