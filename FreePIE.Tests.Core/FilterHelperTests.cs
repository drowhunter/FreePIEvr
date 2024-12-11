using System;
using System.Text;

using FreePIE.Core.Model;
using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers;
using FreePIE.Tests.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FreePIE.Tests.Core
{
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

    [TestClass]
    public class When_giving_reversing_range : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 1;
            xMin = 0;
            xMax = 5;
            yMin = 5;
            yMax = 0;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]
        
        public void It_should_return_yMin_inverted()
        {
            Assert.AreEqual(4, y);
        }
    }

    [TestClass]
    public class When_giving_reversing_range2 : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 1;
            xMin = 0;
            xMax = 5;
            yMin = 20;
            yMax = 10;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]

        public void It_should_return_yMin_inverted()
        {
            Assert.AreEqual(18, y);
        }
    }

    [TestClass]
    public class When_giving_reversing_range3 : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 1;
            xMin = 0;
            xMax = 5;
            yMin = 10;
            yMax = 20;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]

        public void It_should_return_yMin_normally()
        {
            Assert.AreEqual(12, y);
        }
    }

    [TestClass]
    public class When_giving_reversing_range4 : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 4;
            xMin = 5;
            xMax = 0;
            yMin = 10;
            yMax = 20;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]

        public void It_should_return_yMin_inverted()
        {
            Assert.AreEqual(12, y);
        }
    }

    [TestClass]
    public class When_giving_reversing_input_range5 : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 9;
            xMin = 10;
            xMax = 5;
            yMin = 10;
            yMax = 20;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]

        public void It_should_return_yMin_inverted()
        {
            Assert.AreEqual(12, y);
        }
    }

    [TestClass]
    public class When_giving_reversing_input_range_with_negatives : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 0;
            xMin = -10;
            xMax = 10;
            yMin = 10;
            yMax = 20;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]

        public void It_should_return_yMin_inverted()
        {
            Assert.AreEqual(15, y);
        }
    }

    [TestClass]
    public class When_giving_reversing_output_range_with_negatives : MapRangeTest
    {
        [TestInitialize]
        public void Context()
        {
            x = 15;
            xMin = 10;
            xMax = 20;
            yMin = 10;
            yMax = -10;

            Map(filterHelper.ensureMapRange);
        }

        [TestMethod]

        public void It_should_return_zero()
        {
            Assert.AreEqual(0, y);
        }
    }
}
