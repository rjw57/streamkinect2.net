using Bonjour;
using Microsoft.Kinect;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace StreamKinect2
{
    public class ServerException : Exception
    {
        public ServerException(string message) : base(message) { }
    }

    public class Server : IDisposable
    {
        private enum State
        {
            STOPPED,                    // Server is stopped, can be started
            WAITING_FOR_REGISTRATION,   // Waiting for ZeroConf registration callback
            WAITING_FOR_RESOLVE,        // Waiting for ZeroConf resolution to discover our hostname
            RUNNING,                    // Server is running
        }

        // Zeroconf magic
        private DNSSDEventManager m_zcEventManager;
        private DNSSDService m_zcService, m_zcRegistrar;

        // Connection information
        private string m_hostname;          // Local hostname to advertise endpoints via
        private string m_name;          // Name of this server

        // state machine
        State m_state = State.STOPPED;

        // ZeroMQ related objects
        private NetMQContext m_netMQContext;
        private Poller m_poller;
        private NetMQSocket m_controlSocket;
        private int m_controlSocketPort;

        public Server(Poller poller, NetMQContext netMQContext = null)
        {
            KinectSensor[] sensors = KinectSensor.KinectSensors.Cast<KinectSensor>().ToArray();
            System.Diagnostics.Debug.WriteLine("Number of sensors connected: " + sensors.Length);

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

            // Register interest in service registraion
            m_zcEventManager.ServiceRegistered += EventManager_ServiceRegistered;
            m_zcEventManager.ServiceResolved += EventManager_ServiceResolved;
        }

        public void Dispose()
        {
            // Stop the server if it is running.
            if (m_state == State.RUNNING)
            {
                Stop();
            }
        }

        public void Start()
        {
            // Ensure we're not running.
            if (m_state != State.STOPPED)
            {
                throw new ServerException("Server already running");
            }
            System.Diagnostics.Debug.WriteLine("Starting server");

            // Create a control endpoint socket and bind it to a random port
            m_controlSocket = m_netMQContext.CreateResponseSocket();
            m_controlSocketPort = m_controlSocket.BindRandomPort("tcp://0.0.0.0");
            m_controlSocket.ReceiveReady += ControlSocket_ReceiveReady;
            System.Diagnostics.Debug.WriteLine("Server control socket bound to port: " + m_controlSocketPort);

            // Register the server with ZeroConf
            System.Diagnostics.Debug.WriteLine("Registering with ZeroConf");
            m_zcRegistrar = m_zcService.Register(0, 0,
                System.Environment.UserName, "_kinect2._tcp", null, null, (ushort)m_controlSocketPort, null, m_zcEventManager);

            // Wait for registration
            m_state = State.WAITING_FOR_REGISTRATION;
        }

        public void Stop()
        {
            // Ensure we're running.
            if (m_state != State.RUNNING)
            {
                throw new ServerException("Server not running");
            }
            System.Diagnostics.Debug.WriteLine("Stopping server");

            m_state = State.RUNNING;
            m_poller.RemoveSocket(m_controlSocket);
        }

        protected MePayload GetCurrentMe()
        {
            var devices = new List<DeviceRecord>();
            foreach(KinectSensor sensor in KinectSensor.KinectSensors)
            {
                devices.Add(new DeviceRecord
                {
                    id = sensor.UniqueKinectId,
                    endpoints = { },
                });
            }

            return new MePayload
            {
                version = 1,
                name = m_name,
                endpoints = new Dictionary<string, string> {
                    { "control", "tcp://" + m_hostname + ":" + m_controlSocketPort },
                },
                devices = devices,
            };
        }

        protected void SendReply(MessageType type, Payload payload = null)
        {
            System.Diagnostics.Debug.WriteLine("Send type: " + type);

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

        void EventManager_ServiceResolved(DNSSDService service, DNSSDFlags flags, uint ifIndex, string fullname, string hostname, ushort port, TXTRecord record)
        {
            if (m_state != State.WAITING_FOR_RESOLVE) { return; }

            // Only pay attention if the resolved service corresponds to our control socket
            if (port != m_controlSocketPort) { return; }

            // Record hostname
            m_hostname = hostname;

            // Transition to RUNNING state
            m_state = State.RUNNING;

            // Now we can start polling for connections
            m_poller.AddSocket(m_controlSocket);
        }

        void EventManager_ServiceRegistered(DNSSDService service, DNSSDFlags flags, string name, string regtype, string domain)
        {
            if (m_state != State.WAITING_FOR_REGISTRATION) { return; }

            // Record our name
            m_name = name;

            // Service is now registered, resolve our hostname
            m_state = State.WAITING_FOR_RESOLVE;
            service.Resolve(0, 0, name, regtype, domain, m_zcEventManager);
        }

        protected void ControlSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
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
}