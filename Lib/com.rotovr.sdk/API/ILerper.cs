using System;
using System.Threading;
using System.Threading.Tasks;

namespace com.rotovr.sdk
{
    public interface ILerper
    {
        /// <summary>
        /// Event triggered when chair data changes.
        /// </summary>
        event Action<double> OnAngleUpdate;
        float NewFPS { get; }
        float OldFPS { get; }

        void UpdateYaw(double degrees);
        double GetInterpolatedYaw();
        Task StartInterpolationLoopAsync(CancellationToken cancellationToken = default);
    }


}
