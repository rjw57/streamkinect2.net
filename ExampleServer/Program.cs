using StreamKinect2;
using System;
using System.Diagnostics;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IDevice device = null;

            try
            {
                if (KinectDevice.DefaultDevice != null)
                {
                    Console.WriteLine("Hardware kinect was found.");
                    device = KinectDevice.DefaultDevice;
                }
            }
            catch (TypeInitializationException)
            {
                // This is usually due to somehow running on Windows <8
                Debug.WriteLine("Failed to load Kinect driver.");
            }

            if (device == null)
            {
                Console.WriteLine("No hardware kinect found, using simulated device.");
                device = new SimulatedDevice();
            }

            using (Server server = new Server())
            {
                server.Start();
                server.AddDevice(device);
                System.Console.WriteLine("Press Enter to stop server");
                System.Console.ReadLine();
            }
        }
    }
}
