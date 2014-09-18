using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StreamKinect2;
using System.Threading;
using StreamKinect2Tests.Mocks;

namespace StreamKinect2Tests
{
    [TestClass]
    public class StoppedServerTests
    {
        // Re-initialised for each test
        private Server m_server;

        // Should be passed to Server.Start()
        private IZeroconfServiceBrowser m_mockZcBrowser;

        [TestInitialize]
        public void Initialize()
        {
            m_server = new Server();
            m_mockZcBrowser = new MockZeroconfServiceBrowser();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Dispose server
            m_server.Dispose();
            m_server = null;
        }

        [TestMethod]
        public void ServerInitialized()
        {
            Assert.IsNotNull(m_server);
        }

        [TestMethod]
        public void ServerDoesNotStartImmediately()
        {
            Assert.IsFalse(m_server.IsRunning);
        }

        [TestMethod, Timeout(3000)]
        public void ServerDoesStartEventually()
        {
            Assert.IsFalse(m_server.IsRunning);
            m_server.Start(m_mockZcBrowser);
            while (!m_server.IsRunning) { Thread.Sleep(100); }
            m_server.Stop();
        }

        [TestMethod, Timeout(3000)]
        public void ServerFiresStartedAndStoppedEvents()
        {
            Assert.IsFalse(m_server.IsRunning);
            int startedCalls = 0, stoppedCalls = 0;
            var started = new AutoResetEvent(false);
            var stopped = new AutoResetEvent(false);

            m_server.Started += (Server s) => { startedCalls += 1; started.Set(); };
            m_server.Stopped += (Server s) => { stoppedCalls += 1; stopped.Set(); };

            Debug.WriteLine("Starting server.");
            m_server.Start(m_mockZcBrowser);
            started.WaitOne();

            Debug.WriteLine("Stopping server.");
            m_server.Stop();
            stopped.WaitOne();

            Assert.AreEqual(1, startedCalls);
            Assert.AreEqual(1, stoppedCalls);
        }

    }
}
