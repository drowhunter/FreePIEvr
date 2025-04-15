using com.rotovr.sdk;

using FreePIE.Core.Common;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.RotoVr;

using System;
using System.Linq;

namespace FreePIE.Core.Plugins.RotoPlugin
{
    [GlobalType(Type = typeof(RotoPluginGlobal))]
    public class RotoPlugin : Plugin
    {
        public override string FriendlyName => "roto";

        public override object CreateGlobal() => new RotoPluginGlobal(this);


        public RotoBehaviour Roto;

        public RotoDataModel rotoDataModel;
        
        public ConnectionStatus connectionStatus = ConnectionStatus.Unknown;
        
        public ModeType Mode = default;

        public RotoPlugin()
        {
            
        }

        private void _roto_OnDataChanged(RotoDataModel obj)
        {
            rotoDataModel = obj;
            //Mode = (ModeType) Enum.Parse(typeof(ModeType),obj.Mode);
            if(Enum.TryParse(obj.Mode, out ModeType mode))
            {
                Mode = mode;
            }
            
            OnUpdate();
        }

        private void _roto_OnConnectionStatusChanged(ConnectionStatus obj)
        {
            connectionStatus = obj;
            OnUpdate();
        }

        private void _roto_OnModeChanged(ModeType obj)
        {
            Mode = obj;
            OnUpdate();
        }


        public override Action Start()
        {
            Roto = new RotoBehaviour();
            Roto.OnModeChanged += _roto_OnModeChanged;
            Roto.OnConnectionStatusChanged += _roto_OnConnectionStatusChanged;
            Roto.OnDataChanged += _roto_OnDataChanged;

            Roto.Connect();

            SwitchMode(RotoModeType.IdleMode);//, 0, 1, RotoMovementMode.Jerky);


            

            return null;
        }



        public override void Stop()
        {
            

            Roto.OnModeChanged -= _roto_OnModeChanged;
            Roto.OnConnectionStatusChanged -= _roto_OnConnectionStatusChanged;
            Roto.OnDataChanged -= _roto_OnDataChanged;

            // Cleanup the plugin
            Roto.Disconnect();

            Roto = null;
        }

        public override void DoBeforeNextExecute()
        {
            
        }

        public void SetPower(double power)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 30, 100);
            Roto.SetPower(RoundDouble(p));
        }


        /// <summary>
        /// Rumble the chair
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="power">value 0 - 1 </param>
        public void Rumble(double seconds, double power)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            Roto.Rumble((float)seconds, RoundDouble(p));
        }


        public void Rotate(double degrees, double power)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            var (d,a) = GetAngleDirection(degrees);

            Roto.Rotate(d, a, RoundDouble(p));
        }

        public void RotateTo(double degrees, double power)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            var d = GetAngleDirection(degrees);
            Roto.RotateToAngle(d.direction, d.angle, RoundDouble(p));
        }

        public void RotateClosest(double degrees, double power)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            
            Roto.RotateToClosestAngleDirection(RoundDouble(degrees), RoundDouble(p));
        }

        public void SwitchMode(RotoModeType mode, Func<float> targetFunc = null)//, double limit, double power, RotoMovementMode movementMode)
        {            
            //var l = RoundDouble(Maths.EnsureMapRange(limit, 0, 1, 60, 140));
            //var p = RoundDouble( Maths.EnsureMapRange(power, 0, 1, 30, 100));

            var m = (ModeType)(byte)mode;

            if (mode != RotoModeType.HeadTrack)
            {
                Roto.SwitchMode(m, targetFunc);//, new ModeParams { CockpitAngleLimit = l, MaxPower = p, MovementMode = (MovementMode)(byte)movementMode });               
            }
            else //if(m == ModeType.HeadTrack)
            {
                Roto.roto.SetMode(m, new ModeParams { MaxPower = 100 });
            }
        }



        public void SetToZero()
        {
            Roto.Calibration(CalibrationMode.SetToZero);
        }

        
        private int RoundDouble(double value)
        {
            return (int)Math.Round(value, 0);
        }

        private (Direction direction, int angle) GetAngleDirection(double degrees)
        {
            return (degrees < 0  ? Direction.Left : Direction.Right, Math.Abs(RoundDouble(degrees)));
        }
    }


    [Global(Name = "roto")]
    public class RotoPluginGlobal : UpdateblePluginGlobal<RotoPlugin>
    {
        public double angle => plugin.rotoDataModel?.Angle ?? 0;

        public string mode => plugin.Mode.ToString();

        public int angleLimit => plugin.rotoDataModel?.TargetCockpit ?? 0;          

        public int maxPower => plugin.rotoDataModel?.MaxPower ?? 0;

        public string connectionStatus => plugin.connectionStatus.ToString();


        public void rumble(double seconds, double power = 1) => plugin.Rumble(seconds, power);

        public void rotate(double degrees, double power = 1) => plugin.Rotate(degrees, power);

        public void rotateTo(double degrees, double power = 1) => plugin.RotateTo(degrees, power);

        public void rotateClosest(double degrees, double power = 1) => plugin.RotateClosest(degrees, power);

        public void switchMode(RotoModeType mode, Func<float> targetFunc = null)// double limit = 1, double maxPower = 0, RotoMovementMode movementMode = RotoMovementMode.Smooth) 
            => plugin.SwitchMode(mode, targetFunc );// limit, maxPower, movementMode);

        public void setPower(double power = .5) => plugin.SetPower(power);

        public void setToZero() => plugin.SetToZero();

        public RotoPluginGlobal(RotoPlugin plugin) : base(plugin)
        {
        }


        
    }
}
