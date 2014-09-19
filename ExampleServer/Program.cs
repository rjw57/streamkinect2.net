using StreamKinect2;
using System;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IDevice device;

            if(KinectDevice.DefaultDevice != null)
            {
                Console.WriteLine("Hardware kinect was found.");
                device = KinectDevice.DefaultDevice;
            }
            else
            {
                Console.WriteLine("No hardware kinect found, using simulated device.");
                device = new SimulatedDevice();
            }

            using (Server server = new Server())
            {
                server.Start();
                server.AddDevice(new SimulatedDevice());
                System.Console.WriteLine("Press Enter to stop server");
                System.Console.ReadLine();
            }
        }
    }
}
