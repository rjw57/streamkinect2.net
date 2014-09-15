using NetMQ;
using StreamKinect2;
using System.Threading.Tasks;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Poller poller = new Poller())
            {
                using (Server server = new Server(poller))
                {
                    Task pollerTask = Task.Factory.StartNew(poller.Start);
                    System.Console.WriteLine("Press Enter to stop server");
                    System.Console.ReadLine();
                    poller.Stop();
                }
            }
        }
    }
}
