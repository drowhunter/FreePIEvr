using FreePIE.Core.Contracts;
using FreePIE.Core.Model;

using System;
using System.Collections.Generic;
using System.Linq;

namespace FreePIE.Core.ScriptEngine.Globals.ScriptHelpers
{
    [Global(Name = "curves")]
    public class CurveHelper : IScriptHelper
    {
        public CurveHelper()
        {

        }

        
        /// <summary>
        /// Create a curve from a list of points
        /// </summary>
        /// <param name="points">list of points in the format x,y,x,y,x,y,x,y...</param>
        /// <returns>a curve global</returns>
        public CurveGlobalProvider.CurveGlobal create(double minimum, double maximum, params double[] points)
        {

            var pointz = new List<Point>() { new Point(minimum, minimum) };

            // ensure that all of the points values are between the minimum and maximum

            if (points.Any(p => p < minimum || p > maximum))
                throw new Exception("All points must be between the minimum and maximum values");


            pointz.AddRange(points.Select((x, i) => new { x, i }).GroupBy(p => p.i / 2).Select(g => new Point(g.First().x, g.Last().x)));

            pointz.Add(new Point(maximum, maximum));

            return new CurveGlobalProvider.CurveGlobal(new Curve(Guid.NewGuid().ToString(), pointz) { ValidateCurve = true });
        }

        public double arc(double x, bool reverse = false)
        {
            var y = 0.0;

            y = Math.Sqrt(1 - (x * x));

            return y * (reverse ? -1 : 1);
        }

        /// <summary>  
        /// Convert x,y coordinates to angle from 0-360 and magnitude  
        /// </summary>  
        /// <param name="x">The x-coordinate</param>  
        /// <param name="y">The y-coordinate</param>  
        /// <returns>A tuple containing the angle (0-360) and magnitude (0-1)</returns>  
        public (double angle, double magnitude) rectToPolar(double x, double y)
        {
            // Calculate the magnitude using the Pythagorean theorem  
            double magnitude = Math.Sqrt(x * x + y * y);

            // Normalize the magnitude to the range [0, 1]  
            magnitude = Math.Min(1, magnitude);

            // Calculate the angle in radians and convert to degrees  
            double angle = Math.Atan2(x, y) * (180 / Math.PI);

            // Ensure the angle is in the range [0, 360]  
            if (angle < 0)
                angle += 360;

            return (angle, magnitude);
        }

        public (double x, double y) polarToRect(double degrees, double magnitude)
        {
            // Convert degrees to radians  
            double radians = degrees * (Math.PI / 180);

            // Calculate x and y using the magnitude and angle  
            double y = magnitude * Math.Cos(radians);
            double x = magnitude * Math.Sin(radians);

            return (x, y);
        }

    }
   
}
