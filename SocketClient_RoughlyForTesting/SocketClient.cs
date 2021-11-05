using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient_RoughlyForTesting
{
    public class SocketClient
    {
        private bool _flagMalfunction;
        private string _enteringKey = "<EOF>";
        private (int left, int right) _cursorPosition;
        private ManualResetEvent connectDone = new(false);
        private ManualResetEvent sendDone = new(false);
        private ManualResetEvent receiveDone = new(false);

        public int CheckIfPortValid(string[] args)
        {
            int port;

            if (args == null || args.Length == 0)
                port = 9999;
            else
                int.TryParse(args[0], out port);

            return (port is <= 65535 and >= 1024) ? port : 0;
        }

        public async Task StartClient(int port, CancellationToken stoppingToken)
        {
            var ipDefault = "127.0.0.1";
            var ipAddress = IPAddress.Parse(ipDefault);

            var remoteEP = new IPEndPoint(ipAddress, port);
            var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                if (_flagMalfunction) return;

                Console.WriteLine("\n" + "Please enter message to continue the procedure:");
                var input = Console.ReadLine();

                Send(client, input);
                if (_flagMalfunction) return;

                sendDone.WaitOne();

                _cursorPosition = Console.GetCursorPosition();
                await ReceiveEndlessly(client, stoppingToken);
                receiveDone.WaitOne();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            finally
            {
                if (client.Connected)
                    client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            Socket client = null;

            try
            {
                client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
            }
            catch (Exception e)
            {
                _flagMalfunction = true;
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Socket connected to: " + client.RemoteEndPoint);
            connectDone.Set();
        }

        private async Task ReceiveEndlessly(Socket client, CancellationToken stoppingToken)
        {
            var state = new StateObject(client);

            while (!stoppingToken.IsCancellationRequested)
            {
                client.BeginReceive(state.buffer, 0, StateObject.bufferSize, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), state);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            var client = state.workSocket;

            int bytesRead = client.EndReceive(ar);
            var response = Encoding.UTF8.GetString(state.buffer, 0, bytesRead);

            Console.SetCursorPosition(_cursorPosition.left, _cursorPosition.right);
            Console.WriteLine($"Message received <{DateTimeOffset.Now}>:");
            Console.WriteLine(response);

            receiveDone.Set();
        }

        private void Send(Socket client, string data)
        {
            if (!data.Contains(_enteringKey))
            {
                _flagMalfunction = true;
                return;
            }

            var byteData = Encoding.UTF8.GetBytes(data);

            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            var client = (Socket)ar.AsyncState;

            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);
            Console.WriteLine();

            sendDone.Set();
        }
    }
}
