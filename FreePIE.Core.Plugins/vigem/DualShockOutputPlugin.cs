using System;
using System.Collections.Generic;
using System.Linq;

using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.Globals;
using Nefarius.ViGEm.Client.Targets.DualShock4;

using Nefarius.ViGEm.Client.Targets;
using FreePIE.Core.Common;

namespace FreePIE.Core.Plugins.vigem
{
    [GlobalType(Type = typeof(DsOutputGlobal), IsIndexed = true)]
    public class DualShockOutputPlugin : ViGemPluginBase
    {
        public override string FriendlyName { get { return "DualShockOutput"; } }
        public override object CreateGlobal()
        {
            _globals = new List<VigemGlobalBase>();
            return new GlobalIndexer<DsOutputGlobal>(CreateGlobal);
        }

        private DsOutputGlobal CreateGlobal(int index)
        {
            var global = new DsOutputGlobal(index, this);
            _globals.Add(global);
            PlugController(index);
            return global;
        }
    }

    [Global(Name = "dsOut")]
    public class DsOutputGlobal : VigemGlobalBase, IDisposable
    {

        private IDualShock4Controller controller;
        private ushort? buttons => controller?.ButtonState;

        public DsOutputGlobal(int index, ViGemPluginBase plugin) : base(index)
        {

            controller = plugin.Client.CreateDualShock4Controller();
            controller.AutoSubmitReport = false;

            //var thread = new Thread(() =>
            //{
            //    foreach(var bytes in controller.AwaitRawOutputReport())
            //    {
            //        controller_FeedbackReceived(controller, new DualShock4FeedbackReceivedEventArgs(bytes[0], bytes[1], LightbarColor);
            //    }
            //});

            controller.Connect();
        }

        internal override void Update()
        {
            controller.SetDPadDirection(GetDirection(_DpadFlags));

            controller.SubmitReport();
            controller.ResetReport();

            _DpadFlags = 0;
        }



        public bool cross
        {
            get => (buttons & DualShock4Button.Cross.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.Cross, value);
        }

        public bool circle
        {
            get => (buttons & DualShock4Button.Circle.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.Circle, value);
        }

        public bool square
        {
            get => (buttons & DualShock4Button.Square.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.Square, value);
        }

        public bool triangle
        {
            get => (buttons & DualShock4Button.Triangle.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.Triangle, value);
        }

       

        public bool options
        {
            get => (buttons & DualShock4Button.Options.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.Options, value);
        }


        public bool share
        {
            get => (buttons & DualShock4Button.Share.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.Share, value);
        }


        public bool up
        {
            get => _DpadFlags.HasFlag(dpadFlags.Up);
            set => _DpadFlags = value ? _DpadFlags | dpadFlags.Up : _DpadFlags & ~dpadFlags.Up;
        }

        public bool down
        {
            get => _DpadFlags.HasFlag(dpadFlags.Down);
            set => _DpadFlags = value ? _DpadFlags | dpadFlags.Down : _DpadFlags & ~dpadFlags.Down;

        }

        public bool left
        {
            get => _DpadFlags.HasFlag(dpadFlags.Left);
            set => _DpadFlags = value ? _DpadFlags | dpadFlags.Left : _DpadFlags & ~dpadFlags.Left;
        }

        public bool right
        {
            get => _DpadFlags.HasFlag(dpadFlags.Right);
            set => _DpadFlags = value ? _DpadFlags | dpadFlags.Right : _DpadFlags & ~dpadFlags.Right;
        }

        public bool leftThumb
        {
            get => (buttons & DualShock4Button.ThumbLeft.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.ThumbLeft, value);
        }

        public bool rightThumb
        {
            get => (buttons & DualShock4Button.ThumbRight.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.ThumbRight, value);
        }

        public bool PS
        {
            get => (controller.SpecialButtonState & DualShock4SpecialButton.Ps.Value) != 0;
            set => controller.SetButtonState(DualShock4SpecialButton.Ps, value);
        }

        public bool touchPad
        {
            get => (controller.SpecialButtonState & DualShock4SpecialButton.Touchpad.Value) != 0;
            set => controller.SetButtonState(DualShock4SpecialButton.Touchpad, value);
        }

        /// <summary>
        /// Acceptable values range 0 - 1
        /// </summary>
        public double L2
        {
            get => controller.LeftTrigger / 255.0;
            set => controller.SetSliderValue(DualShock4Slider.LeftTrigger, (byte)Maths.EnsureMapRange(value, 0, 1, 0, 255));

        }

        public double R2
        {
            get => controller.RightTrigger / 255.0;
            set => controller.SetSliderValue(DualShock4Slider.RightTrigger, (byte)Maths.EnsureMapRange(value, 0, 1, 0, 255));
        }

        public bool L1
        {
            get => (buttons & DualShock4Button.ShoulderLeft.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.ShoulderLeft, value);
        }

        public bool R1
        {
            get => (buttons & DualShock4Button.ShoulderRight.Value) != 0;
            set => controller.SetButtonState(DualShock4Button.ShoulderRight, value);
        }

        public double leftStickX
        {
            get => Maths.EnsureMapRange(controller.LeftThumbX, 0, 255, -1, 1);
            set => controller.SetAxisValue(DualShock4Axis.LeftThumbX, (byte)Maths.EnsureMapRange(value, -1, 1, 0, 255));
        }

        public double leftStickY
        {
            get => -Maths.EnsureMapRange(controller.LeftThumbY, 0, 255, -1, 1);
            set => controller.SetAxisValue(DualShock4Axis.LeftThumbY, (byte)Maths.EnsureMapRange(value, 1, -1, 0, 255));
        }

        public double rightStickX
        {
            get => Maths.EnsureMapRange(controller.RightThumbX, 0, 255, -1, 1);
            set => controller.SetAxisValue(DualShock4Axis.RightThumbX, (byte)Maths.EnsureMapRange(value, -1, 1, 0, 255));

        }


        public double rightStickY
        {
            get => -Maths.EnsureMapRange(controller.RightThumbY, 0, 255, -1, 1);
            set => controller.SetAxisValue(DualShock4Axis.RightThumbY, (byte)Maths.EnsureMapRange(value, 1, -1, 0, 255));
        }




        [Flags]
        private enum dpadFlags
        {
            None = 0, Up = 1 << 0, Right = 1 << 1, Down = 1 << 2, Left = 1 << 3
        }

        private dpadFlags _DpadFlags = 0;

        private static DualShock4DPadDirection GetDirection(dpadFlags flags)
        {

            DualShock4DPadDirection retval = DualShock4DPadDirection.None;

            var g = new List<KeyValuePair<dpadFlags, DualShock4DPadDirection>>()
            {
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Up,DualShock4DPadDirection.North),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Up | dpadFlags.Right,DualShock4DPadDirection.Northeast ),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Right, DualShock4DPadDirection.East ),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Right | dpadFlags.Down,DualShock4DPadDirection.Southeast),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Down, DualShock4DPadDirection.South),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Down | dpadFlags.Left, DualShock4DPadDirection.Southwest),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Left, DualShock4DPadDirection.West),
                new KeyValuePair<dpadFlags, DualShock4DPadDirection>( dpadFlags.Up | dpadFlags.Left, DualShock4DPadDirection.Northwest)
            };


            if (flags != 0)
                retval = g.Single(gg => gg.Key == flags).Value;


            return retval;

        }

        internal override void Disconnect()
        {
            if (controller != null)
            {
                controller.Disconnect();
            }
        }

        

        public void Dispose()
        {
            if (controller != null)
            {
                Disconnect();
                controller = null;
            }

        }

    }
}
