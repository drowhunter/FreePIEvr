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

        private int? zeroAngle = null;

        private int? ZeroAngle
        {
            get => zeroAngle;            
            set
            {
                if(zeroAngle == null)
                    zeroAngle = value;
            }
        }

        public int Angle
        {
            get
            {
                if (rotoDataModel == null)
                    return 0;
                var a = (rotoDataModel.Angle + (ZeroAngle ?? 0));
                if (a < 0)
                    a += 360;
                return a;
            }
        }

        public RotoPlugin()
        {
            
        }

        private void _roto_OnDataChanged(RotoDataModel obj)
        {
            if(Mode == ModeType.FollowObject && ZeroAngle == null)
            {
                ZeroAngle = -obj.Angle;
            }
            
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
            //Roto.ConnectionType = ConnectionType.Simulation;
            Roto.OnModeChanged += _roto_OnModeChanged;
            Roto.OnConnectionStatusChanged += _roto_OnConnectionStatusChanged;
            Roto.OnDataChanged += _roto_OnDataChanged;

            Roto.Connect();

            SwitchMode(RotoModeType.IdleMode);//, 0, 1, RotoMovementMode.Jerky);

            SetPower(1);
            

            return null;
        }



        public override void Stop()
        {
            Roto.SwitchMode(ModeType.IdleMode);

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

        public void SetPower(double power = 1)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 30, 100);
            Roto.SetPower(RoundDouble(p));
        }


        /// <summary>
        /// Rumble the chair
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="power">value 0 - 1 </param>
        public void Rumble(double seconds, double power = 1)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            Roto.Rumble((float)seconds, RoundDouble(p));
        }


        public void Rotate(double degrees, double power = 1)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            var (d,a) = GetAngleDirection(degrees);

            Roto.Rotate(d, a, RoundDouble(p));
        }

        public void RotateTo(RotoDirection direction, double degrees, double power = 1)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            
            Roto.RotateToAngle(direction == RotoDirection.Left ? Direction.Left : Direction.Right, RoundDouble(Ensure360(degrees)), RoundDouble(p));
        }

        public void RotateClosest(double degrees, double power = 1)
        {
            var p = Maths.EnsureMapRange(power, 0, 1, 0, 100);
            
            Roto.RotateToClosestAngleDirection(RoundDouble(Ensure360(degrees)), RoundDouble(p));
        }

        public void SwitchMode(RotoModeType mode, Func<float?> targetFunc = null)//, double limit, double power, RotoMovementMode movementMode)
        {            
            //var l = RoundDouble(Maths.EnsureMapRange(limit, 0, 1, 60, 140));
            //var p = RoundDouble( Maths.EnsureMapRange(power, 0, 1, 30, 100));

            var m = (ModeType)(byte)mode;

            if (mode == RotoModeType.FollowObject )
            {
                Roto.SwitchMode(m, new ModeParams { CockpitAngleLimit = 0, MaxPower = 100 }, targetFunc);               
            }
            else //if(m == ModeType.HeadTrack)
            {
                Roto.roto.SetMode(m, new ModeParams { MaxPower = 100 });
                //SetPower(1);
            }
        }

        public double Ensure360(double degrees)
        {
            var a = Math.Sign(degrees) * (degrees % 360);
            if(a < 0)
            {
                a += 360;
            }
            return a;
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
            var d = degrees < 0 ? Direction.Left : Direction.Right;

            var ang = RoundDouble(Ensure360(degrees));

            return (d, ang);
        }
    }


    [Global(Name = "roto")]
    public class RotoPluginGlobal : UpdateblePluginGlobal<RotoPlugin>
    {
        public double rawAngle => plugin.rotoDataModel?.Angle ?? 0;
        public double angle => plugin.Angle;


        public string mode => plugin.Mode.ToString();

        public int angleLimit => plugin.rotoDataModel?.TargetCockpit ?? 0;          

        public int maxPower => plugin.rotoDataModel?.MaxPower ?? 0;

        public string connectionStatus => plugin.connectionStatus.ToString();


        public void rumble(double seconds, double power = 1) => plugin.Rumble(seconds, power);

        public void rotate(double degrees, double power = 1) => plugin.Rotate(degrees, power);

        public void rotateTo(RotoDirection direction, double degrees, double power = 1) => plugin.RotateTo(direction, degrees, power);

        public void rotateClosest(double degrees, double power = 1) => plugin.RotateClosest(degrees, power);

        public void switchMode(RotoModeType mode, Func<float?> targetFunc = null) 
            => plugin.SwitchMode(mode, targetFunc );

        public void setPower(double power = .5) => plugin.SetPower(power);

        //public void setToZero() => plugin.SetToZero();


        public RotoPluginGlobal(RotoPlugin plugin) : base(plugin)
        {
        }


        
    }
}
