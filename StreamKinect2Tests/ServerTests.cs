using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ;
using StreamKinect2;
using System.Threading;

namespace StreamKinect2Tests
{
    [TestClass]
    public class ServerTests
    {
        // Re-initialised for each test
        private Poller m_poller = null;
        private Task m_pollerTask = null;
        private Server m_server = null;

        [TestInitialize]
        public void Initialize()
        {
            // Initialise poller and start poll task
            m_poller = new Poller();
            m_pollerTask = Task.Factory.StartNew(m_poller.Start);

            // Initialise server
            m_server = new Server(m_poller);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Stop poller
            if (m_poller.IsStarted)
            {
                m_poller.Stop();
                m_pollerTask.Wait(200);
            }
            m_pollerTask = null;

            // Dispose poller
            m_poller.Dispose();
            m_poller = null;

            // Dipose server
            m_server.Dispose();
            m_server = null;
        }

        [TestMethod]
        public void PollerAndServerInitialized()
        {
            Assert.IsNotNull(m_poller);
            Assert.IsNotNull(m_server);
        }

        [TestMethod]
        public void ServerDoesNotStartImmediately()
        {
            Assert.IsFalse(m_server.IsRunning);
        }

        /*
        [TestMethod, Timeout(3000)]
        public void ServerDoesStartEventually()
        {
            Assert.IsFalse(m_server.IsRunning);
            m_server.Start();
            while (!m_server.IsRunning)
            {
                Debug.WriteLine("is server running: " + m_server.IsRunning);
                Thread.Sleep(100);
            }
            m_server.Stop();
        }
         */
    }
}
