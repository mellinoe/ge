

using BEPUutilities;
namespace BEPUphysics.Paths
{
    /// <summary>
    /// Wrapper around a 3d position curve that specifies a specific velocity at which to travel.
    /// </summary>
    public class ConstantLinearSpeedCurve : ConstantSpeedCurve<System.Numerics.Vector3>
    {
        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speed">Speed to maintain while traveling around a curve.</param>
        /// <param name="curve">Curve to wrap.</param>
        public ConstantLinearSpeedCurve(float speed, Curve<System.Numerics.Vector3> curve)
            : base(speed, curve)
        {
        }

        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speed">Speed to maintain while traveling around a curve.</param>
        /// <param name="curve">Curve to wrap.</param>
        /// <param name="sampleCount">Number of samples to use when constructing the wrapper curve.
        /// More samples increases the accuracy of the speed requirement at the cost of performance.</param>
        public ConstantLinearSpeedCurve(float speed, Curve<System.Numerics.Vector3> curve, int sampleCount)
            : base(speed, curve, sampleCount)
        {
        }

        protected override float GetDistance(System.Numerics.Vector3 start, System.Numerics.Vector3 end)
        {
            float distance;
            Vector3Ex.Distance(ref start, ref end, out distance);
            return distance;
        }
    }
}