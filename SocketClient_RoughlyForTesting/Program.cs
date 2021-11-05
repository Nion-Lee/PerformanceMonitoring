using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient_RoughlyForTesting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var cilent = new SocketClient();

            int port = cilent.CheckIfPortValid(args);
            var source = new CancellationTokenSource();

            if (port != 0)
                await cilent.StartClient(port, source.Token);
            else
                Console.WriteLine("Invalid Port! Please restart the program.\nPort should be set between 1024~65535.");

            Console.WriteLine("The program has ended.");
        }
    }
}