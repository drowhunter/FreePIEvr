using FreePIE.Core.Contracts;

using SharpDX.DirectInput;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FreePIE.Core.Plugins.joystick
{
    internal class JoyProp
    {
        public string name; public bool isHalfAxis; public int min; public int max;
    }

    [Global(Name = "joystick")]
    public class JoystickGlobal : IDisposable
    {

        private readonly Joystick _joystick;

        private JoystickState state;
        public JoystickState State
        {
            get { return state ?? (state = _joystick.GetCurrentState()); }
        }


        
        public event EventHandler<JoystickEventArgs> Updated;
        public event EventHandler Started, Stopped;

        public Capabilities caps { get { return _joystick.Capabilities; } }

        
        public int ButtonCount { get { return _joystick.Capabilities.ButtonCount; } }

        public int AxesCount { get { return _joystick.Capabilities.AxeCount; } }

        public int PovCount { get { return _joystick.Capabilities.PovCount; } }

        private Dictionary<string, JoyProp> AxisInfo { get; } 

        public JoystickGlobal(int index, Joystick joystick)
        {
            this._joystick = joystick;

            Debug.WriteLine("Found {0} \"{1}\"", joystick.Information.Type, joystick.Information.ProductName);

            buttons = new bool[ButtonCount];

            var objProps = joystick.GetObjects().Where(o => (o.ObjectId.Flags & DeviceObjectTypeFlags.NoData) == 0) .ToDictionary(o => o.ObjectId.ToString(), o => (name: o.Name, flags: o.ObjectId.Flags, props: joystick.GetObjectPropertiesById(o.ObjectId)));

            AxisInfo = objProps.Where(f => (f.Value.flags & DeviceObjectTypeFlags.Axis) != 0).ToDictionary(o => o.Value.name, v => new JoyProp { isHalfAxis = v.Value.props.LogicalRange.Minimum == 0,min = v.Value.props.Range.Minimum, max = v.Value.props.Range.Maximum, name = v.Value.name });           

        }

        
        public void Dispose()
        {
            _joystick.Unacquire();
            _joystick.Dispose();
            Stopped?.Invoke(this, null);
        }


        
        

        public string JoystickName { get { return _joystick?.Information.ProductName ?? ""; } }


        
        internal void Update(params object[] args)
        {
            state = _joystick.GetCurrentState();

            buttons = state.Buttons.Take(_joystick.Capabilities.ButtonCount).ToArray();

            Updated?.Invoke(this, new JoystickEventArgs(state));
        }

        private float ensureMapRange(float x, float xMin, float xMax, float yMin, float yMax)
        {
            return Math.Max(Math.Min(((x - xMin) / (xMax - xMin)) * (yMax - yMin) + yMin, yMax), yMin);
        }

        #region stuff




        public float x
        {
            get 
            {
                return todouble(State.X, "X Axis");
            }
        }

        public float y
        {
            get { return todouble(State.Y, "Y Axis"); }
        }

        public float z
        {
            get { return todouble(State.Z, "Z Axis"); }
        }

        public float rotationX
        {
            get { return todouble(State.RotationX, "X Rotation"); }
        }

        public float rotationY
        {
            get { return todouble(State.RotationY, "Y Rotation"); }
        }

        public float rotationZ
        {
            get { return todouble(State.RotationZ, "Z Rotation"); }
        }

        

        public bool[] buttons { get; private set; }

        public float[] sliders
        {
            get { return State.Sliders.Select(s => todouble(s, "Slider")).ToArray(); }
        }

        public int[] pov
        {
            get
            {
                return PovCount > 0 ? State.PointOfViewControllers.Select(p =>
                {
                    var r = p > 0 ? p / 100 : p;
                    return r;
                }).ToArray() : new[] { -1, -1, -1, -1 };
            }
        }

        private float todouble(int value, string axis )
        {
            
            if (AxisInfo.TryGetValue(axis, out JoyProp xprop))
            {
                return ensureMapRange(value, xprop.min, xprop.max, xprop.isHalfAxis ? 0 : -1, 1);
            }
            return (value * 1.0f);
        }
        #endregion
    }
}
