using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StreamKinect2;
using System.Threading;
using System.Diagnostics;

namespace StreamKinect2Tests
{

    [TestClass]
    public class SimulatedKinectTests
    {
        // Access to kinect device both through interface and actual type
        private IDevice m_device;
        private SimulatedKinectDevice m_simulatedDevice;

        [TestInitialize]
        public void Initialize()
        {
            m_simulatedDevice = new SimulatedKinectDevice();
            m_device = m_simulatedDevice;
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_simulatedDevice.Dispose();
            m_simulatedDevice = null;
            m_device = null;
        }
        
        [TestMethod]
        public void DepthFrameProperty()
        {
            Assert.AreEqual(DeviceOutputFlags.None, m_device.ActiveOutputs);
            m_device.ActiveOutputs |= DeviceOutputFlags.Depth;
            Assert.AreEqual(DeviceOutputFlags.Depth, m_device.ActiveOutputs);
            m_device.ActiveOutputs &= ~DeviceOutputFlags.Depth;
            Assert.AreEqual(DeviceOutputFlags.None, m_device.ActiveOutputs);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameHandlerCalledALeastOnce()
        {
            int depthFrames = 0;
            m_device.DepthFrame += (IDevice device, DepthFrameHandlerArgs args) => depthFrames += 1;
            m_device.ActiveOutputs |= DeviceOutputFlags.Depth;
            while (depthFrames == 0) { Thread.Sleep(100); }
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameHandlerCalledEnough()
        {
            int depthFrames = 0;
            m_device.DepthFrame += (IDevice device, DepthFrameHandlerArgs args) => depthFrames += 1;
            m_device.ActiveOutputs |= DeviceOutputFlags.Depth;
            Thread.Sleep(1000);
            m_device.ActiveOutputs &= ~DeviceOutputFlags.Depth;

            Debug.WriteLine("Waited one second and received " + depthFrames + " frames.");
            
            // Be generous in what we accept here
            Assert.IsTrue(depthFrames > 30);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameIs1080p()
        {
            int depthFrames = 0, frameWidth = 0, frameHeight = 0;
            m_device.DepthFrame += (IDevice device, DepthFrameHandlerArgs args) =>
            {
                depthFrames += 1;
                frameWidth = args.Width;
                frameHeight = args.Height;
            };
            m_device.ActiveOutputs |= DeviceOutputFlags.Depth;
            while (depthFrames == 0) { Thread.Sleep(100); }
            Assert.AreEqual(1920, frameWidth);
            Assert.AreEqual(1080, frameHeight);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameIsOnly12Bit()
        {
            UInt16[] frameCopy = new UInt16[1920*1080];
            int depthFrames = 0;
            m_device.DepthFrame += (IDevice device, DepthFrameHandlerArgs args) =>
            {
                depthFrames += 1;
                Assert.IsTrue(args.FrameData.Length <= frameCopy.Length);
                args.FrameData.CopyTo(frameCopy, 0);
            };
            m_device.ActiveOutputs |= DeviceOutputFlags.Depth;
            while (depthFrames == 0) { Thread.Sleep(100); }

            foreach(UInt16 v in frameCopy)
            {
                Assert.IsTrue(v <= (1 << 12));
            }
        }

    }
}
