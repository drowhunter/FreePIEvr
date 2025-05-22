using FreePIE.Core.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreePIE.Core.Plugins.Telemetry
{
    public class TcpTelemetryConfig
    {
        public IPEndPoint SendAddress { get; set; }

        //public IPEndPoint ReceiveAddress { get; set; }

        public int ReceiveTimeout { get; set; } = 0;

        public TcpTelemetryConfig()
        {

        }

        /// <summary>
        /// Configure the TCP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="sendAddress">ipaddress:port</param>
        /// <param name="receiveAddress">ipaddress:port</param>      
        public TcpTelemetryConfig(string sendAddress = null)
        {
            SendAddress = ParseAddressAndPort(sendAddress);
            //ReceiveAddress = ParseAddressAndPort(receiveAddress);
        }

        /// <summary>
        /// Configure the TCP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="sendAddress">send address</param>
        /// <param name="receiveAddress">receive address</param>      
        public TcpTelemetryConfig(IPEndPoint sendAddress = null)
        {
            SendAddress = sendAddress;
            //ReceiveAddress = receiveAddress;
        }

        private IPEndPoint ParseAddressAndPort(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var parts = address.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid address format. Expected format: ipaddress:port");
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);
            return new IPEndPoint(ip, port);
        }
    }

    public class TcpTelemetry<TData> : TelemetryBase<TData, TcpTelemetryConfig> where TData : struct
    {
        private static TcpClient tcpClient;

        public event Action<TData> OnReceiveAsync;

        public bool IsConnected => tcpClient?.Connected ?? false;

        public TcpTelemetry(TcpTelemetryConfig config) : base(config)
        {
        }

        protected override void Configure(TcpTelemetryConfig config)
        {
            //if (config.ReceiveAddress != null)
            //{
            //    Log($"Create TcpClient: Receiving @ {config.ReceiveAddress.Address}: {config.ReceiveAddress.Port} with timeout of {Config.ReceiveTimeout} ms");
            //    tcpClient = new TcpClient(config.ReceiveAddress);
            //}
            //else
            //{
            //    Log($"Create TcpClient");
            //    tcpClient = new TcpClient();
            //}

            if (config.SendAddress != null)
            {
                Log($"Create TcpClient");
                tcpClient = new TcpClient();
                Log($"Create Send Adress {config.SendAddress.Address}: {config.SendAddress.Port} with timeout of {Config.ReceiveTimeout} ms");
                tcpClient.Connect(config.SendAddress);
            }

            tcpClient.Client.ReceiveTimeout = Config.ReceiveTimeout;
        }

        public override TData Receive()
        {
            IPEndPoint remoteEp = null;

            var size = Marshal.SizeOf<TData>();

            var buffer = new byte[size];

            tcpClient.Client.Receive(buffer, SocketFlags.None);

            var data = Convert.FromBytes(buffer);

            //OnReceiveAsync?.Invoke(new TcpReceiveResult(buffer, remoteEp), data);

            return data;

        }

        public override int Send(TData data)
        {
            try
            {
                if (IsConnected)
                {
                    var bytes = Convert.ToBytes(data);
                    return tcpClient.Client.Send(bytes);
                }
            }
            catch (SocketException)
            {

            }
            return 0;
        }

        public override void Dispose()
        {
            tcpClient.Close();
        }

        public override async Task<int> SendAsync(TData data, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                var segment = new ArraySegment<byte>(Convert.ToBytes(data));
                var sent = await tcpClient.Client.SendAsync(segment, SocketFlags.None).WithCancellation(cancellationToken);

                return sent;
            }

            return 0;
        }


        public override async Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var size = Marshal.SizeOf<TData>();

                ArraySegment<byte> segment = new ArraySegment<byte>(new byte[size]);

                var result = await tcpClient.Client.ReceiveAsync(segment, SocketFlags.None).WithCancellation(cancellationToken);

                var data = Convert.FromBytes(segment.Array);
                //OnReceiveAsync?.Invoke(data);
                return data;
            }
            catch (OperationCanceledException)
            {
                Log("ReceiveAsync operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Log($"An error occurred during ReceiveAsync: {ex.Message}");
                throw;
            }
        }
    }
}
