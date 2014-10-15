using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StreamKinect2
{
    /// <summary>
    /// Exception raised by Server on an error.
    /// </summary>
    public class ServerException : Exception
    {
        public ServerException(string message) : base(message) { }
    }

    public delegate void ServerStopStartHandler(Server server);

    internal class ServerDevice : IDisposable
    {
        private IDevice m_device;
        private DepthFrameCompressor m_depthFrameCompressor;

        public ServerDevice(IDevice device)
        {
            m_device = device;
            m_depthFrameCompressor = new DepthFrameCompressor();
            m_depthFrameCompressor.CompressedDepthFrame += DepthFrameCompressor_CompressedDepthFrame;

            m_device.DepthFrameSource.DepthFrame += DepthFrameSource_DepthFrame;
            m_device.DepthFrameSource.Start();
        }

        ~ServerDevice()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_device.DepthFrameSource.IsRunning)
            {
                m_device.DepthFrameSource.Stop();
            }
        }

        private void DepthFrameCompressor_CompressedDepthFrame(DepthFrameCompressor compressor, byte[] data)
        {
            Console.WriteLine("> " + data.Length);
        }

        private void DepthFrameSource_DepthFrame(IDepthFrameSource source, DepthFrameHandlerArgs args)
        {
            m_depthFrameCompressor.NewDepthFrame(source, args);
        }

        public IDevice Device { get { return m_device; } }
    }

    public class Server : IDisposable
    {
        public event ServerStopStartHandler Started;
        public event ServerStopStartHandler Stopped;

        private enum State
        {
            STOPPED,                    // Server is stopped, can be started
            WAITING_FOR_REGISTRATION,   // Waiting for ZeroConf registration callback
            WAITING_FOR_RESOLVE,        // Waiting for ZeroConf resolution to discover our hostname
            RUNNING,                    // Server is running
        }

        private class DeviceEndpoint
        {
            public string      Address;
            public NetMQSocket Socket;
        }

        // Zeroconf browser we're using to register ourselves
        IZeroconfServiceBrowser m_zcBrowser;

        // Connection information
        private string m_hostname;      // Local hostname to advertise endpoints via
        private string m_name;          // Name of this server

        // State machine
        State m_state = State.STOPPED;

        // ZeroMQ related objects
        private NetMQContext m_netMQContext;
        private Poller m_poller;
        private Task m_pollerTask; // non-null iff we are managing the poller ourselves

        // Control socket
        private NetMQSocket m_controlSocket;
        private int m_controlSocketPort;

        // Set of Kinect devices which we know about
        private IDictionary<ServerDevice, IDictionary<string, DeviceEndpoint>> m_devices;
        private Dictionary<IDevice, ServerDevice> m_deviceToServerDevice;

        public Server() : this(new Poller(), NetMQContext.Create())
        {
            // Start poller task
            m_pollerTask = Task.Factory.StartNew(m_poller.Start);
        }

        public Server(Poller poller) : this(poller, NetMQContext.Create()) { }

        public Server(Poller poller, NetMQContext netMQContext)
        {
            // Record the netMQ context we use to create the server.
            m_netMQContext = netMQContext;

            // Record the netMQ poller
            m_poller = poller;

            // Initialise our set of devices
            m_devices = new Dictionary<ServerDevice, IDictionary<string, DeviceEndpoint>>();
            m_deviceToServerDevice = new Dictionary<IDevice, ServerDevice>();
        }

        public void Dispose()
        {
            // Stop the server if it is running.
            if (m_state != State.STOPPED)
            {
                Stop();
            }

            // Ensure that the poller task is stopped if one was started
            if (m_poller.IsStarted && (m_pollerTask != null) && !m_pollerTask.IsCompleted)
            {
                m_poller.Stop();
                m_pollerTask.Wait(100);
            }
        }

        /// <summary>
        /// Start the server registering it with the default Zeroconf service browser.
        /// </summary>
        public void Start()
        {
            Start(new BonjourServiceBrowser());
        }

        public void Start(IZeroconfServiceBrowser zcBrowser)
        {
            // Ensure we're not running.
            if (m_state != State.STOPPED)
            {
                throw new ServerException("Server already running");
            }
            Trace.WriteLine("Starting server");

            // Create a control endpoint socket and bind it to a random port
            m_controlSocket = m_netMQContext.CreateResponseSocket();
            m_controlSocketPort = m_controlSocket.BindRandomPort("tcp://0.0.0.0");
            m_controlSocket.ReceiveReady += ControlSocket_ReceiveReady;
            Trace.WriteLine("Server control socket bound to port: " + m_controlSocketPort);

            // Wire up our event handlers to the browser
            m_zcBrowser = zcBrowser;
            m_zcBrowser.ServiceRegistered += ZeroconfBrowser_ServiceRegistered;
            m_zcBrowser.ServiceResolved += ZeroconfBrowser_ServiceResolved;

            // Wait for registration
            m_state = State.WAITING_FOR_REGISTRATION;

            // Register the server with ZeroConf
            Trace.WriteLine("Registering with ZeroConf");
            zcBrowser.Register("Kinect stream on " + System.Environment.MachineName, "_kinect2._tcp", (ushort) m_controlSocketPort);
        }

        public void Stop()
        {
            // Ensure we're not stopped. We can stop the server even when
            // waiting for ZeroConf registration.
            if (m_state == State.STOPPED)
            {
                throw new ServerException("Server not running");
            }
            Trace.WriteLine("Stopping server");

            // Remove all endpoints for all devices
            foreach(var device in m_devices)
            {
                var endpoints = device.Value;
                foreach(var endpoint in endpoints)
                {
                    endpoint.Value.Socket.Close();
                }
                endpoints.Clear();
            }

            // Stop being interested in Zeroconf events
            m_zcBrowser.ServiceRegistered -= ZeroconfBrowser_ServiceRegistered;
            m_zcBrowser.ServiceResolved -= ZeroconfBrowser_ServiceResolved;
            m_zcBrowser = null;

            m_poller.RemoveSocket(m_controlSocket);
            m_state = State.STOPPED;

            if (Stopped != null) { Stopped(this); }
        }

        public Poller Poller { get { return m_poller; } }

        public bool IsRunning { get { return m_state == State.RUNNING; } }

        public void AddDevice(IDevice device)
        {
            var serverDevice = new ServerDevice(device);
            m_deviceToServerDevice.Add(device, serverDevice);
            m_devices.Add(serverDevice, new Dictionary<string, DeviceEndpoint>());
        }

        public void RemoveDevice(IDevice device)
        {
            var serverDevice = m_deviceToServerDevice[device];
            m_devices.Remove(serverDevice);
            m_deviceToServerDevice.Remove(device);
        }

        protected MePayload GetCurrentMe()
        {
            var devices = new List<DeviceRecord>();
            foreach(var device in m_devices)
            {
                var endpoints = device.Value;

                // create endpoints for device if this is the first time we've been asked about it
                if (endpoints.Count == 0)
                {
                    var depthSocket = m_netMQContext.CreatePublisherSocket();
                    var depthSocketPort = depthSocket.BindRandomPort("tcp://0.0.0.0");
                    endpoints[EndpointTypes.DEPTH] = new DeviceEndpoint
                    {
                        Address = "tcp://" + m_hostname + ":" + depthSocketPort,
                        Socket = depthSocket,
                    };
                }

                var endpointPayload = new Dictionary<string, string>(); 
                foreach(var endpoint in endpoints)
                {
                    endpointPayload[endpoint.Key] = endpoint.Value.Address;
                }
                
                devices.Add(new DeviceRecord
                {
                    id = device.Key.Device.UniqueId,
                    endpoints = endpointPayload,
                });
            }

            return new MePayload
            {
                version = 1,
                name = m_name,
                endpoints = new Dictionary<string, string> {
                    { EndpointTypes.CONTROL, "tcp://" + m_hostname + ":" + m_controlSocketPort },
                },
                devices = devices,
            };
        }

        protected void SendReply(MessageType type, Payload payload = null)
        {
            System.Diagnostics.Trace.WriteLine("Send type: " + type);

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

        private void ZeroconfBrowser_ServiceResolved(IZeroconfServiceBrowser browser, ServiceResolvedArgs args)
        {
            // Only pay attention if the resolved service corresponds to our
            // control socket and we're waiting for it
            if (m_state != State.WAITING_FOR_RESOLVE) { return; }
            if (args.Port != m_controlSocketPort) { return; }

            Trace.WriteLine("Resolved our service: " + args);

            // Record hostname
            m_hostname = args.Hostname;

            // Transition to RUNNING state
            m_state = State.RUNNING;
            if (Started != null) { Started(this); }

            // Now we can start polling for connections
            m_poller.AddSocket(m_controlSocket);
        }

        private void ZeroconfBrowser_ServiceRegistered(IZeroconfServiceBrowser browser, ServiceRegisteredArgs args)
        {
            // Only pay attention if we're waiting for it
            if (m_state != State.WAITING_FOR_REGISTRATION) { return; }

            Trace.WriteLine("Registered our service: " + args);

            // Record our name
            m_name = args.Name;

            // Service is now registered, resolve our hostname
            m_state = State.WAITING_FOR_RESOLVE;
            browser.Resolve(args.Name, args.RegType, args.Domain);
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
            System.Diagnostics.Trace.WriteLine("Received type: " + type);

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