using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace com.rotovr.sdk
{
    public class Slerper : ILerper
    {
        private double latestYaw = 0.0; // Last received yaw
        private double previousYaw = 0.0; // Previous yaw for interpolation
        private double interpolationFactor = 0.0; // Smooth interpolation step
        private readonly object lockObj = new object(); // Thread safety
        private double oldMs;
        private readonly double newMs;
        private DateTime lastSensorUpdate; // Track last yaw update time

        public float OldFPS => (float)(1000 / oldMs);

        DateTime lastNewUpdate = DateTime.UtcNow;

        float FPS = 1;
        public float NewFPS => FPS;

        /// <summary>
        /// Event triggered when chair data changes.
        /// </summary>
        public event Action<double> OnAngleUpdate;

        public Slerper(int newFps)
        {
            lastSensorUpdate = DateTime.UtcNow;
            this.newMs = Math.Min(1, 1000 / newFps);
        }

        public void UpdateYaw(double degrees)
        {
            lock (lockObj)
            {
                var now = DateTime.UtcNow;
                oldMs = Math.Min(1, (now - lastSensorUpdate).TotalMilliseconds);
                lastSensorUpdate = now;
                previousYaw = latestYaw;
                latestYaw = degrees;
            }
        }

        public double GetInterpolatedYaw()
        {
            lock (lockObj)
            {
                double elapsedMs = (DateTime.UtcNow - lastSensorUpdate).TotalMilliseconds;
                double t = elapsedMs / oldMs; // Normalize time step based on 30ms sensor updates
                t = Math.Max(0.0, Math.Min(t, 1.0)); // Clamp between 0-1

                // Convert yaw angles to quaternions
                Quaternion quatStart = Quaternion.CreateFromYawPitchRoll((float)ToRadians(previousYaw), 0, 0);
                Quaternion quatEnd = Quaternion.CreateFromYawPitchRoll((float)ToRadians(latestYaw), 0, 0);

                // Apply SLERP
                Quaternion interpolatedQuat = Quaternion.Slerp(quatStart, quatEnd, (float)t);

                // Convert back to yaw
                double interpolatedYaw = Math.Atan2(2.0 * (interpolatedQuat.W * interpolatedQuat.Y + interpolatedQuat.X * interpolatedQuat.Z),
                                                    1.0 - 2.0 * (interpolatedQuat.Y * interpolatedQuat.Y + interpolatedQuat.Z * interpolatedQuat.Z));

                return (ToDegrees(interpolatedYaw) + 360) % 360; // Ensure yaw stays in 0-360 range
            }
        }

        public Task StartInterpolationLoopAsync(CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        FPS = (float) DateTime.UtcNow.Subtract(lastNewUpdate).TotalMilliseconds;
                        lastNewUpdate = DateTime.UtcNow;
                        double interpolatedYaw = GetInterpolatedYaw();
                        OnAngleUpdate?.Invoke(interpolatedYaw);
                        //Thread.Sleep(newMs);
                        await Task.Delay((int)newMs, cancellationToken);

                    }

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
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine($"Task error: {task.Exception?.GetBaseException().Message}");
                }
            });
        }

        // Helper Functions for .NET Standard 2.0
        private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
        private static double ToDegrees(double radians) => radians * (180.0 / Math.PI);

        
    }
}
