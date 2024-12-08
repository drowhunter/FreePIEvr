using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.keyboard;
using FreePIE.Core.Plugins.Strategies;
using SharpDX.DirectInput;

namespace FreePIE.Core.Plugins
{

    
    //// SlimDX key-codes
    //[GlobalEnum]
    //public enum Key
    //{
    //    D0 = 0,
    //    D1 = 1,
    //    D2 = 2,
    //    D3 = 3,
    //    D4 = 4,
    //    D5 = 5,
    //    D6 = 6,
    //    D7 = 7,
    //    D8 = 8,
    //    D9 = 9,
    //    A = 10,
    //    B = 11,
    //    C = 12,
    //    D = 13,
    //    E = 14,
    //    F = 15,
    //    G = 16,
    //    H = 17,
    //    I = 18,
    //    J = 19,
    //    K = 20,
    //    L = 21,
    //    M = 22,
    //    N = 23,
    //    O = 24,
    //    P = 25,
    //    Q = 26,
    //    R = 27,
    //    S = 28,
    //    T = 29,
    //    U = 30,
    //    V = 31,
    //    W = 32,
    //    X = 33,
    //    Y = 34,
    //    Z = 35,
    //    AbntC1 = 36,
    //    AbntC2 = 37,
    //    Apostrophe = 38,
    //    Applications = 39,
    //    AT = 40,
    //    AX = 41,
    //    Backspace = 42,
    //    Backslash = 43,
    //    Calculator = 44,
    //    CapsLock = 45,
    //    Colon = 46,
    //    Comma = 47,
    //    Convert = 48,
    //    Delete = 49,
    //    DownArrow = 50,
    //    End = 51,
    //    Equals = 52,
    //    Escape = 53,
    //    F1 = 54,
    //    F2 = 55,
    //    F3 = 56,
    //    F4 = 57,
    //    F5 = 58,
    //    F6 = 59,
    //    F7 = 60,
    //    F8 = 61,
    //    F9 = 62,
    //    F10 = 63,
    //    F11 = 64,
    //    F12 = 65,
    //    F13 = 66,
    //    F14 = 67,
    //    F15 = 68,
    //    Grave = 69,
    //    Home = 70,
    //    Insert = 71,
    //    Kana = 72,
    //    Kanji = 73,
    //    LeftBracket = 74,
    //    LeftControl = 75,
    //    LeftArrow = 76,
    //    LeftAlt = 77,
    //    LeftShift = 78,
    //    LeftWindowsKey = 79,
    //    Mail = 80,
    //    MediaSelect = 81,
    //    MediaStop = 82,
    //    Minus = 83,
    //    Mute = 84,
    //    MyComputer = 85,
    //    NextTrack = 86,
    //    NoConvert = 87,
    //    NumberLock = 88,
    //    NumberPad0 = 89,
    //    NumberPad1 = 90,
    //    NumberPad2 = 91,
    //    NumberPad3 = 92,
    //    NumberPad4 = 93,
    //    NumberPad5 = 94,
    //    NumberPad6 = 95,
    //    NumberPad7 = 96,
    //    NumberPad8 = 97,
    //    NumberPad9 = 98,
    //    NumberPadComma = 99,
    //    NumberPadEnter = 100,
    //    NumberPadEquals = 101,
    //    NumberPadMinus = 102,
    //    NumberPadPeriod = 103,
    //    NumberPadPlus = 104,
    //    NumberPadSlash = 105,
    //    NumberPadStar = 106,
    //    Oem102 = 107,
    //    PageDown = 108,
    //    PageUp = 109,
    //    Pause = 110,
    //    Period = 111,
    //    PlayPause = 112,
    //    Power = 113,
    //    PreviousTrack = 114,
    //    RightBracket = 115,
    //    RightControl = 116,
    //    Return = 117,
    //    RightArrow = 118,
    //    RightAlt = 119,
    //    RightShift = 120,
    //    RightWindowsKey = 121,
    //    ScrollLock = 122,
    //    Semicolon = 123,
    //    Slash = 124,
    //    Sleep = 125,
    //    Space = 126,
    //    Stop = 127,
    //    PrintScreen = 128,
    //    Tab = 129,
    //    Underline = 130,
    //    Unlabeled = 131,
    //    UpArrow = 132,
    //    VolumeDown = 133,
    //    VolumeUp = 134,
    //    Wake = 135,
    //    WebBack = 136,
    //    WebFavorites = 137,
    //    WebForward = 138,
    //    WebHome = 139,
    //    WebRefresh = 140,
    //    WebSearch = 141,
    //    WebStop = 142,
    //    Yen = 143,
    //    Unknown = 144,
    //}


