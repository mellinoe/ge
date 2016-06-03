using BEPUutilities;


namespace BEPUphysics.Paths
{
    /// <summary>
    /// Wrapper around an orientation curve that specifies a specific velocity at which to travel.
    /// </summary>
    public class ConstantAngularSpeedCurve : ConstantSpeedCurve<System.Numerics.Quaternion>
    {
        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speed">Speed to maintain while traveling around a curve.</param>
        /// <param name="curve">Curve to wrap.</param>
        public ConstantAngularSpeedCurve(float speed, Curve<System.Numerics.Quaternion> curve)
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
        public ConstantAngularSpeedCurve(float speed, Curve<System.Numerics.Quaternion> curve, int sampleCount)
            : base(speed, curve, sampleCount)
        {
        }

        protected override float GetDistance(System.Numerics.Quaternion start, System.Numerics.Quaternion end)
        {
            QuaternionEx.Conjugate(ref end, out end);
            QuaternionEx.Multiply(ref end, ref start, out end);
            return QuaternionEx.GetAngleFromQuaternion(ref end);
        }
    }
}