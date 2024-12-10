using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FreePIE.Core.Contracts;
using SharpDX.DirectInput;
using FreePIE.Core.Plugins.Strategies;

namespace FreePIE.Core.Plugins
{
    
    

    [GlobalType(Type = typeof (KeyboardGlobal))]
    public class KeyboardPlugin : Plugin
    {
        

        // Maps SharpDX key codes to dwFlag ExtendedKeyMap
        private HashSet<ushort> extendedKeyMap = new HashSet<ushort>() { 
            39, 44, 46, 48, 49, 50, 51, 70, 71, 76, 79, 80, 81, 82, 
            84, 85, 86, 100, 105, 108, 109, 110, 112, 113, 114, 116, 
            118, 119, 121, 125, 127, 128, 132, 133, 134, 135, 136, 
            137, 138, 139, 140, 141, 142, 143 };

        private DirectInput DirectInputInstance = new DirectInput();
        private Keyboard KeyboardDevice;
        private KeyboardState KeyState = new KeyboardState();
        private bool[] MyKeyDown = new bool[150];
        private SetPressedStrategy<ushort> setKeyPressedStrategy;
        private GetPressedStrategy<ushort> getKeyPressedStrategy;

        public override object CreateGlobal()
        {
            return new KeyboardGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "Keyboard"; }
        }

        public override Action Start()
        {

            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

            KeyboardDevice = new Keyboard(DirectInputInstance);
            if (KeyboardDevice == null)
                throw new Exception("Failed to create keyboard device");

            KeyboardDevice.SetCooperativeLevel(handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
            KeyboardDevice.Acquire();

            KeyboardDevice.GetCurrentState(ref KeyState);

            setKeyPressedStrategy = new SetPressedStrategy<ushort>(SendKeyDown, SendKeyUp);
            getKeyPressedStrategy = new GetPressedStrategy<ushort>(IsKeyDown);

            OnStarted(this, new EventArgs());
            return null;
        }

        public override void Stop()
        {
            // Don't leave any keys pressed
            for (ushort i = 0; i < MyKeyDown.Length; i++)
            {
                if (MyKeyDown[i])
                    SendKeyUp(i);
            }

            if (KeyboardDevice != null)
            {
                KeyboardDevice.Unacquire();
                KeyboardDevice.Dispose();
                KeyboardDevice = null;
            }

            if (DirectInputInstance != null)
            {
                DirectInputInstance.Dispose();
                DirectInputInstance = null;
            }
        }

        public override bool GetProperty(int index, IPluginProperty property)
        {
            return false;
        }

        public override bool SetProperties(Dictionary<string, object> properties)
        {
            return false;
        }

        public override void DoBeforeNextExecute()
        {
            KeyboardDevice.GetCurrentState(ref KeyState);
            setKeyPressedStrategy.Do();
        }

        public bool IsKeyDown(ushort keycode)
        {
            // Returns true if the key is currently being pressed
            bool down = KeyState.IsPressed((SharpDX.DirectInput.Key)keycode) || MyKeyDown[keycode];
            return down;
        }

        public bool IsKeyUp(ushort keycode) => !IsKeyDown(keycode);

        public bool WasKeyPressed(ushort key)
        {
            return getKeyPressedStrategy.IsPressed(key);
        }

        private MouseKeyIO.KEYBDINPUT KeyInput(ushort code, uint flag)
        {
            var i = new MouseKeyIO.KEYBDINPUT();
            i.wVk = 0;
            i.wScan = code;
            i.time = 0;
            i.dwExtraInfo = IntPtr.Zero;
            i.dwFlags = flag | MouseKeyIO.KEYEVENTF_SCANCODE;
            return i;
        }

        public void SendKeyDown(ushort code)
        {

            if (!MyKeyDown[code])
            {
                MyKeyDown[code] = true;
                
                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_KEYBOARD;
                input[0].ki = KeyInput(code, extendedKeyMap.Contains(code) ? MouseKeyIO.KEYEVENTF_EXTENDEDKEY : 0);

                MouseKeyIO.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
            }
        }

        public void SendKeyUp(ushort code)
        {

            if (MyKeyDown[code])
            {                
                MyKeyDown[code] = false;                

                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_KEYBOARD;
                if (extendedKeyMap.Contains(code))
                    input[0].ki = KeyInput(code, MouseKeyIO.KEYEVENTF_EXTENDEDKEY | MouseKeyIO.KEYEVENTF_KEYUP);
                else
                    input[0].ki = KeyInput(code, MouseKeyIO.KEYEVENTF_KEYUP);

                MouseKeyIO.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

            }
        }

        public void PressAndRelease(ushort keycode)
        {
            setKeyPressedStrategy.Add(keycode);
        }

        public void PressAndRelease(ushort keycode, bool state)
        {
            setKeyPressedStrategy.Add(keycode, state);
        }
    }

    [Global(Name = "keyboard")]
    public class KeyboardGlobal 
    {
        private readonly KeyboardPlugin plugin;

        public KeyboardGlobal(KeyboardPlugin plugin)
        {
            this.plugin = plugin;
        }

        protected bool getKeyDown(Key key)
        {
            return plugin.IsKeyDown((ushort)key);
        }

        public void setKeyDown(Key key)
        {
            plugin.SendKeyDown((ushort)key);
        }

        public bool getKeyUp(Key key)
        {
            return plugin.IsKeyUp((ushort)key);
        }

        public void setKeyUp(Key key)
        {
            plugin.SendKeyUp((ushort)key);
        }

        protected void setKey(Key key, bool down)
        {
            if (down)
                plugin.SendKeyDown((ushort)key);
            else
                plugin.SendKeyUp((ushort)key);
        }

        public bool getPressed(Key key)
        {
            return plugin.WasKeyPressed((ushort)key);
        }

        public void setPressed(Key key)
        {
            plugin.PressAndRelease((ushort)key);
        }

        public void setPressed(Key key, bool state = true)
        {
            plugin.PressAndRelease((ushort)key, state);
        }

        public bool this[Key key]
        {
            get => getKeyDown(key);
            set { setKey(key, value); }
        }
    }
}
