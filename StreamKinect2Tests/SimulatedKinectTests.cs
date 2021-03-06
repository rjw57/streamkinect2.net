﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StreamKinect2;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

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

            m_device.DepthFrameSource.DepthFrame += (source, args) =>
            {
                Debug.WriteLine("Got frame.");
                depthFrames += 1;
            };

            m_device.DepthFrameSource.Start();
            Thread.Sleep(1000);
            m_device.DepthFrameSource.Stop();

            Debug.WriteLine("Waited one second and received " + depthFrames + " frames.");
            
            // Be generous in what we accept here to be kind to CI systems
            Assert.IsTrue(depthFrames > 15);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameCalledAtLeastOnce()
        {
            var gotFrame = new AutoResetEvent(false);
            m_device.DepthFrameSource.DepthFrame += (source, args) => gotFrame.Set();
            m_device.DepthFrameSource.Start();
            gotFrame.WaitOne();
            m_device.DepthFrameSource.Stop();
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameIsExpectedSize()
        {
            var gotFrame = new AutoResetEvent(false);

            int depthFrames = 0, frameWidth = 0, frameHeight = 0;
            m_device.DepthFrameSource.DepthFrame += (source, args) =>
            {
                depthFrames += 1;
                frameWidth = args.Width;
                frameHeight = args.Height;

                gotFrame.Set();
            };

            m_device.DepthFrameSource.Start();
            gotFrame.WaitOne();
            m_device.DepthFrameSource.Stop();

            Assert.AreEqual(512, frameWidth);
            Assert.AreEqual(424, frameHeight);
        }

        [TestMethod, Timeout(3000)]
        public void DepthFrameIsOnly12Bit()
        {
            var gotFrame = new AutoResetEvent(false);
            UInt16[] frameCopy = new UInt16[1920 * 1080];
            int depthFrames = 0;
            m_device.DepthFrameSource.DepthFrame += (source, args) =>
            {
                depthFrames += 1;
                Assert.IsTrue(args.FrameData.Length <= frameCopy.Length);
                args.FrameData.CopyTo(frameCopy, 0);
                gotFrame.Set();
            };

            m_device.DepthFrameSource.Start();
            gotFrame.WaitOne();
            m_device.DepthFrameSource.Stop();

            foreach(UInt16 v in frameCopy)
            {
                Assert.IsTrue(v <= (1 << 12));
            }
        }
    }
}
