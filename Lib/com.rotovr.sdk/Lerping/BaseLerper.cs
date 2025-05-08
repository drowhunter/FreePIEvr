using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace com.rotovr.sdk
{
    public interface ILerper
    {
        /// <summary>  
        /// Event triggered when chair data changes.  
        /// </summary>  
        event Action<float> OnValueUpdate;

        /// <summary>  
        /// Gets the target framerate for the interpolation process.  
        /// </summary>  
        float TargetFramerate { get; }

        /// <summary>  
        /// Gets the original framerate before any adjustments.  
        /// </summary>  
        float OriginalFramerate { get; }

        /// <summary>  
        /// Updates the value used in the interpolation process.  
        /// </summary>  
        /// <param name="degrees">The new value in degrees to update.</param>  
        void UpdateValue(float degrees);

        /// <summary>  
        /// Gets the interpolated value based on the input parameter.  
        /// </summary>  
        ///  <param name="latestValue"> The latest value to interpolate from.</param>
        ///  <param name="previousValue"> The previous value to interpolate from.</param>
        /// <param name="t">The interpolation factor, typically between 0 and 1.</param>  
        /// <returns>The interpolated value as a float.</returns>  
        float GetInterpolatedValue(float latestValue, float previousValue, float t);

        /// <summary>  
        /// Starts the interpolation process with the provided cancellation token.  
        /// </summary>  
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>  
        void Start(int targetFrameRate,  CancellationToken cancellationToken);
    }

    public abstract class BaseLerper : ILerper
    {
        private Stopwatch _originalStopwatch;
        private Stopwatch _interpolatedStopWatch;


        private volatile float _latestValue = 0.0f;
        private volatile float _previousValue = 0.0f;        

        /// <summary>
        /// The actual time between value updates.
        /// </summary>
        private volatile float originalFrameTime = 1f;
        
        /// <summary>
        /// The actual updates per second
        /// </summary>
        public float OriginalFramerate => 1000f / originalFrameTime;

        private float _fps = 1;

        /// <summary>
        /// The acutal target framerate for the interpolation process.
        /// </summary>
        public float TargetFramerate => _fps;

        private float _targetMs;

        public event Action<float> OnValueUpdate;

        protected BaseLerper()
        {
            _originalStopwatch = new Stopwatch();
            _interpolatedStopWatch = new Stopwatch();

            _originalStopwatch.Start();
            _interpolatedStopWatch.Start();

            
        }

        public void UpdateValue(float value)
        {
            originalFrameTime = (float) Math.Max(1, _originalStopwatch.ElapsedMilliseconds);
            _previousValue = _latestValue;
            _latestValue = value;
            _originalStopwatch.Restart();
        }

        public abstract float GetInterpolatedValue(float latestValue, float previousValue, float t);

        public void Start(int targetFrameRate, CancellationToken cancellationToken)
        {
            _targetMs = Math.Max(1, 1000 / targetFrameRate);
            
            new Thread(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (OnValueUpdate == null)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        var interpolatedValue = GetInterpolatedValue(_latestValue, _previousValue, Clamp(_originalStopwatch.ElapsedMilliseconds / originalFrameTime, 0, 1));

                        Task.Run(() => OnValueUpdate?.Invoke(interpolatedValue));

                        var timeleft = (_targetMs - _interpolatedStopWatch.ElapsedMilliseconds);
                        SleepAccurate(timeleft);
                        //if(timeleft > 0)
                        //    Thread.Sleep(timeleft);

                        _fps = 1000f / Math.Max(1, _interpolatedStopWatch.ElapsedMilliseconds);
                        _interpolatedStopWatch.Restart();
                    }

                    OnValueUpdate = null;
                }
                catch (ThreadInterruptedException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in interpolation loop: {ex.Message}");
                }
            })
            { Name = "LerperThread", IsBackground = true }.Start();
        }

        private void SleepAccurate(float ms)
        {
            if (ms <= float.Epsilon)
                return;

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < ms)
            {
                Thread.SpinWait(1);
            }
            stopwatch.Stop();
        }

        protected float Clamp(float value, float min, float max)
        {
            //return (float)Math.Max(0.0f, Math.Min(value, 1.0f));
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>
        /// Normalizes an angle to the range [0, 360).
        /// </summary>
        /// <param name="degrees">angle in degrees</param>
        /// <returns></returns>
        protected float NormalizeAngle(float degrees)
        {
             return (degrees + 360) % 360;

            //if(angle < 0)
            //{
            //    angle += 360;
            //}
            //else if (angle > 360)
            //{
            //    angle -= 360;
            //}
            // return angle;
        }

        protected static float ToRadians(float degrees) => degrees * ((float) Math.PI / 180.0f);
        protected static float ToDegrees(float radians) => radians * (180.0f/ (float) Math.PI);


    }
}
