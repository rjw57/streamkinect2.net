using Bonjour;

namespace StreamKinect2
{
    public class ServiceRegisteredArgs
    {
        public string Name;
        public string RegType;
        public string Domain;

        public override string ToString()
        {
            return base.ToString() + "[Name=\"" + Name + "\",RegType=\"" + RegType + "\",Domain=\"" + Domain + "\"]";
        }
    }

    public class ServiceResolvedArgs
    {
        public string FullName;
        public string Hostname;
        public ushort Port;

        public override string ToString()
        {
            return base.ToString() + "[FullName=\"" + FullName + "\",Hostname=\"" + Hostname + "\",Port=" + Port + "\"]";
        }
    }

    public delegate void ServiceRegisteredHandler(IZeroconfServiceBrowser browser, ServiceRegisteredArgs args);
    public delegate void ServiceResolvedHandler(IZeroconfServiceBrowser browser, ServiceResolvedArgs args);

    public interface IZeroconfServiceBrowser
    {
        event ServiceRegisteredHandler ServiceRegistered;
        event ServiceResolvedHandler ServiceResolved;

        void Register(string name, string regType, ushort port);
        void Resolve(string name, string regType, string domain);
    }

    public class BonjourServiceBrowser : IZeroconfServiceBrowser
    {
        public event ServiceRegisteredHandler ServiceRegistered;

        public event ServiceResolvedHandler ServiceResolved;

        private DNSSDEventManager m_eventManager;
        private DNSSDService m_service;

        public BonjourServiceBrowser()
        {
            // Register interest in service registraion
            m_eventManager = new DNSSDEventManager();
            m_eventManager.ServiceRegistered += EventManager_ServiceRegistered;
            m_eventManager.ServiceResolved += EventManager_ServiceResolved;

            m_service = new DNSSDService();
        }

        public void Register(string name, string regType, ushort port)
        {
            m_service.Register(0, 0, name, regType, null, null, port, null, m_eventManager);
        }

        public void Resolve(string name, string regType, string domain)
        {
            m_service.Resolve(0, 0, name, regType, domain, m_eventManager);
        }

        private void EventManager_ServiceResolved(DNSSDService service, DNSSDFlags flags, uint ifIndex,
            string fullname, string hostname, ushort port, TXTRecord record)
        {
            var args = new ServiceResolvedArgs
            {
                Hostname = hostname,
                FullName = fullname,
                Port = port,
            };

            ServiceResolved(this, args);
        }

        private void EventManager_ServiceRegistered(DNSSDService service, DNSSDFlags flags, string name, string regtype, string domain)
        {
            var args = new ServiceRegisteredArgs
            {
                Name = name,
                RegType = regtype,
                Domain = domain,
            };

            ServiceRegistered(this, args);
        }
    }
}