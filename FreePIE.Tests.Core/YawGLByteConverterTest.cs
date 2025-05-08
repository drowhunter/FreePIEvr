using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [TestMethod]
        public void ShouldWork()
        {
            var m_Queue = new EnforcedQueue<(long elapsedMs, float angle)>(3);
            m_Queue.Enqueue((10, 221));
            m_Queue.Enqueue((11, 221.002625f));

            m_Queue.Enqueue((12, 246f));

            var av = CalculateAngularVelocity(m_Queue);

            Assert.IsTrue(av <= 360 && av >= 0);
        }



        

        private float CalculateAngularVelocity(EnforcedQueue<(long elapsedMs, float angle)> m_Queue)
        {


            (long elapsedTime, float angle)[] x;

            x = m_Queue.ToArraySafe();

            List<float> avg = new List<float>();

            for (var i = 0; i < x.Length; i++)
            {
                if (i == 0 || x[i].elapsedTime == 0)
                    continue;

                var dA = Math.Abs(x[i].angle - x[i - 1].angle);
                if (dA > 180)
                    dA = 360 - dA;

                var dT = x[i].elapsedTime;  //(x[i].time - x[i - 1].time).TotalMilliseconds;
                
                var v = (dA / dT) * 1000f;
                
                
                if(v < 120)
                    avg.Add(v);
                

            }
            if (avg.Any())
            {
                var retval = avg.Average();

                return retval;
            }

            return 0;
        }

    }

    internal class EnforcedQueue<T> : IEnumerable<T>
    {

        private int _limit = 0;

        private Queue<T> _queue;

        private readonly object _lock = new object();


        public EnforcedQueue(int capacity)
        {
            _queue = new Queue<T>(capacity);
            _limit = capacity;
        }


        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        public T[] ToArraySafe()
        {
            lock (_lock)
            {
                return _queue.ToArray();
            }
        }

        public void Enqueue(T item)
        {
            lock (_lock)
            {
                if (_queue.Count >= _limit)
                {
                    _queue.Dequeue();
                }

                _queue.Enqueue(item);
            }
        }

        public T CalculateAverage(Func<T, T, T> accumulator, Func<T, T> divisor)
        {
            T sum = this.Aggregate(accumulator);

            return divisor(sum);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_queue).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_queue).GetEnumerator();
        }

        //public T Sum(Func<T,T, T> summer)
        //{

        //    return this.ToArray().Aggregate(summer);
        //}

        //public T Average(Func<T, T, T> summer)
        //{
        //    var count = this.Count;
        //    if (count == 0)
        //        return default(T);
        //    var sum = this.Sum(summer);


        //}
    }
}
