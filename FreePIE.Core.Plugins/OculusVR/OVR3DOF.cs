﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FreePIE.Core.Plugins.OculusVR
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OculusVr3Dof
    {
        public float Yaw;
        public float Pitch;
        public float Roll;
    }
}
