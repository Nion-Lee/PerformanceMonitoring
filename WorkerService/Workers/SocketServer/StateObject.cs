using System.Net.Sockets;

namespace WorkerService
{
    public struct StateObject
    {
        public const int bufferSize = 1024;
        public byte[] buffer;
        public Socket workSocket;

        public StateObject(Socket workSocket)
        {
            this.buffer = new byte[bufferSize];
            this.workSocket = workSocket;
        }
    }
}
