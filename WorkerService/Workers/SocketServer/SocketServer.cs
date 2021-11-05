using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerService.Workers
{
    public class SocketServer : BackgroundService
    {
        private static int _connections;
        private readonly ILogger<SocketServer> _logger;
        public static ManualResetEvent _allDone = new(false);

        public SocketServer(ILogger<SocketServer> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(
                () => StartListening(stoppingToken));
        }
        
        public void StartListening(CancellationToken stoppingToken)
        {
            bool isValid = IsPortAndIpValid(out int port, out IPAddress ipAddress);

            if (!isValid)
            {
                _logger.LogInformation("Invalid port or IP address! Please check then restart the program.");
                return;
            }

            var localEndPoint = new IPEndPoint(ipAddress, port);
            var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEndPoint);
            listener.Listen(1000);

            while (!stoppingToken.IsCancellationRequested)
            {
                _allDone.Reset();
                _logger.LogInformation($"Listening for connecting... (now {_connections} connections)");

                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                _allDone.WaitOne();
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                _allDone.Set();
                Interlocked.Increment(ref _connections);

                var listener = (Socket)ar.AsyncState;
                var handler = listener.EndAccept(ar);

                var state = new StateObject(handler);
                handler.BeginReceive(state.buffer, 0, StateObject.bufferSize, SocketFlags.None,
                    new AsyncCallback(ReadCallback), state);
            }

            catch (Exception e)
            {
                _logger.LogError(e.Message);
                Interlocked.Decrement(ref _connections);
            }
        }

        public async void ReadCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            var handler = state.workSocket;
            var stoppingToken = new CancellationToken();

            try
            {
                handler.EndReceive(ar);
                if (!stoppingToken.IsCancellationRequested)
                    await SendEndlessly(handler, stoppingToken);
            }

            catch (Exception e)
            {
                _logger.LogError(e.Message);
                Interlocked.Decrement(ref _connections);
            }

            finally
            {
                if (handler.Connected)
                    handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }

        private async Task SendEndlessly(Socket handler, CancellationToken stoppingToken)
        {
            string msg;
            byte[] byteSend;

            while (!stoppingToken.IsCancellationRequested)
            {
                msg = PerformanceCounter.GetPrintOutData() + "\n" + PerformanceCounter.GetPrintOutError();
                byteSend = Encoding.UTF8.GetBytes(msg);

                handler.BeginSend(byteSend, 0, byteSend.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), handler);

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            var handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
        }

        private bool IsPortAndIpValid(out int port, out IPAddress ipAddress)
        {
            port = 0; ipAddress = null;

            return int.TryParse(SocketConfig.Port, out port)
                && IPAddress.TryParse(SocketConfig.Ip, out ipAddress);
        }
    }
}
