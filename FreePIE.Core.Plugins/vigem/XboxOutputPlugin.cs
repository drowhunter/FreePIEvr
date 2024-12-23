using System;
using System.Collections.Generic;

using FreePIE.Core.Common;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.Globals;

using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace FreePIE.Core.Plugins.vigem
{
    [GlobalType(Type = typeof(XboxOutputGlobal), IsIndexed = true)]
    public class XboxOutputPlugin : ViGemPluginBase
    {
        public override string FriendlyName { get { return "XboxOutput"; } }
        public override object CreateGlobal()
        {
            _globals = new List<VigemGlobalBase>(4);
            return new GlobalIndexer<XboxOutputGlobal>(CreateGlobal);
        }

        private XboxOutputGlobal CreateGlobal(int index)
        {
            var global = new XboxOutputGlobal(index, this);
            _globals.Add(global);
            PlugController(index);
            return global;
        }
    }

    [Global(Name = "xbOut")]
    public class XboxOutputGlobal : VigemGlobalBase, IDisposable
    {
        public event RumbleEvent onRumble;
        public delegate void RumbleEvent(byte largeMotor, byte smallMotor, byte led);

        private IXbox360Controller controller;

        private ushort? buttons => controller?.ButtonState;


        public XboxOutputGlobal(int index, ViGemPluginBase plugin) : base(index)
        {
            controller = plugin.Client.CreateXbox360Controller();
            controller.AutoSubmitReport = false;
            controller.FeedbackReceived += controller_FeedbackReceived;
            controller.Connect();
        }

        

        private void controller_FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
        {
            onRumble?.Invoke(e.LargeMotor, e.SmallMotor, e.LedNumber);            
        }

        #region Buttons
               

        public bool a
        {
            get => (buttons & Xbox360Button.A.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.A, value);
        }

        public bool b
        {
            get => (buttons & Xbox360Button.B.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.B, value);
        }

        public bool x
        {
            get => (buttons & Xbox360Button.X.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.X, value);
        }

        public bool y
        {
            get => (buttons & Xbox360Button.Y.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Y, value);
        }

        public bool leftShoulder
        {
            get => (buttons & Xbox360Button.LeftShoulder.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.LeftShoulder, value);
        }

        public bool rightShoulder
        {
            get => (buttons & Xbox360Button.RightShoulder.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.RightShoulder, value);
        }

        public bool start
        {
            get => (buttons & Xbox360Button.Start.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Start, value);
        }

        public bool back
        {
            get => (buttons & Xbox360Button.Start.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Start, value);
        }

        public bool up
        {
            get => (buttons & Xbox360Button.Up.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Up, value);
        }

        public bool down
        {
            get => (buttons & Xbox360Button.Down.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Down, value);
        }

        public bool left
        {
            get => (buttons & Xbox360Button.Left.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Left, value);
        }

        public bool right
        {
            get => (buttons & Xbox360Button.Right.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.Right, value);
        }

        public bool leftThumb
        {
            get => (buttons & Xbox360Button.LeftThumb.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.LeftThumb, value);
        }

        public bool rightThumb
        {
            get => (buttons & Xbox360Button.RightThumb.Value) != 0;
            set => controller.SetButtonState(Xbox360Button.RightThumb, value);
        }

        /// <summary>
        /// Acceptable values range 0 - 1
        /// </summary>
        public double leftTrigger
        {
            get => controller.LeftTrigger / 255.0;
            set
            {
                var v = (byte)Maths.EnsureMapRange(value, 0, 1, 0, 255);
                controller.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)v);
            }
        }

        public double rightTrigger
        {
            get => controller.RightTrigger / 255.0;
            set
            {
                var v = Maths.EnsureMapRange(value, 0, 1, 0, 255);
                controller.SetSliderValue(Xbox360Slider.RightTrigger, (byte)v);
            }
        }

        public double leftStickX
        {
            get
            {
                if (controller.LeftThumbX < 0)
                    return controller.LeftThumbX / 32768.0;

                return controller.LeftThumbX / 32767.0;
            }
            set
            {
                controller.SetAxisValue(Xbox360Axis.LeftThumbX, (short)Maths.EnsureMapRange(value, -1, 1, -32768, 32767));
                var lx = controller.LeftThumbX;
            }
        }



        public double leftStickY
        {

            get
            {
                if (controller.LeftThumbY < 0)
                    return controller.LeftThumbY / 32768.0;

                return controller.LeftThumbY / 32767.0;
            }
            set => controller.SetAxisValue(Xbox360Axis.LeftThumbY, (short)Maths.EnsureMapRange(value, -1, 1, -32768, 32767));

        }

        public double rightStickX
        {
            get
            {
                if (controller.RightThumbX < 0)
                    return controller.RightThumbX / 32768.0;

                return controller.RightThumbX / 32767.0;
            }
            set => controller.SetAxisValue(Xbox360Axis.RightThumbX, (short)Maths.EnsureMapRange(value, -1, 1, -32768, 32767));

        }


        public double rightStickY
        {
            get
            {
                if (controller.RightThumbY < 0)
                    return controller.RightThumbY / 32768.0;

                return controller.RightThumbY / 32767.0;
            }
            set => controller.SetAxisValue(Xbox360Axis.RightThumbY, (short)Maths.EnsureMapRange(value, -1, 1, -32768, 32767));
        }

        #endregion


        internal override void Disconnect()
        {
            if (controller != null)
            {
                controller.Disconnect();
            }
        }

        internal override void Update()
        {
            controller.SubmitReport();
            controller.ResetReport();
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
