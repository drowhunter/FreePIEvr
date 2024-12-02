using SharpDX.DirectInput;

using System;
//using SharPieEngine.Helpers;
//using SharPieEngine.Parts;

namespace FreePIE.Core.Plugins.joystick
{
    public class JoystickEventArgs : EventArgs
    {
        public JoystickState State { get; }

        public JoystickEventArgs(JoystickState state)
        {
            this.State = state;
        }
    }
}
