using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.Globals;

using SharpDX;
using SharpDX.DirectInput;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FreePIE.Core.Plugins.joystick
{
    [GlobalType(Type = typeof(JoystickGlobal), IsIndexed = true)]
    public class JoystickPlugin : Plugin
    {
        private List<JoystickGlobal> globals = new List<JoystickGlobal>();

        private DirectInput _directInput;
        protected DirectInput directInput
        {
            get
            {
                if (_directInput == null)
                {
                    _directInput = new DirectInput();
                }
                return _directInput;
            }
        }

        private IList<DeviceInstance> _devices;
        private IntPtr _hWnd;

        public int DeviceCount { get { return _devices.Count; } }

        public override string FriendlyName
        {
            get { return "Joystick"; }
        }

        public override object CreateGlobal()
        {
            _hWnd = Process.GetCurrentProcess().MainWindowHandle;

            _devices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

            var creator = new Func<int, DeviceInstance, JoystickGlobal>((idx, d) =>
            {
                try
                {
                    var joystick = new Joystick(directInput, d.InstanceGuid);
                    joystick.SetCooperativeLevel(_hWnd, CooperativeLevel.Exclusive | CooperativeLevel.Background);

                    joystick.Acquire();

                    var g = new JoystickGlobal(idx, joystick);
                    globals.Add(g);
                    return g;
                }
                catch (SharpDXException)
                {
                    return null;
                }
            });

            return new GlobalIndexer<JoystickGlobal, int, string>(
                (int intIndex) => creator(intIndex, _devices[intIndex]),
                (string strIndex, int idx) =>
                {
                    var d = _devices.Where(di => di.InstanceName.Equals(strIndex,StringComparison.InvariantCultureIgnoreCase)).ToArray();
                    if (d.Length > 0)
                    {
                        return creator(idx, d[idx]);
                    }
                    return null;
                }
            );
        }

        public override Action Start()
        {
            
            return null;
        }

        public override void DoBeforeNextExecute()
        {
            globals.ForEach(d => d?.Update());
        }

        public override void Stop()
        {
            foreach (var j in globals)
            {
                
                j.Dispose();
                
            }

            if (_directInput != null && !_directInput.IsDisposed)
            {
                _directInput.Dispose();
                _directInput = null;
            }
        }
    }
}
