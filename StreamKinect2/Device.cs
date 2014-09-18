using System;

namespace StreamKinect2
{
    public class DepthFrameHandlerArgs
    {
        public UInt16[] FrameData;
        public int      Width;
        public int      Height;
    }

    public delegate void DepthFrameHandler(IDepthFrameSource device, DepthFrameHandlerArgs args);

    public interface ISource : IDisposable
    {
        bool IsRunning { get;  }
        void Start();
        void Stop();
    }

    public interface IDepthFrameSource : ISource
    {
        event DepthFrameHandler DepthFrame;
    }

    public interface IDevice
    {
        string UniqueId { get; }
        IDepthFrameSource DepthFrameSource { get; }
    }
}