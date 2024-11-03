using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FreePIE.Core.Common.Extensions;
using FreePIE.Core.Model;
using FreePIE.Core.ScriptEngine.Globals;
using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers;
using FreePIE.Tests.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FreePIE.Tests.Core
{
    [TestClass]
    public class CurveHelperTest : TestBase
    {
        protected CurveHelper curveHelper;

        public CurveHelperTest()
        {
            curveHelper = Get<CurveHelper>();
        }

        [TestMethod]
        public void It_should_return_a_named_CurveGlobal()
        {
            var curve = curveHelper.create("mycurve", (0, 0), (1, 1) );
            Assert.IsInstanceOfType(curve, typeof(CurveGlobalProvider.CurveGlobal));
            Assert.AreEqual("mycurve", curve.Name);
        }

        [TestMethod]
        public void It_should_solve_for_y()
        {
            var curve = curveHelper.create("curve", (0, 0), (1, 1) );

            var y = curve.getY(0.5);

            Assert.AreEqual(0.5, y);
        }

        [TestMethod]
        public void It_should_ease_in_should_be_correct()
        {
            double min = 0;
            double max = 100;

            CurveGlobalProvider.CurveGlobal curve = curveHelper.create("myCurve", (min, min), (60,12),(93,70), (max, max) );

            var results = new Dictionary<double,double>();
            for(var i = min; i <= max; i+= (int)(max/10))
            {
                results.Add(i , Math.Round(curve.getY(i),2));
            }

            Assert.IsTrue(results.All(_ => _.Value >= min && _.Value <= max));
            
            Assert.AreEqual(results.First().Key , results.First().Value);
            Assert.AreEqual(results.Last().Key , results.Last().Value);

            Assert.IsTrue(results.Skip(1).TakeAllButLast().All(_ => _.Value < _.Key));
            

            
        }

        [TestMethod]
        public void It_should_ease_out_should_be_correct()
        {
            double min = 0;
            double max = 100;

            CurveGlobalProvider.CurveGlobal curve = curveHelper.create("myCurve", (min, min), (12, 40), (50, 91), (max, max));

            var results = new Dictionary<double, double>();
            for (var i = min; i <= max; i += (int)(max / 10))
            {
                results.Add(i, Math.Round(curve.getY(i), 2));
            }

            Assert.IsTrue(results.All(_ => _.Value >= min && _.Value <= max));

            Assert.AreEqual(results.First().Key, results.First().Value);
            Assert.AreEqual(results.Last().Key, results.Last().Value);

            Assert.IsTrue(results.Skip(1).TakeAllButLast().All(_ => _.Value > _.Key));



        }
    }
    public class MapRangeTest : TestBase
    {
        protected double x;
        protected double xMin;
        protected double xMax;
        protected double yMin;
        protected double yMax;
        protected double y;

        protected FilterHelper filterHelper;

        protected MapRangeTest()
        {
            filterHelper = Get<FilterHelper>();
        }

        protected void Map(Func<double, double, double, double, double, double> mapRangeFunc)
        {
            y = mapRangeFunc(x, xMin, xMax, yMin, yMax);
        }
    }

    [TestClass]
    public class When_giving_ensureMapRange_a_min_value_out_of_range : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = -19;
            xMin = -5;
            xMax = 5;
            yMin = -15;
            yMax = 15;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]
        public void It_should_return_yMin()
        {
            Assert.AreEqual(-15, y);
        }
    }

    [TestClass]
    public class When_giving_ensureMapRange_a_max_value_out_of_range : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 17;
            xMin = -5;
            xMax = 5;
            yMin = -15;
            yMax = 15;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]
        public void It_should_return_yMin()
        {
            Assert.AreEqual(15, y);
        }
    }

    [TestClass]
    public class When_giving_ensureMapRange_a_value_in_range : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 3;
            xMin = -5;
            xMax = 5;
            yMin = -15;
            yMax = 15;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]
        public void It_should_return_yMin()
        {
            Assert.AreEqual(9, y);
        }
    }
}
