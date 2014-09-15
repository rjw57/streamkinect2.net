using System.Collections.Generic;

namespace StreamKinect2
{
    public enum MessageType : byte
    {
        ERROR = 0x00,
        PING = 0x01,
        PONG = 0x02,
        WHO = 0x03,
        ME = 0x04,

        INVALID = 0xff,
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
}