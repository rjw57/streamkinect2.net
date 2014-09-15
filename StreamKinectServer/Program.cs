using Bonjour;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StreamKinectServer
{
    public class ServerException : Exception {
        public ServerException(string message) : base(message) { }
    }

    public class Server : IDisposable
    {
        private NetMQContext m_netMQContext;
        private NetMQSocket m_controlSocket;
        private bool m_isRunning;

        public Server(NetMQContext netMQContext = null)
        {
            // Record the netMQ context we use to create the server.
            m_netMQContext = (netMQContext != null) ? netMQContext : NetMQContext.Create();

            // Start the server.
            Start();
        }

        public void Dispose()
        {
            // Stop the server if it is running.
            if (m_isRunning)
            {
                Stop();
            }
        }

        protected void Start()
        {
            // Ensure we're not running.
            if(m_isRunning) {
                throw new ServerException("Server already running");
            }

            // Create a control endpoint socket and bind it to a random port
            m_controlSocket = m_netMQContext.CreateResponseSocket();
            m_controlSocket.BindRandomPort("tcp://0.0.0.0");

            System.Diagnostics.Debug.WriteLine("Starting server on address " + m_controlSocket);

            m_isRunning = true;
        }

        protected void Stop()
        {
            // Ensure we're running.
            if (!m_isRunning)
            {
                throw new ServerException("Server not running");
            }

            m_isRunning = false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Create Bonjour event manager
            DNSSDEventManager eventManager = new DNSSDEventManager();

            System.Diagnostics.Debug.Listeners.Add(
                new System.Diagnostics.DefaultTraceListener()
            );

            DNSSDService service = new DNSSDService(), registrar;

            IPEndPoint localEP = new IPEndPoint(System.Net.IPAddress.Any, 0);
            System.Console.WriteLine(localEP);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(localEP);
            localEP = (IPEndPoint)socket.LocalEndPoint;
            System.Console.WriteLine(localEP);

            registrar = service.Register( 0, 0, System.Environment.UserName, "_kinect2._tcp", null, null, ( ushort ) localEP.Port, null, eventManager );

            using (Server server = new Server())
            {
                System.Console.WriteLine("Hello, world.");
                System.Console.ReadLine();
            }
        }
    }
}
