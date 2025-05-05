namespace com.rotovr.sdk
{
    public class Lerper : BaseLerper
    {
        public Lerper() 
        {
        }

        public override float GetInterpolatedValue(float latestValue, float previousValue, float t)
        {
            return NormalizeAngle(previousValue + (latestValue - previousValue) * t);
        }

    }
}
