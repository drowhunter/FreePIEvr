using FreePIE.Core.Common;
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
        public float pitch;//-180 to 180
        public float roll; // -180 to 180
        public float amp; //0-254
        public float hz; // if hz == 0 amp = 0
        public float fan;

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

    public class YawGLByteConverter : IByteConvertor<YawGLData>
    {
        static Regex rot = new Regex($@"Y\[(?<yaw>-?[\d.]+)\]P\[(?<pitch>-?[\d.]+)\]R\[(?<roll>-?[\d.]+)\]");

        static Regex vibes = new Regex($@"V\[(?<amp>\d+?),\d*?,\d*?,(?<hz>\d*?)\]");

        static Regex fan = new Regex($@"F\[(?<fan>\d+?)");

        static CultureInfo c = CultureInfo.InvariantCulture;

        private float FullCircle(float degrees) => (degrees + 360) % 360;   
        
        private float FullCircle2(float degrees)
        {
            if (degrees < 0)
            {
                degrees += 360;
            }

            return degrees;
        }

        public YawGLData FromBytes(byte[] data)
        {
            var dataString = Encoding.ASCII.GetString(data);
            var yawGLData = new YawGLData();

            try
            {
                var r = rot.Match(dataString);
                if (r.Success)
                {
                    yawGLData.yaw = FullCircle(float.Parse(r.Groups["yaw"].Value, c));      //-180-180
                    yawGLData.pitch = FullCircle(float.Parse(r.Groups["pitch"].Value, c));  //-180-180
                    yawGLData.roll = FullCircle(float.Parse(r.Groups["roll"].Value, c));    //-180-180
                }

                var v = vibes.Match(dataString);
                if (v.Success)
                {
                    yawGLData.amp = byte.Parse(v.Groups["amp"].Value, c)/ byte.MaxValue;
                    yawGLData.hz = byte.Parse(v.Groups["hz"].Value, c)/ byte.MaxValue;
                }

                var f = fan.Match(dataString);
                if (f.Success)
                {
                    yawGLData.fan = byte.Parse(f.Groups["fan"].Value, c) / byte.MaxValue;
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
            udp?.Dispose();
        }

    }

    [Global(Name = "gamelink")]
    public class YawGamelinkGlobal : UpdateblePluginGlobal<YawGamelinkPlugin>
    {
        public float yaw => plugin.Data.yaw;

        public float pitch => plugin.Data.pitch;

        public float roll => plugin.Data.roll;

        public float amp => plugin.Data.amp;

        public float hz => plugin.Data.hz;

        public float fan => plugin.Data.fan;


        public void listen(string receiveAddress)
        {
            var t = plugin.Listen(receiveAddress);
        }

        public YawGamelinkGlobal(YawGamelinkPlugin plugin) : base(plugin)
        {
           
        }

        

    }
}
