using System.Net.Sockets;

namespace SocketClient_RoughlyForTesting
{
    public struct StateObject
    {
        public Socket workSocket;
        public const int bufferSize = 1024;
        public byte[] buffer;

        public StateObject(Socket workSocket)
        {
            this.workSocket = workSocket;
            this.buffer = new byte[bufferSize];
        }
    }
}
