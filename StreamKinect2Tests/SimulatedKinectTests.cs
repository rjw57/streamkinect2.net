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
        private SimulatedDevice m_simulatedDevice;

        [TestInitialize]
        public void Initialize()
        {
            m_simulatedDevice = new SimulatedDevice();
            m_device = m_simulatedDevice;
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_simulatedDevice = null;
            m_device = null;
        }
        
        [TestMethod]
        public void DepthFrameSourceIsNotNull()
        {
            Assert.IsNotNull(m_device.DepthFrameSource);
        }

        [TestMethod]
        public void DepthFrameHandlerCalledEnough()
        {
            int depthFrames = 0;
            using(IDepthFrameSource dfs = m_device.DepthFrameSource) {
                dfs.DepthFrame += (source, args) =>
                {
                    Debug.WriteLine("Got frame.");
                    depthFrames += 1;
                };
                dfs.Start();
                Thread.Sleep(1000);
            }
            Debug.WriteLine("Waited one second and received " + depthFrames + " frames.");
            
            // Be generous in what we accept here to be kind to CI systems
            Assert.IsTrue(depthFrames > 15);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameIs1080p()
        {
            int depthFrames = 0, frameWidth = 0, frameHeight = 0;
            using(IDepthFrameSource dfs = m_device.DepthFrameSource) {
                dfs.DepthFrame += (source, args) =>
                {
                    depthFrames += 1;
                    frameWidth = args.Width;
                    frameHeight = args.Height;
                };
                dfs.Start();
                while (depthFrames == 0) { Thread.Sleep(100); }
            }
            Assert.AreEqual(1920, frameWidth);
            Assert.AreEqual(1080, frameHeight);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameIsOnly12Bit()
        {
            UInt16[] frameCopy = new UInt16[1920*1080];
            int depthFrames = 0;
            using(IDepthFrameSource dfs = m_device.DepthFrameSource) {
                dfs.DepthFrame += (device, args) =>
                {
                    depthFrames += 1;
                    Assert.IsTrue(args.FrameData.Length <= frameCopy.Length);
                    args.FrameData.CopyTo(frameCopy, 0);
                };
                dfs.Start();
                while (depthFrames == 0) { Thread.Sleep(100); }
            }

            foreach(UInt16 v in frameCopy)
            {
                Assert.IsTrue(v <= (1 << 12));
            }
        }
    }
}
