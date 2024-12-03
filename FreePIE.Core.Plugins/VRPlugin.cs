using System;
using System.Collections.Generic;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.VR;

namespace FreePIE.Core.Plugins
{
    [GlobalType(Type = typeof(VRGlobal))]
    public class VRPlugin : Plugin
    {
        const string Oculus = "Oculus";
        const string OpenVR = "OpenVR";
        const string OpenXR = "OpenXR";

        private string m_vrRuntime = OpenVR;
        private VRAPI m_vrAPI;
        private bool m_invertZ = false;

        public OpenVrData Data;

        public override object CreateGlobal()
        {
            return new VRGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "VR"; }
        }

        public string Runtime => m_vrRuntime;

        public override bool GetProperty(int index, IPluginProperty property)
        {
            if (index > 0)
                return false;
            
            if (index == 0)
            {
                property.Name = "VRRuntime";
                property.Caption = "VR Runtime";

                property.Choices.Add(Oculus, Oculus);
                property.Choices.Add(OpenVR, OpenVR);
                property.Choices.Add("OpenXR (experimental)", OpenXR);

                property.DefaultValue = OpenVR;
                property.HelpText = "Select the runtime for acessing the VR Device";
            }

            return true;
        }

        public override bool SetProperties(Dictionary<string, object> properties)
        {
            m_vrRuntime = (string)properties["VRRuntime"];

            return true;
        }

        public override Action Start()
        {
            switch (m_vrRuntime)
            {
                case Oculus:
                    m_vrAPI = new OculusAPI();
                    break;
                case OpenXR:
                    m_vrAPI = new OpenXRAPI();
                    break;
                default:            
                    m_vrAPI = new OpenVRAPI();
                    break;
            }

            int error = m_vrAPI.Init();
            if (error != 0)
                throw new Exception($"{m_vrRuntime} SDK failed to init ({error})");

            return null;
        }

        public override void Stop()
        {
            m_vrAPI.Dispose();
        }

        public override void DoBeforeNextExecute()
        {
            int error = m_vrAPI.Read(out Data);

            if (m_invertZ)
            {
                Data.HeadPose.position.z = -Data.HeadPose.position.z;
                Data.LeftTouchPose.position.z = -Data.LeftTouchPose.position.z;
                Data.RightTouchPose.position.z = -Data.RightTouchPose.position.z;
            }

            OnUpdate();
        }

        public void Center()
        {
            m_vrAPI.Center();
        }
        

        public void ConfigureInput(uint inputConfig)
        {
            m_vrAPI.ConfigureInput(inputConfig);
        }
        public void ConfigureDebug(uint debugFlags)
        {
            m_vrAPI.ConfigureDebug(debugFlags);
        }

        public void InvertZ(bool inverted)
        {
            m_invertZ = inverted;
        }

        public void TriggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            m_vrAPI.TriggerHapticPulse(controllerIndex, duration, frequency, amplitude);
        }
    }

    public class Vr6DofGlobal
    {
        private const float RadToDeg = 57.2958f;

        public Vectorf left;
        public Vectorf up;
        public Vectorf forward;
        public Vectorf position;

        public float x => position.x;
        public float y => position.y;
        public float z => position.z;

        /// <summary>
        /// the pitch in degrees
        /// </summary>
        public float pitch => (float)Math.Asin(forward.y) * RadToDeg;

        /// <summary>
        /// the yaw in degrees
        /// </summary>
        public float yaw => (float)Math.Atan2(left.z, left.x) * RadToDeg;

        /// <summary>
        /// the roll in degrees
        /// </summary>
        public float roll
        {
            get
            {
                var yaw = Math.Atan2(forward.z, forward.x);
                var planeRightX = Math.Sin(yaw);
                var planeRightZ = -Math.Cos(yaw);
                return (float)Math.Asin(Math.Max(-1, Math.Min(1, up.x * planeRightX + up.z * planeRightZ))) * RadToDeg;
            }
        }

        public static implicit operator Vr6DofGlobal(Vr6Dof obj)
        {
            return new Vr6DofGlobal { 
                left = obj.left,
                up = obj.up,
                forward = obj.forward,
                position = obj.position,

            };
        }
    }

    [Global(Name = "vr")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public class VRGlobal : UpdateblePluginGlobal<VRPlugin>
    {
        public VRGlobal(VRPlugin plugin) : base(plugin) { }

        public Vr6DofGlobal headPose => plugin.Data.HeadPose;
        public Vr6DofGlobal leftTouchPose => plugin.Data.LeftTouchPose;
        public Vr6DofGlobal rightTouchPose => plugin.Data.RightTouchPose;

        public string runtime => plugin.Runtime;

        public uint headStatus => plugin.Data.HeadStatus;
        public uint leftTouchStatus => plugin.Data.LeftTouchStatus;
        public uint rightTouchStatus => plugin.Data.RightTouchStatus;

        public bool isMounted => plugin.Data.IsHmdMounted > 0;
        public bool isHeadTracking => plugin.Data.HeadStatus > 1;
        public bool isLeftTouchTracking => plugin.Data.LeftTouchStatus > 1;
        public bool isRightTouchTracking => plugin.Data.RightTouchStatus > 1;

        public float leftTrigger => plugin.Data.LeftTrigger;
        public float rightTrigger => plugin.Data.RightTrigger;

        public float leftGrip => plugin.Data.LeftGrip;
        public float rightGrip => plugin.Data.RightGrip;

        public Pointf leftStickAxes => plugin.Data.LeftStickAxes;
        public Pointf rightStickAxes => plugin.Data.RightStickAxes;

        public float a => plugin.Data.A;
        public float b => plugin.Data.B;
        public float x => plugin.Data.X;
        public float y => plugin.Data.Y;
        public float leftStick => plugin.Data.LeftStick;
        public float rightStick => plugin.Data.RightStick;
        
        public float leftThumbRest => plugin.Data.LeftThumbRest;
        public float rightThumbRest => plugin.Data.RightThumbRest;

        public void center() => plugin.Center();

        public void configureInput(uint inputConfig) => plugin.ConfigureInput(inputConfig);

        public void configureDebug(uint debugFlags)
        {
            plugin.ConfigureDebug(debugFlags);
        }

        public void invertZ(bool inverted)
        {
            plugin.InvertZ(inverted);
        }

        public void triggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            plugin.TriggerHapticPulse(controllerIndex, duration, frequency, amplitude);
        }
    }
}
