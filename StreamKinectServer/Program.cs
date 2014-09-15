using Bonjour;
using NetMQ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StreamKinectServer
{
    public enum MessageType : byte
    {
        ERROR       = 0x00,
        PING        = 0x01,
        PONG        = 0x02,
        WHO         = 0x03,
        ME          = 0x04,

        INVALID     = 0xff,
    }

    public class Payload { }

    public class ErrorPayload : Payload
    {
        public string reason;
    }

    public class DeviceRecord : Payload
    {
        public string id;
        public IDictionary<string, string> endpoints;
    }

    public class MePayload : Payload
    {
        public int version;
        public string name;
        public IDictionary<string, string> endpoints;
        public IList<DeviceRecord> devices;
    }

    public class ServerException : Exception {
        public ServerException(string message) : base(message) { }
    }

    public class Server : IDisposable
    {
        // Zeroconf magic
        private DNSSDEventManager m_zcEventManager;
        private DNSSDService m_zcService, m_zcRegistrar;

        private Poller m_poller;
        private NetMQContext m_netMQContext;
        private NetMQSocket m_controlSocket;
        private bool m_isRunning;

        public Server(Poller poller, NetMQContext netMQContext = null)
        {
            // Record the netMQ context we use to create the server.
            m_netMQContext = (netMQContext != null) ? netMQContext : NetMQContext.Create();

            // Record the netMQ poller
            m_poller = poller;

            // Initialise ZeroConf
            try
            {
                m_zcService = new DNSSDService();
                m_zcEventManager = new DNSSDEventManager();
            }
            catch
            {
                throw new ServerException("ZeroConf is not available");
            }

            // Start the server.
            Start();
        }

        ~Server()
        {
            if (m_isRunning)
            {
                Stop();
            }
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
            System.Diagnostics.Debug.WriteLine("Starting server");

            // Create a control endpoint socket and bind it to a random port
            m_controlSocket = m_netMQContext.CreateResponseSocket();
            int controlSocketPort = m_controlSocket.BindRandomPort("tcp://0.0.0.0");
            m_poller.AddSocket(m_controlSocket);
            m_controlSocket.ReceiveReady += controlSocket_ReceiveReady;
            System.Diagnostics.Debug.WriteLine("Server control socket bound to port: " + controlSocketPort);

            // Register the server with ZeroConf
            System.Diagnostics.Debug.WriteLine("Registering with ZeroConf");
            m_zcRegistrar = m_zcService.Register(0, 0,
                System.Environment.UserName, "_kinect2._tcp", null, null, (ushort)controlSocketPort, null, m_zcEventManager);

            m_isRunning = true;
        }

        protected void Stop()
        {
            // Ensure we're running.
            if (!m_isRunning)
            {
                throw new ServerException("Server not running");
            }
            System.Diagnostics.Debug.WriteLine("Stopping server");

            m_poller.RemoveSocket(m_controlSocket);

            m_isRunning = false;
        }

        protected MePayload GetCurrentMe()
        {
            return new MePayload
            {
                version = 1,
                name = "Test kinext",
                endpoints = new Dictionary<string, string> {
                    { "control", "foo" },
                },
                devices = new DeviceRecord[] {

                },
            };
        }

        protected void SendReply(MessageType type, Payload payload = null)
        {
            byte[] typeFrame = { (byte)type, };
            m_controlSocket.Send(typeFrame, 1, false, payload != null);
            if (payload != null)
            {
                var ser = new JavaScriptSerializer();
                m_controlSocket.Send(ser.Serialize(payload));
            }
        }

        protected void SendErrorReply(string reason)
        {
            ErrorPayload payload = new ErrorPayload { reason = reason };
            SendReply(MessageType.ERROR, payload);
        }

        // EVENT HANDLERS

        protected void controlSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            // Receive multipart message,
            byte[][] messages = e.Socket.ReceiveMessages().Cast<byte[]>().ToArray();

            if ((messages.Length < 1) || (messages.Length > 2))
            {
                SendErrorReply("Invalid message length of " + messages.Length + ".");
                return;
            }

            if (messages[0].Length != 1)
            {
                SendErrorReply("Message type frame has invalid length: " + messages[0].Length);
                return;
            }

            // Extract message type
            MessageType type = MessageType.INVALID;
            try
            {
                type = (MessageType)(messages[0][0]);
            }
            catch
            {
                SendErrorReply("Unknown message type: " + messages[0][0]);
                return;
            }

            // Parse type and payload
            System.Diagnostics.Debug.WriteLine("Received type: " + type);

            // Switch on type:
            switch (type)
            {
                case MessageType.PING:
                    SendReply(MessageType.PONG);
                    break;
                case MessageType.WHO:
                    SendReply(MessageType.ME, GetCurrentMe());
                    break;
                default:
                    SendErrorReply("Unknown message");
                    break;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (Poller poller = new Poller())
            {
                using (Server server = new Server(poller))
                {
                    Task pollerTask = Task.Factory.StartNew(poller.Start);
                    System.Console.WriteLine("Hello, world.");
                    System.Console.ReadLine();
                    poller.Stop();
                }
            }
        }
    }
}
