using BEPUutilities;


namespace BEPUphysics.Paths
{
    /// <summary>
    /// Wraps a curve that is traveled along with arbitrary defined angular speed.
    /// </summary>
    /// <remarks>
    /// The speed curve should be designed with the wrapped curve's times in mind.
    /// Speeds will be sampled based on the wrapped curve's interval.</remarks>
    public class VariableAngularSpeedCurve : VariableSpeedCurve<System.Numerics.Quaternion>
    {
        /// <summary>
        /// Constructs a new variable speed curve.
        /// </summary>
        /// <param name="speedCurve">Curve defining speeds to use.</param>
        /// <param name="curve">Curve to wrap.</param>
        public VariableAngularSpeedCurve(Path<float> speedCurve, Curve<System.Numerics.Quaternion> curve)
            : base(speedCurve, curve)
        {
        }

        /// <summary>
        /// Constructs a new variable speed curve.
        /// </summary>
        /// <param name="speedCurve">Curve defining speeds to use.</param>
        /// <param name="curve">Curve to wrap.</param>
        /// <param name="sampleCount">Number of samples to use when constructing the wrapper curve.
        /// More samples increases the accuracy of the speed requirement at the cost of performance.</param>
        public VariableAngularSpeedCurve(Path<float> speedCurve, Curve<System.Numerics.Quaternion> curve, int sampleCount)
            : base(speedCurve, curve, sampleCount)
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