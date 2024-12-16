﻿using FreePIE.Core.Contracts;

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

    public enum AxisConfig
    {
        FullAxis,
        FullAxisInverted,
        HalfAxis,
        HalfAxisInverted,
        Raw,
    }

    public class JoyConfig
    {
        public AxisConfig x { get; set; }
        public AxisConfig y { get; set; }
        public AxisConfig z { get; set; }
        public AxisConfig rotationX { get; set; } 
        public AxisConfig rotationY { get; set; } 
        public AxisConfig rotationZ { get; set; } 
        public List<AxisConfig> sliders { get; set; } = new List<AxisConfig>();

    }

    public class JoyCount
    {
        public int povs { get; set; }
        public int buttons { get; set; }
        public int axes { get; set; }

        public int sliders { get; set; }
    }


    [Global(Name = "joystick")]
    public class JoystickGlobal : IDisposable
    {

        private readonly Joystick _joystick;

        private JoystickState _state;
        
        //public JoystickState state => _state;
        
        //private Capabilities _caps => _joystick.Capabilities;

        //private int _numAxes => _joystick.Capabilities.AxeCount;

        //private int _numPovs => _joystick.Capabilities.PovCount;

        private Dictionary<AxisConfig, (int min, int max)> _axisConfigValue;

        //private static List<JoystickOffset> _joystickAxisOffsets = new List<JoystickOffset>() { JoystickOffset.X, JoystickOffset.Y, JoystickOffset.Z, JoystickOffset.RotationX, JoystickOffset.RotationY, JoystickOffset.RotationZ, JoystickOffset.Sliders0, JoystickOffset.Sliders1 };
        //private Dictionary<JoystickOffset, JoyProp> _axisProperties { get; } = new Dictionary<JoystickOffset, JoyProp>();

        public JoystickGlobal(int index, Joystick joystick)
        {
            this._joystick = joystick;

            _state = joystick.GetCurrentState();

            Debug.WriteLine("Found {0} \"{1}\"", joystick.Information.Type, joystick.Information.ProductName);
            config = new JoyConfig() { sliders = _state.Sliders.Select(s => AxisConfig.FullAxis).ToList() };

            buttons = new bool[joystick.Capabilities.ButtonCount];

            _axisConfigValue = new Dictionary<AxisConfig, (int min, int max)>()
            {
                { AxisConfig.FullAxis, (-1, 1) },
                { AxisConfig.FullAxisInverted, (1, -1) },
                { AxisConfig.HalfAxis, (0, 1) },
                { AxisConfig.HalfAxisInverted, (1, 0) }
            };

            

            /*foreach(var offset in _joystickAxisOffsets)
            {
                try
                {
                    var info = joystick.GetObjectInfoByName(offset.ToString());
                    _axisProperties.Add(offset, new JoyProp { offset = offset, name = info.Name, props = joystick.GetObjectPropertiesById(info.ObjectId) });
                }
                catch { }
            }
            */

        }
        
        internal void Update(params object[] args)
        {
            _state = _joystick.GetCurrentState();

            //count.sliders = new int[][] { _state.Sliders, _state.AccelerationSliders, _state.VelocitySliders, _state.ForceSliders }.Select(sliders => sliders.Length).Sum();

            buttons = _state.Buttons.Take(count.buttons).ToArray();

            Updated?.Invoke(this, new JoystickEventArgs(_state));
        }

        

        #region Visible Global Properties

        public string name { get { return _joystick?.Information.ProductName ?? ""; } }

        public JoyCount count => new JoyCount
            {
                povs = _joystick.Capabilities.PovCount,
                buttons = _joystick.Capabilities.ButtonCount,
                axes = _joystick.Capabilities.AxeCount,
                sliders = _state.Sliders.Length//.Where(s => s != 0 ).Count()
            };


        public JoyConfig config;

        /// <summary>
        /// Events that are triggered when the joystick is updated
        /// </summary>
        public event EventHandler<JoystickEventArgs> Updated;

        public event EventHandler Started, Stopped;


        public double x => normalize(_state?.X??0, config.x );
        public double y => normalize(_state?.Y ?? 0, config.y);
        public double z => normalize(_state?.Z ?? 0, config.z);
        public double rotationX => normalize(_state?.RotationX ?? 0, config.rotationX);
        public double rotationY => normalize(_state?.RotationY ?? 0, config.rotationY);
        public double rotationZ => normalize(_state?.RotationZ ?? 0, config.rotationZ); 
            
        

        public bool[] buttons { get; private set; }

        public double[] sliders
        {
            get { return _state.Sliders.Take(count.sliders).Select((s,i) => normalize(s, config.sliders[i])).ToArray(); }
        }

        public int[] pov
        {
            get
            {
                return _joystick.Capabilities.PovCount > 0 ? _state.PointOfViewControllers.Take(count.povs).Select(p =>
                {
                    var r = p > 0 ? p / 100 : p;
                    return r;
                }).ToArray() : new[] { -1, -1, -1, -1 };
            }
        }

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

        
        private double normalize(int value, AxisConfig cfg )
        {
            if(cfg == AxisConfig.Raw) 
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
