using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.OculusVR
{
    public interface VRAPI
    {
        bool Init();
        void Read(out OpenVrData output);
        bool Dispose();
        bool Center();
        void TriggerHapticPulse(uint controllerIndex, uint durationMicroSec, float frequency, float amplitude);
    }

    public class OculusAPI : VRAPI
    {
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_init();
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_read(out OpenVrData output);
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_destroy();
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_reset_orientation();
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_trigger_haptic_pulse(uint controllerIndex, uint durationMicroSec, float frequency, float amplitude);

        public bool Init()
        {
            return ovr_freepie_init() == 0;
        }

        public void Read(out OpenVrData output)
        {
            ovr_freepie_read(out output);
        }

        public bool Dispose()
        {
            return ovr_freepie_destroy() == 0;
        }

        public bool Center()
        {
            return ovr_freepie_reset_orientation() == 0;
        }

        public void TriggerHapticPulse(uint controllerIndex, uint durationMicroSec, float frequency, float amplitude)
        {
            ovr_freepie_trigger_haptic_pulse(controllerIndex, durationMicroSec, frequency, amplitude);
        }
    }

    public class OpenVRAPI : VRAPI
    {
        [DllImport("OpenVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_init();
        [DllImport("OpenVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_read(out OpenVrData output);
        [DllImport("OpenVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_destroy();
        [DllImport("OpenVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_reset_orientation();
        [DllImport("OpenVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_trigger_haptic_pulse(uint controllerIndex, uint durationMicroSec, float frequency, float amplitude);

        public bool Init()
        {
            return ovr_freepie_init() == 0;
        }

        public void Read(out OpenVrData output)
        {
            ovr_freepie_read(out output);
        }

        public bool Dispose()
        {
            return ovr_freepie_destroy() == 0;
        }

        public bool Center()
        {
            return ovr_freepie_reset_orientation() == 0;
        }

        public void TriggerHapticPulse(uint controllerIndex, uint durationMicroSec, float frequency, float amplitude)
        {
            ovr_freepie_trigger_haptic_pulse(controllerIndex, durationMicroSec, frequency, amplitude);
        }
    }
}
