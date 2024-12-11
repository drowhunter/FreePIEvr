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
        private DirectInput DirectInputInstance = new DirectInput();
        private Keyboard KeyboardDevice;
        private KeyboardState KeyState = new KeyboardState();
        private bool[] MyKeyDown = new bool[255];
        private SetPressedStrategy<Key> setKeyPressedStrategy;
        private GetPressedStrategy<Key> getKeyPressedStrategy;

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

            setKeyPressedStrategy = new SetPressedStrategy<Key>(SendKeyDown, SendKeyUp);
            getKeyPressedStrategy = new GetPressedStrategy<Key>(IsKeyDown);

            OnStarted(this, new EventArgs());
            return null;
        }

        public override void Stop()
        {
            // Don't leave any keys pressed
            for (ushort i = 0; i < MyKeyDown.Length; i++)
            {
                if (MyKeyDown[i])
                    SendKeyUp((Key)i);
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

        public bool IsKeyDown(Key keycode)
        {
            // Returns true if the key is currently being pressed
            bool down = KeyState.IsPressed((SharpDX.DirectInput.Key)keycode) || MyKeyDown[(int)keycode];
            return down;
        }

        public bool IsKeyUp(Key keycode) => !IsKeyDown(keycode);

        public bool WasKeyTapped(Key key)
        {
            return getKeyPressedStrategy.IsPressed(key);
        }

        private MouseKeyIO.KEYBDINPUT KeyInput(Key key, uint flag)
        {
            ushort code = (ushort)key;
            if (code > 0x7f)
            {
                flag |= MouseKeyIO.KEYEVENTF_EXTENDEDKEY;
            }

            var i = new MouseKeyIO.KEYBDINPUT();
            i.wVk = 0;
            i.wScan = code;
            i.time = 0;
            i.dwExtraInfo = IntPtr.Zero;
            i.dwFlags = flag | MouseKeyIO.KEYEVENTF_SCANCODE;
            return i;
        }

        public void SendKeyDown(Key code)
        {
            if (!MyKeyDown[(int)code])
            {
                MyKeyDown[(int)code] = true;
                
                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_KEYBOARD;
                input[0].ki = KeyInput(code, 0);

                MouseKeyIO.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
            }
        }

        public void SendKeyUp(Key code)
        {
            if (MyKeyDown[(int)code])
            {                
                MyKeyDown[(int)code] = false;                

                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_KEYBOARD;
                input[0].ki = KeyInput(code, MouseKeyIO.KEYEVENTF_KEYUP);

                MouseKeyIO.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
            }
        }

        public void TapKey(Key keycode)
        {
            setKeyPressedStrategy.Add(keycode);
        }

        public void TapKey(Key keycode, bool state)
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

        public bool getKey(Key key)
        {
            return plugin.IsKeyDown(key);
        }

        public void setKey(Key key, bool down)
        {
            if (down)
                plugin.SendKeyDown(key);
            else
                plugin.SendKeyUp(key);
        }

        public bool wasTapped(Key key)
        {
            return plugin.WasKeyTapped(key);
        }

        public void tapKey(Key key)
        {
            plugin.TapKey(key);
        }

        protected void tapKey(Key key, bool state = true)
        {
            plugin.TapKey(key, state);
        }

        public bool this[Key key]
        {
            get => getKey(key);
            set { setKey(key, value); }
        }
    }
}
