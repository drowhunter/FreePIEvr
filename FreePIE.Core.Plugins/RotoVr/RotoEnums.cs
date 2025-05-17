using FreePIE.Core.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreePIE.Core.Plugins.RotoVr
{
    [GlobalEnum]
    public enum RotoDirection : byte
    {
        /// <summary>
        /// Rotate to the left (counterclockwise).
        /// </summary>
        Left = 0x00,

        /// <summary>
        /// Rotate to the right (clockwise).
        /// </summary>
        Right = 0x01,
    }

    [GlobalEnum]
    public enum RotoModeType : byte
    {
        /// <summary>
        /// The chair remains stationary and ignores all rotation commands.
        /// </summary>
        IdleMode = 0x00,

        /// <summary>
        /// Calibration mode. Used to reset or adjust the chair's default orientation.
        /// </summary>
        Calibration = 0x01,

        /// <summary>
        /// Head tracking mode. The chair automatically rotates to follow the user's headset orientation.
        /// </summary>
        HeadTrack = 0x02,

        /// <summary>
        /// Free movement mode. The user can manually rotate the chair without any angle restrictions.
        /// </summary>
        FreeMode = 0x03,

        /// <summary>
        /// Cockpit mode. The user can rotate the chair, but movement is restricted within predefined angle limits.
        /// </summary>
        CockpitMode = 0x04,

        /// <summary>
        /// Indicates that a mode switch has failed due to an error.
        /// </summary>
        //Error = 0x05,

        /// <summary>
        /// Follow Object mode. Uses <see cref="HeadTrack"/> internally.
        /// Allows the chair to follow the rotation of a specified GameObject in the scene.
        /// </summary>
        FollowObject = 0x06,

        /// <summary>
        /// Continuous mode. The chair continuously rotates based on Joystick Axis -1 to 1.
        /// </summary>
        JoystickMode = 0x07,
    }


    [GlobalEnum]
    public enum RotoMovementMode
    {
        Smooth,
        Jerky
        
    }

}
