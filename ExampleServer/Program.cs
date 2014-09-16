using StreamKinect2;
using System.Threading.Tasks;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Server server = new Server())
            {
                server.Start();
                System.Console.WriteLine("Press Enter to stop server");
                System.Console.ReadLine();
            }
        }
    }
}
