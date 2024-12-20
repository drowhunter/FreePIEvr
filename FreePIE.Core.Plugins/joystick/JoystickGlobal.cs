using FreePIE.Core.Contracts;

using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FreePIE.Core.Plugins.joystick
{
    public class JoyProp
    {
        public string name; public JoystickOffset offset; public ObjectProperties props;
    }

    [GlobalEnum]
    public enum axisConfig
    {
        FullAxis,
        FullAxisInverted,
        HalfAxis,
        HalfAxisInverted,
        Raw,
    }

    public class JoyConfig
    {
        public axisConfig x { get; set; }
        public axisConfig y { get; set; }
        public axisConfig z { get; set; }
        public axisConfig rotationX { get; set; } 
        public axisConfig rotationY { get; set; } 
        public axisConfig rotationZ { get; set; } 
        public List<axisConfig> sliders { get; set; } = new List<axisConfig>();

    }

    public class JoyCount
    {
        public int povs { get; set; }
        public int buttons { get; set; }
        public int axes { get; set; }

        public int sliders { get; set; }
    }

    
    public class DpadGlobal
    {
        public bool up { get; set; }
        public bool down { get; set; }
        public bool left { get; set; }
        public bool right { get; set; }
        
        public DpadGlobal(int pov = -1)
        {
            if (pov >= 0)
            {
                if (pov >= 360)
                    pov = pov % 360;

                up = pov >= 315 || pov < 45;
                down = pov >= 135 && pov < 225;
                left = pov >= 225 && pov < 315;
                right = pov >= 45 && pov < 135;
            }
        }
    }


    [Global(Name = "joystick")]
    public class JoystickGlobal : IDisposable
    {

        private readonly Joystick _joystick;

        private JoystickState _state;
        
        private static Dictionary<axisConfig, (int min, int max)> _axisConfigValue = _axisConfigValue = new Dictionary<axisConfig, (int min, int max)>()
            {
                { axisConfig.FullAxis, (-1, 1) },
                { axisConfig.FullAxisInverted, (1, -1) },
                { axisConfig.HalfAxis, (0, 1) },
                { axisConfig.HalfAxisInverted, (1, 0) }
            }; 
        
        public JoystickGlobal(int index, Joystick joystick)
        {
            this._joystick = joystick;

            _state = joystick.GetCurrentState();

            Debug.WriteLine("Found {0} \"{1}\"", joystick.Information.Type, joystick.Information.ProductName);

            // Initialize Properties

            name = joystick.Information.ProductName;
            config = new JoyConfig() { sliders = _state.Sliders.Select(s => axisConfig.FullAxis).ToList() };

            buttons = new bool[joystick.Capabilities.ButtonCount];

            count = new JoyCount
            {
                povs = _joystick.Capabilities.PovCount,
                buttons = _joystick.Capabilities.ButtonCount,
                axes = _joystick.Capabilities.AxeCount,
                sliders = _state.Sliders.Length
            };

            if (count.povs > 0)
                _state.PointOfViewControllers.Take(count.povs).Select(p => p > 0 ? p / 100 : p).ToArray();
            else
                pov = new[] { -1, -1, -1, -1 };

            sliders = _state.Sliders.Select((s, i) => normalize(s, config.sliders[i])).ToArray();

        }

        

        
        internal void Update(params object[] args)
        {
            _state = _joystick.GetCurrentState();

            buttons = _state.Buttons.Take(count.buttons).ToArray();

            for (var i = 0; i < _state.Sliders.Length; i++)
            {
                sliders[i] = normalize(_state.Sliders[i], config.sliders[i]);
            }

            for (var i = 0; i < count.povs; i++)
            {
                pov[i] = _state.PointOfViewControllers[i] > 0 ? _state.PointOfViewControllers[i] / 100 : _state.PointOfViewControllers[i];
            }

            Updated?.Invoke(this, new JoystickEventArgs(_state));
        }

        

        #region Visible Global Properties

        public string name { get; private set; }

        public JoyCount count { get; private set; }


        public JoyConfig config { get; private set; }

        public event EventHandler<JoystickEventArgs> Updated;

        public event EventHandler Started, Stopped;


        public double x => normalize(_state?.X ?? 0, config.x );
        public double y => normalize(_state?.Y ?? 0, config.y);
        public double z => normalize(_state?.Z ?? 0, config.z);
        public double rotationX => normalize(_state.RotationX, config.rotationX);
        public double rotationY => normalize(_state.RotationY, config.rotationY);
        public double rotationZ => normalize(_state.RotationZ, config.rotationZ);                    

        public bool[] buttons { get; private set; }

        public double[] sliders { get; private set; }

        /// <summary>
        /// Returns -1 of no direction is pressed, otherwise returns the direction in degrees
        /// </summary>
        public int[] pov { get; private set; }
        

        public DpadGlobal[] dpad => pov.Select(p => new DpadGlobal(p)).ToArray();


        #endregion

        #region Private Methods
        private double mapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }

        private double ensureMapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return Math.Max(Math.Min(mapRange(x, xMin, xMax, yMin, yMax), Math.Max(yMin, yMax)), Math.Min(yMin, yMax));
        }

        
        private double normalize(int value, axisConfig cfg )
        {
            if (cfg == axisConfig.Raw) 
                return value;
            
            return ensureMapRange(value, 0, ushort.MaxValue, _axisConfigValue[cfg].min, _axisConfigValue[cfg].max);           
            
        }

        void IDisposable.Dispose()
        {
            _joystick?.Unacquire();
            _joystick?.Dispose();
            Stopped?.Invoke(this, null);
        }

        #endregion
    }
}
