using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StreamKinect2;
using System.Threading;
using System.Diagnostics;

namespace StreamKinect2Tests
{
    [TestClass]
    public class DepthCompressorTests
    {
        private DepthFrameCompressor m_compressor;
        private IDepthFrameSource m_depthFrameSource;

        [TestInitialize]
        public void Initialise()
        {
            m_compressor = new DepthFrameCompressor();
            m_depthFrameSource = new SimulatedDepthFrameSource();
            m_depthFrameSource.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_compressor = null;

            if (m_depthFrameSource.IsRunning)
            {
                m_depthFrameSource.Stop();
            }
            m_depthFrameSource.Dispose();
            m_depthFrameSource = null;
        }

        [TestMethod, Timeout(3000)]
        public void CompressorGetsFrames()
        {
            var gotFrameEvent = new AutoResetEvent(false);
            m_compressor.CompressedDepthFrame += (compressor, data) => gotFrameEvent.Set();
            m_depthFrameSource.DepthFrame += m_compressor.NewDepthFrame;
            gotFrameEvent.WaitOne();
        }

        [TestMethod, Timeout(3000)]
        public void CompressorGetsEnoughFrames()
        {
            int nFrames = 0, totalLength = 0;
            m_compressor.CompressedDepthFrame += (compressor, data) =>
            {
                nFrames += 1;
                totalLength += data.Length; 
            };

            m_depthFrameSource.DepthFrame += m_compressor.NewDepthFrame;
            Thread.Sleep(1000);

            Debug.WriteLine("Waited one second and got " + nFrames +
                " compressed frames with a total length of " + (totalLength >> 10) + "kB .");

            // Be generous in what we expect. We're not benchmarking the CI VMs.
            Assert.IsTrue(nFrames > 10);
        }
    }
}
