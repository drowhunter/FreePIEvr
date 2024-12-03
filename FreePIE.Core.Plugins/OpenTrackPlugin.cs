using System;
using System.Net;
using System.Net.Sockets;

using FreePIE.Core.Contracts;

namespace FreePIE.Core.Plugins
{
    public class OpenTrackData
    {
        public double X;
        public double Y;
        public double Z;
        public double Yaw;
        public double Pitch;
        public double Roll;
    }

    [GlobalType(Type = typeof(OpenTrackGlobal))]
    public class OpenTrackPlugin : Plugin
    {

        private OpenTrackData _output = null;

        private UdpClient _socket;


        public OpenTrackData Output => _output ?? (_output = new OpenTrackData());


        public OpenTrackPlugin()
        {

        }

        public override object CreateGlobal()
        {
            return new OpenTrackGlobal(this);
        }

        public override Action Start()
        {
            _socket = new UdpClient();
            _socket.Connect(IPAddress.Loopback, 4242);

            return null;
        }

        public override void Stop()
        {
            if (_socket != null && _socket.Client.Connected)
            {
                _socket.Close();
                _socket = null;
            }

        }

        public override string FriendlyName => "OpenTrack";

        public override void DoBeforeNextExecute()
        {
            if (_output != null)
            {
                var dataBytes = new byte[48];

                Buffer.BlockCopy(new double[] { _output.X * 100, _output.Y * 100, _output.Z * 100, _output.Yaw, _output.Pitch, _output.Roll }, 0, dataBytes, 0, 48);

                _socket.Send(dataBytes, dataBytes.Length);
                _output = null;
            }

        }
    }

    [Global(Name = "openTrack")]
    public class OpenTrackGlobal : UpdateblePluginGlobal<OpenTrackPlugin>
    {
        // public string Name
        // {
        //     get => "openTrack";
        // }
        public OpenTrackGlobal(OpenTrackPlugin plugin) : base(plugin)
        { }

        /// <summary>
        /// value in degrees
        /// </summary>     
        public double yaw
        {
            get => plugin.Output.Yaw;
            set { plugin.Output.Yaw = value; }
        }
        /// <summary>
        /// value in degrees
        /// </summary>
        public double pitch
        {
            get => plugin.Output.Pitch;
            set => plugin.Output.Pitch = value;
        }
        /// <summary>
        /// value in degrees
        /// </summary>
        public double roll
        {
            get => plugin.Output.Roll;
            set => plugin.Output.Roll = value;
        }

        /// <summary>
        /// value in meters
        /// </summary>
        public double x
        {
            get => plugin.Output.X;
            set => plugin.Output.X = value;
        }

        /// <summary>
        /// value in meters
        /// </summary>
        public double y
        {
            get => plugin.Output.Y;
            set => plugin.Output.Y = value;
        }

        /// <summary>
        /// value in meters
        /// </summary>
        public double z
        {
            get => plugin.Output.Z;
            set => plugin.Output.Z = value;
        }
    }
}