    [GlobalType(Type = typeof (KeyboardGlobal))]
    public class KeyboardPlugin : Plugin
    {
        private static List<key> keys => KeyCollection.keys;

        private DirectInput DirectInputInstance = new DirectInput();
        private Keyboard KeyboardDevice;
        private KeyboardState KeyState = new KeyboardState();
        private bool[] MyKeyDown = new bool[150];
        private SetPressedStrategy setKeyPressedStrategy;
        private GetPressedStrategy<int> getKeyPressedStrategy;

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

            setKeyPressedStrategy = new SetPressedStrategy(KeyDown, KeyUp);
            getKeyPressedStrategy = new GetPressedStrategy<int>(IsKeyDown);

            OnStarted(this, new EventArgs());
            return null;
        }

        public override void Stop()
        {
            // Don't leave any keys pressed
            for (int i = 0; i < MyKeyDown.Length; i++)
            {
                if (MyKeyDown[i])
                    KeyUp(i);
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

        public bool IsKeyDown(int keycode)
        {
            // Returns true if the key is currently being pressed
            var key = (SharpDX.DirectInput.Key) keycode;
            bool down = KeyState.IsPressed(key) || MyKeyDown[keycode];
            return down;
        }

        public bool IsKeyUp(int keycode)
        {
            // Returns true if the key is currently being pressed
            var key = (SharpDX.DirectInput.Key) keycode;
            bool up = KeyState.IsPressed(key) == false && !MyKeyDown[keycode];
            return up;
        }

        public bool WasKeyPressed(int key)
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

        public void KeyDown(int code)
        {

            if (!MyKeyDown[code])
            {
                //System.Console.Out.WriteLine("keydown");
                MyKeyDown[code] = true;
                var scancode = keys[code].scanCode;  // ScanCodeMap[code]; // convert the keycode for SendInput

                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_KEYBOARD;

                
                input[0].ki = KeyInput(scancode, keys[code].IsExtended ? MouseKeyIO.KEYEVENTF_EXTENDEDKEY : 0);

                MouseKeyIO.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

            }
        }

        public void KeyUp(int code)
        {

            if (MyKeyDown[code])
            {
                //System.Console.Out.WriteLine("keyup");
                MyKeyDown[code] = false;

                var scancode = keys[code].scanCode; // convert the keycode for SendInput

                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_KEYBOARD;
                if (keys[code].IsExtended)
                    input[0].ki = KeyInput(scancode, MouseKeyIO.KEYEVENTF_EXTENDEDKEY | MouseKeyIO.KEYEVENTF_KEYUP);
                else
                    input[0].ki = KeyInput(scancode, MouseKeyIO.KEYEVENTF_KEYUP);

                MouseKeyIO.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

            }
        }

        public void PressAndRelease(int keycode)
        {
            setKeyPressedStrategy.Add(keycode);
        }

        public void PressAndRelease(int keycode, bool state)
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

        public bool getKeyDown(Key key)
        {
            return plugin.IsKeyDown((int) key);
        }

        public void setKeyDown(Key key)
        {
            plugin.KeyDown((int) key);
        }

        public bool getKeyUp(Key key)
        {
            return plugin.IsKeyUp((int) key);
        }

        public void setKeyUp(Key key)
        {
            plugin.KeyUp((int) key);
        }

        public void setKey(Key key, bool down)
        {
            if (down)
                plugin.KeyDown((int) key);
            else
                plugin.KeyUp((int) key);
        }

        public bool getPressed(Key key)
        {
            return plugin.WasKeyPressed((int) key);
        }

        public void setPressed(Key key)
        {
            plugin.PressAndRelease((int) key);
        }

        public void setPressed(Key key, bool state)
        {
            plugin.PressAndRelease((int)key, state);
        }
    }
}
