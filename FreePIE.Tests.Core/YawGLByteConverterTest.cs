using System;
using System.Text;

using FreePIE.Core.Plugins;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FreePIE.Tests.Core
{
    [TestClass]
    public class YawGLByteConverterTest
    {
        private YawGLByteConverter converter;

        [TestInitialize]
        public void Setup()
        {
            converter = new YawGLByteConverter();
        }

        [TestMethod]
        public void FromBytes_Should_Parse_Valid_Data()
        {
            // Arrange  
            var input = "Y[123.456]P[78.910]R[11.121]V[5,5,5,60]F[3,3]";
            var data = Encoding.ASCII.GetBytes(input);

            // Act  
            var result = converter.FromBytes(data);

            // Assert  
            Assert.AreEqual(123.456f, result.yaw);
            Assert.AreEqual(78.910f, result.pitch);
            Assert.AreEqual(11.121f, result.roll);
            Assert.AreEqual(5, result.amp);
            Assert.AreEqual(60, result.hz);
            Assert.AreEqual(3, result.fan);
        }

        [TestMethod]
        public void FromBytes_Should_Handle_Missing_Values()
        {
            // Arrange  
            var input = "Y[123.456]P[78.910]R[11.121]";
            var data = Encoding.ASCII.GetBytes(input);

            // Act  
            var result = converter.FromBytes(data);

            // Assert  
            Assert.AreEqual(123.456f, result.yaw);
            Assert.AreEqual(78.910f, result.pitch);
            Assert.AreEqual(11.121f, result.roll);
            Assert.AreEqual(0, result.amp);
            Assert.AreEqual(0, result.hz);
            Assert.AreEqual(0, result.fan);
        }

        [TestMethod]
        public void FromBytes_Should_Handle_Invalid_Data()
        {
            // Arrange  
            var input = "InvalidData";
            var data = Encoding.ASCII.GetBytes(input);

            // Act  
            var result = converter.FromBytes(data);

            // Assert  
            Assert.AreEqual(0, result.yaw);
            Assert.AreEqual(0, result.pitch);
            Assert.AreEqual(0, result.roll);
            Assert.AreEqual(0, result.amp);
            Assert.AreEqual(0, result.hz);
            Assert.AreEqual(0, result.fan);
        }

        [TestMethod]
        public void ToBytes_Should_Convert_Data_To_Byte_Array()
        {
            // Arrange  
            var input = new YawGLData
            {
                yaw = 123.456f,
                pitch = 78.910f,
                roll = 11.121f,
                amp = 5,
                hz = 60,
                fan = 3
            };

            // Act  
            var result = converter.ToBytes(input);
            var resultString = Encoding.ASCII.GetString(result);

            // Assert  
            Assert.IsTrue(resultString.Contains("Y[123.456]"));
            Assert.IsTrue(resultString.Contains("P[078.910]"));
            Assert.IsTrue(resultString.Contains("R[011.121]"));
            Assert.IsTrue(resultString.Contains("V[5,5,5,60]"));
            Assert.IsTrue(resultString.Contains("F[3,3]"));
        }
    }
}
