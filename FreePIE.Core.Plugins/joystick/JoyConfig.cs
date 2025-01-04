using System.Collections.Generic;

namespace FreePIE.Core.Plugins.joystick
{
    public class JoyConfig
    {
        public axisConfig x { get; set; }
        public axisConfig y { get; set; }
        public axisConfig z { get; set; }
        public axisConfig rotationX { get; set; } 
        public axisConfig rotationY { get; set; } 
        public axisConfig rotationZ { get; set; } 
        public List<axisConfig> sliders { get; set; } = new List<axisConfig>();

        public JoyConfig()
        {
            
        }

        /// <summary>
        /// Configure the joystick base on "well known" controllers
        /// </summary>
        /// <param name="name"></param>
        public JoyConfig(string name)  
        {
                
            switch (name)
            {
                case "Wireless Controller":
                case "DualSense Wireless Controller":
                    y = axisConfig.FullAxisInverted;
                    rotationZ = axisConfig.FullAxisInverted;
                    rotationX = axisConfig.HalfAxis;
                    rotationY = axisConfig.HalfAxis;
                break;
                //case "Logitech G27 Racing Wheel USB":
                //        // g27 pedals start at 65535 and go to 0 when pressed    
                //    y = axisConfig.HalfAxisInverted; //gas
                //    rotationZ = axisConfig.HalfAxisInverted; //brake
                //    //rotationX = axisConfig.HalfAxisInverted; //clutch
                //break;



            }   
        }

    }
}
