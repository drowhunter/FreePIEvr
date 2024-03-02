using System;
using System.Collections.Generic;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.OculusVR;

namespace FreePIE.Core.Plugins
{
    [GlobalType(Type = typeof(OpenVRGlobal))]
    public class OpenVRPlugin : Plugin
    {
        const string Oculus = "Oculus";
        const string OpenVR = "OpenVR";
        const string OpenXR = "OpenXR";

        private string m_vrEngine = OpenVR;
        private VRAPI m_vrAPI;

        public OpenVrData Data;

        public override object CreateGlobal()
        {
            return new OpenVRGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "Open VR"; }
        }

        public override bool GetProperty(int index, IPluginProperty property)
        {
            if (index > 0)
                return false;
            
            if (index == 0)
            {
                property.Name = "VREngine";
                property.Caption = "VR Engine";

                property.Choices.Add(Oculus, Oculus);
                property.Choices.Add(OpenVR, OpenVR);
                property.Choices.Add("OpenXR (experimental)", OpenXR);

                property.DefaultValue = OpenVR;
                property.HelpText = "Select the engine for acessing the VR Device";
            }

            return true;
        }

        public override bool SetProperties(Dictionary<string, object> properties)
        {
            m_vrEngine = (string)properties["VREngine"];

            return true;
        }

        public override Action Start()
        {
            switch (m_vrEngine)
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
                throw new Exception($"{m_vrEngine} SDK failed to init ({error})");

            return null;
        }

        public override void Stop()
        {
            m_vrAPI.Dispose();
        }

        public override void DoBeforeNextExecute()
        {
            int error = m_vrAPI.Read(out Data);
            //if (error != 0)
            //    throw new Exception($"Open VR SDK failed to update ({error})");

            OnUpdate();
        }

        public void Center()
        {
            m_vrAPI.Center();
        }

        public void TriggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            m_vrAPI.TriggerHapticPulse(controllerIndex, duration, frequency, amplitude);
        }
    }

    [Global(Name = "openVR")]
    public class OpenVRGlobal : UpdateblePluginGlobal<OpenVRPlugin>
    {
        public OpenVRGlobal(OpenVRPlugin plugin) : base(plugin) { }

        public OpenVr6Dof headPose => plugin.Data.HeadPose;
        public OpenVr6Dof leftTouchPose => plugin.Data.LeftTouchPose;
        public OpenVr6Dof rightTouchPose => plugin.Data.RightTouchPose;

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
        
        public void center()
        {
            plugin.Center();
        }

        public void triggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            plugin.TriggerHapticPulse(controllerIndex, duration, frequency, amplitude);
        }
    }
}
