using System;
using System.Numerics;

namespace com.rotovr.sdk
{
    public class Slerper : BaseLerper
    {
        public Slerper()
        {
        }


        public override float GetInterpolatedValue(float latestValue, float previousValue, float t)
        {
            
            // Convert yaw angles to quaternions
            Quaternion quatStart = Quaternion.CreateFromYawPitchRoll(ToRadians(previousValue), 0, 0);
            Quaternion quatEnd = Quaternion.CreateFromYawPitchRoll(ToRadians(latestValue), 0, 0);

            // Apply SLERP
            Quaternion interpolatedQuat = Quaternion.Slerp(quatStart, quatEnd, t);

            // Convert back to yaw
            float interpolatedYaw = (float)Math.Atan2(2.0f * (interpolatedQuat.W * interpolatedQuat.Y + interpolatedQuat.X * interpolatedQuat.Z),
                                                1.0f - 2.0f * (interpolatedQuat.Y * interpolatedQuat.Y + interpolatedQuat.Z * interpolatedQuat.Z));

               
            return NormalizeAngle(ToDegrees(interpolatedYaw)); 
           
        }
    }
}
