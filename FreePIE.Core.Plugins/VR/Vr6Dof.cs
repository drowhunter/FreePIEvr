using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VR
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vectorf
    {
        public float x, y, z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vr6Dof
    {
        public Vectorf left;
        public Vectorf up;
        public Vectorf forward;
        public Vectorf position;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Pointf
    {
        public float x, y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OpenVrData
    {
        public Vr6Dof HeadPose;
        public Vr6Dof LeftTouchPose;
        public Vr6Dof RightTouchPose;

        public float LeftTrigger;
        public float RightTrigger;

        public float LeftGrip;
        public float RightGrip;

        public Pointf LeftStickAxes;
        public Pointf RightStickAxes;

        public float LeftStick;
        public float RightStick;

        public float A;
        public float B;
        public float X;
        public float Y;

        public float LeftThumbRest;
        public float RightThumbRest;

        public uint HeadStatus;
        public uint LeftTouchStatus;
        public uint RightTouchStatus;

        public uint IsHmdMounted;
    }
}
