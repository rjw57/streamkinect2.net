using StreamKinect2;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
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
