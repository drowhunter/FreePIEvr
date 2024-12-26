using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreePIE.Core.Common
{
    public static class Maths
    {
        /// <summary>
        /// Apply the sign of y to x
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static double CopySign(double x, double y) 
        {
            return x * (y < 0 ? -1 : 1);
        }

        public static double DeadZone(double x, double deadZone, double minY, double maxY)
        {
            if (Deadband(x, deadZone) == 0)
                return 0;

            return CopySign(EnsureMapRange(Math.Abs(x), deadZone, 1, minY, maxY), x);
        }


        public static double Deadband(double x, double deadZone, double minY, double maxY)
        {
            var scaled = EnsureMapRange(x, minY, maxY, -1, 1);
            var y = 0d;

            if (Math.Abs(scaled) > deadZone)
                y = EnsureMapRange(Math.Abs(scaled), deadZone, 1, 0, 1) * Math.Sign(x);

            return EnsureMapRange(y, -1, 1, minY, maxY);
        }

        public static double Deadband(double x, double deadZone)
        {
            if (Math.Abs(x) >= Math.Abs(deadZone))
                return x;

            return 0;
        }

        public static double MapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }

        public static double EnsureMapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return Math.Max(Math.Min(MapRange(x, xMin, xMax, yMin, yMax), Math.Max(yMin, yMax)), Math.Min(yMin, yMax));
        }

    }
}
