using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.rotovr.sdk
{
    public class Lerper : ILerper
    {
        private double latestYaw = 0.0; // Last received yaw value
        private double previousYaw = 0.0; // Previous yaw for interpolation
        private DateTime lastSensorUpdate; // Track last update time
        private readonly object lockObj = new object();

        private double _oldMs = 1;
        private readonly double newMs = 1;

        public float OldFPS => (float) (1000 / _oldMs);

        DateTime lastNewUpdate = DateTime.UtcNow;

        float _fps = 1;
        public float NewFPS => _fps;



        /// <summary>
        /// Event triggered when chair data changes.
        /// </summary>
        public event Action<double> OnAngleUpdate;

        public Lerper(int newFps)
        {
            lastSensorUpdate = DateTime.UtcNow;
           
            this.newMs = Math.Max(1, 1000 / newFps);
        }

        public void UpdateYaw(double degrees)
        {
            lock (lockObj)
            {
                var now = DateTime.UtcNow;
                _oldMs = Math.Max(1, (now - lastSensorUpdate).TotalMilliseconds);
                lastSensorUpdate = now;
                previousYaw = latestYaw;
                latestYaw = degrees;
            }
        }

        public int actualNewMs = 1;

        public double GetInterpolatedYaw()
        {
            lock (lockObj)
            {
                double elapsedMs = (DateTime.UtcNow - lastSensorUpdate).TotalMilliseconds;
                double t = elapsedMs / _oldMs; // Normalize time step (sensor updates every 30ms)
                t = Math.Max(0.0, Math.Min(t, 1.0)); // Clamp between 0-1

                // Apply LERP formula
                double interpolatedYaw = previousYaw + (latestYaw - previousYaw) * t;

                // Ensure yaw stays within 0-360° range
                return (interpolatedYaw + 360) % 360;
            }
        }

        //public void StartInterpolationLoop()
        //{
        //    new Thread(() =>
        //    {
        //        while (true)
        //        {
        //            double interpolatedYaw = GetInterpolatedYaw();
        //            //Console.WriteLine($"Interpolated Yaw: {interpolatedYaw:F2}°");
        //            Thread.Sleep(newMs); 
        //        }
        //    })
        //    { IsBackground = true }.Start();
        //}
        
        public Task StartInterpolationLoopAsync(CancellationToken cancellationToken = default)
        {
            //return Task.Factory.StartNew(async () =>
            new Thread(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        _fps = 1000f / (float) (DateTime.UtcNow - lastNewUpdate).TotalMilliseconds;

                        lastNewUpdate = DateTime.UtcNow;
                        double interpolatedYaw = GetInterpolatedYaw();
                        OnAngleUpdate?.Invoke((float)interpolatedYaw);

                        Thread.Sleep((int)newMs); 
                        //await Task.Delay(newMs, cancellationToken);                        
                    }

                    // remove all subscribers
                    OnAngleUpdate = null;
                }
                catch (ThreadInterruptedException)
                {
                    // Handle thread interruption if needed  
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in interpolation loop: {ex.Message}");
                }
            })
            { Name = "LerperThread", IsBackground = true }.Start();
            //,cancellationToken,
            //TaskCreationOptions.LongRunning,
            //TaskScheduler.Default).ContinueWith(task =>
            //{
            //    if (task.IsFaulted)
            //    {
            //        Console.WriteLine($"Task error: {task.Exception?.GetBaseException().Message}");
            //    }
            //});

            return Task.CompletedTask;
        }
    }
}
