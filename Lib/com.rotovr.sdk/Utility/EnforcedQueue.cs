using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rotovr.sdk.Utility
{
    internal class EnforcedQueue<T> : IEnumerable<T>
    {

        private int _limit = 0;

        private Queue<T> _queue;

        private readonly object _lock = new object();
        
        
        public EnforcedQueue(int capacity) {
            _queue = new(capacity);
            _limit = capacity;
        }

        
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return  _queue.Count;
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

        public  T CalculateAverage( Func<T, T, T> accumulator, Func<T, T> divisor)
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
