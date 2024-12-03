using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VR
{
    public interface VRAPI
    {
        int Init();
        int Read(out OpenVrData output);
        bool Dispose();
        bool Center();
        void ConfigureInput(uint inputConfig);
        void ConfigureDebug(uint debugFlags);
        void TriggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude);
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
        private extern static int ovr_freepie_trigger_haptic_pulse(uint controllerIndex, float duration, float frequency, float amplitude);

        public int Init()
        {
            return ovr_freepie_init();
        }

        public int Read(out OpenVrData output)
        {
            return ovr_freepie_read(out output);
        }

        public bool Dispose()
        {
            return ovr_freepie_destroy() == 0;
        }

        public bool Center()
        {
            return ovr_freepie_reset_orientation() == 0;
        }

        public void ConfigureInput(uint inputConfig) { }
        public void ConfigureDebug(uint debugFlags) { }

        public void TriggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            ovr_freepie_trigger_haptic_pulse(controllerIndex, duration, frequency, amplitude);
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
        private extern static int ovr_freepie_trigger_haptic_pulse(uint controllerIndex, float duration, float frequency, float amplitude);

        public int Init()
        {
            return ovr_freepie_init();
        }

        public int Read(out OpenVrData output)
        {
            return ovr_freepie_read(out output);
        }

        public bool Dispose()
        {
            return ovr_freepie_destroy() == 0;
        }

        public bool Center()
        {
            return ovr_freepie_reset_orientation() == 0;
        }

        public void ConfigureInput(uint inputConfig) { }
        public void ConfigureDebug(uint debugFlags) { }

        public void TriggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            ovr_freepie_trigger_haptic_pulse(controllerIndex, duration, frequency, amplitude);
        }
    }

    public class OpenXRAPI : VRAPI
    {
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_init();
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_read(out OpenVrData output);
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_destroy();
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_reset_orientation();
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_configure_input(uint inputConfig);
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_configure_debug(uint debugFlags);
        [DllImport("OpenXRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_trigger_haptic_pulse(uint controllerIndex, float duration, float frequency, float amplitude);

        private string GetJSonPath()
        {
            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (pluginDir == null)
                return string.Empty;

            var exeDir = Directory.GetParent(pluginDir);
            if (exeDir == null)
                return string.Empty;

            return exeDir.FullName + "\\openxr-api-layer.json";
        }

        public int Init()
        {
            string jsonPath = GetJSonPath();

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException(jsonPath);
            }
            if (!string.IsNullOrEmpty(jsonPath))
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit", jsonPath, 0);
            }
            return ovr_freepie_init();
        }

        public int Read(out OpenVrData output)
        {
            return ovr_freepie_read(out output);
        }

        public bool Dispose()
        {
            string jsonPath = GetJSonPath();
            if (string.IsNullOrEmpty(jsonPath) == false)
            {
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit");
                    if (key != null)
                    {
                        key.DeleteValue(GetJSonPath());
                    }
                }
                catch
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit", jsonPath, 1);
                }
            }

            return ovr_freepie_destroy() == 0;
        }

        public bool Center()
        {
            return ovr_freepie_reset_orientation() == 0;
        }

        public void ConfigureInput(uint inputConfig)
        {
            ovr_freepie_configure_input(inputConfig);
        }
        public void ConfigureDebug(uint debugFlags)
        {
            ovr_freepie_configure_debug(debugFlags);
        }

        public void TriggerHapticPulse(uint controllerIndex, float duration, float frequency, float amplitude)
        {
            ovr_freepie_trigger_haptic_pulse(controllerIndex, duration, frequency, amplitude);
        }
    }
}
