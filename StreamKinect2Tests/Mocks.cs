using System.Diagnostics;
using StreamKinect2;
using System.Collections.Generic;
using System;

namespace StreamKinect2Tests.Mocks
{
    /// <summary>
    /// A mock zeroconf browser which avoids our test suite advertising to all and sundry.
    /// </summary>
    public class MockZeroconfServiceBrowser : IZeroconfServiceBrowser
    {
        public event ServiceRegisteredHandler ServiceRegistered;

        public event ServiceResolvedHandler ServiceResolved;

        private IDictionary<Tuple<string, string>, ushort> m_nameRegTypeToPort;

        public MockZeroconfServiceBrowser()
        {
            m_nameRegTypeToPort = new Dictionary<Tuple<string, string>, ushort>();
        }

        public void Register(string name, string regType, ushort port)
        {
            var args = new ServiceRegisteredArgs
            {
                Name = name,
                RegType = regType,
                Domain = "local.",
            };

            // Record this service
            m_nameRegTypeToPort.Add(Tuple.Create(name, regType), port);

            Trace.WriteLine("Mock Zeroconf Browser signalling register: " + args);
            ServiceRegistered(this, args);
        }

        public void Resolve(string name, string regType, string domain)
        {
            ushort port = 0;
            try
            {
                port = m_nameRegTypeToPort[Tuple.Create(name, regType)];
            }
            catch (KeyNotFoundException)
            {
                Trace.WriteLine("Mock Zeroconf Browser failed to resolve: " + name + ", " + regType + ", " + domain);
                return;
            }

            var args = new ServiceResolvedArgs
            {
                FullName = name + "." + regType + ".local.",
                Hostname = "localhost",
                Port = port,
            };

            Trace.WriteLine("Mock Zeroconf Browser resolving: " + args);
            ServiceResolved(this, args);
        }
    }
}
