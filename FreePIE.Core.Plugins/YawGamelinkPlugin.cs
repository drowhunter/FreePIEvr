using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.Telemetry;

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FreePIE.Core.Plugins
{
    public struct YawGLData
    {
        public float yaw;
        public float pitch;
        public float roll;
        public byte amp;
        public byte hz;
        public byte fan;

        public override string ToString()
        {
            var c = CultureInfo.InvariantCulture;

            string retval = string.Format("Y[{0}]P[{1}]R[{2}]",
                yaw.ToString("000.000", c),
                pitch.ToString("000.000", c),
                roll.ToString("000.000", c)
            );
            
            string vibes = string.Format("V[{0},{1},{2},{3}]", new object[]
            {
                amp.ToString(c),
                amp.ToString(c),
                amp.ToString(c),
                hz.ToString(c)
            });
            retval += vibes;
            
            
            string f = string.Format("F[{0},{0}]", fan.ToString(c));
            retval += f;

            return retval;
            
        }
    }

    class YawGLByteConverter : IByteConvertor<YawGLData>
    {
        static Regex rot = new Regex($@"Y\[(?<yaw>-?[\d.]+)\]P\[(?<pitch>-?[\d.]+)\]R\[(?<roll>-?[\d.]+)\]");

        static Regex vibes = new Regex($@"V\[(?<amp>\d+?),\d*?,\d*?,(?<hz>\d*?)\]");

        static Regex fan = new Regex($@"F\[(?<fan>\d+?)");

        static CultureInfo c = CultureInfo.InvariantCulture;

        public YawGLData FromBytes(byte[] data)
        {
            var dataString = Encoding.ASCII.GetString(data);
            var yawGLData = new YawGLData();

            try
            {
                var r = rot.Match(dataString);
                if (r.Success)
                {
                    yawGLData.yaw = float.Parse(r.Groups["yaw"].Value, c);
                    yawGLData.pitch = float.Parse(r.Groups["pitch"].Value, c);
                    yawGLData.roll = float.Parse(r.Groups["roll"].Value, c);
                }

                var v = vibes.Match(dataString);
                if (v.Success)
                {
                    yawGLData.amp = byte.Parse(v.Groups["amp"].Value, c);
                    yawGLData.hz = byte.Parse(v.Groups["hz"].Value, c);
                }

                var f = fan.Match(dataString);
                if (f.Success)
                {
                    yawGLData.fan = byte.Parse(f.Groups["fan"].Value, c);
                }

                
            }
            catch
            {
                // Handle parsing errors if necessary  
            }

            return yawGLData;
        }

        public byte[] ToBytes(YawGLData data)
        {            
            byte[] array = Encoding.ASCII.GetBytes(data.ToString());
            return array;
        }
    }

    [GlobalType(Type = typeof(YawGamelinkGlobal), IsIndexed = false)]
    public class YawGamelinkPlugin : Plugin
    {
        public override string FriendlyName => "Yaw GameLink Plugin";
        public override object CreateGlobal() => new YawGamelinkGlobal(this);

        private CancellationTokenSource _cancellationTokenSource;


        UdpTelemetry<YawGLData> udp;
        public UdpTelemetryConfig Config;

        private YawGLData _data;

        Object lockObj = new object();

        public YawGLData Data { 
            get { return _data; } 
            set {
                lock (lockObj)
                {
                    _data = value;
                }
            } 
        }

        public YawGamelinkPlugin()
        {
            
        }

        /// <summary>
        /// Configure the UDP plugin with send and receive addresses and ports.
        /// </summary>
        /// <param name="receiveAddress">ipaddress:port</param>       
        public async Task Listen(string receiveAddress)
        {
            Config = new UdpTelemetryConfig(receiveAddress: receiveAddress) ;

            udp = new UdpTelemetry<YawGLData>(Config) { Convert = new YawGLByteConverter() };
            

            while(!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    Data = await udp.ReceiveAsync(_cancellationTokenSource.Token);

                    OnUpdate();

                }
                catch (Exception ex)
                {
                    // Handle exceptions
                }
            }
        }

        

        public override Action Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            return null;
        }

        

        public override void DoBeforeNextExecute()
        {
            // Handle UDP communication here
        }

        public override void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

    }

    [Global(Name = "yawvr")]
    public class YawGamelinkGlobal : UpdateblePluginGlobal<YawGamelinkPlugin>
    {
        public float yaw => plugin.Data.yaw;

        public float pitch => plugin.Data.pitch;

        public float roll => plugin.Data.roll;

        public byte amp => plugin.Data.amp;

        public byte hz => plugin.Data.hz;

        public byte fan => plugin.Data.fan;


        public void listen(string receiveAddress)
        {
            var t = plugin.Listen(receiveAddress);
        }

        public YawGamelinkGlobal(YawGamelinkPlugin plugin) : base(plugin)
        {
           
        }

        

    }
}
