using System;
using System.Runtime.CompilerServices;

namespace BEPUutilities
{
    /// <summary>
    /// Provides XNA-like 4-component vector math.
    /// </summary>
    public static class Vector4Ex
    {
        /// <summary>
        /// Computes the dot product of two vectors.
        /// </summary>
        /// <param name="a">First vector in the product.</param>
        /// <param name="b">Second vector in the product.</param>
        /// <returns>Resulting dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(System.Numerics.Vector4 a, System.Numerics.Vector4 b)
        {
            return System.Numerics.Vector4.Dot(a, b);
        }

        /// <summary>
        /// Computes the dot product of two vectors.
        /// </summary>
        /// <param name="a">First vector in the product.</param>
        /// <param name="b">Second vector in the product.</param>
        /// <param name="product">Resulting dot product.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dot(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out float product)
        {
            product = System.Numerics.Vector4.Dot(a, b);
        }
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="a">First vector to add.</param>
        /// <param name="b">Second vector to add.</param>
        /// <param name="sum">Sum of the two vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out System.Numerics.Vector4 sum)
        {
            sum = a + b;
        }
        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="a">Vector to subtract from.</param>
        /// <param name="b">Vector to subtract from the first vector.</param>
        /// <param name="difference">Result of the subtraction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out System.Numerics.Vector4 difference)
        {
            difference = a - b;
        }
        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="v">Vector to scale.</param>
        /// <param name="scale">Amount to scale.</param>
        /// <param name="result">Scaled vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref System.Numerics.Vector4 v, float scale, out System.Numerics.Vector4 result)
        {
            result = v * scale;
        }
        /// <summary>
        /// Divides a vector's components by some amount.
        /// </summary>
        /// <param name="v">Vector to divide.</param>
        /// <param name="divisor">Value to divide the vector's components.</param>
        /// <param name="result">Result of the division.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(ref System.Numerics.Vector4 v, float divisor, out System.Numerics.Vector4 result)
        {
            result = v / divisor;
        }

        /// <summary>
        /// Computes the squared distance between two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <param name="distanceSquared">Squared distance between the two vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DistanceSquared(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out float distanceSquared)
        {
            distanceSquared = System.Numerics.Vector4.DistanceSquared(a, b);
        }

        /// <summary>
        /// Computes the distance between two two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <param name="distance">Distance between the two vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Distance(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out float distance)
        {
            distance = System.Numerics.Vector4.Distance(a, b);
        }

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="v">Vector to normalize.</param>
        /// <param name="result">Normalized vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(ref System.Numerics.Vector4 v, out System.Numerics.Vector4 result)
        {
            result = System.Numerics.Vector4.Normalize(v);
        }

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="v">Vector to negate.</param>
        /// <param name="negated">Negated vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Negate(ref System.Numerics.Vector4 v, out System.Numerics.Vector4 negated)
        {
            negated = System.Numerics.Vector4.Negate(v);
        }


        /// <summary>
        /// Computes the absolute value of the input vector.
        /// </summary>
        /// <param name="v">Vector to take the absolute value of.</param>
        /// <param name="result">Vector with nonnegative elements.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Abs(ref System.Numerics.Vector4 v, out System.Numerics.Vector4 result)
        {
            result = System.Numerics.Vector4.Abs(v);
        }

        /// <summary>
        /// Computes the absolute value of the input vector.
        /// </summary>
        /// <param name="v">Vector to take the absolute value of.</param>
        /// <returns>Vector with nonnegative elements.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 Abs(System.Numerics.Vector4 v)
        {
            return System.Numerics.Vector4.Abs(v);
        }

        /// <summary>
        /// Creates a vector from the lesser values in each vector.
        /// </summary>
        /// <param name="a">First input vector to compare values from.</param>
        /// <param name="b">Second input vector to compare values from.</param>
        /// <param name="min">Vector containing the lesser values of each vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Min(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out System.Numerics.Vector4 min)
        {
            min = System.Numerics.Vector4.Min(a, b);
        }

        /// <summary>
        /// Creates a vector from the lesser values in each vector.
        /// </summary>
        /// <param name="a">First input vector to compare values from.</param>
        /// <param name="b">Second input vector to compare values from.</param>
        /// <returns>Vector containing the lesser values of each vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 Min(System.Numerics.Vector4 a, System.Numerics.Vector4 b)
        {
            return System.Numerics.Vector4.Min(a, b);
        }


        /// <summary>
        /// Creates a vector from the greater values in each vector.
        /// </summary>
        /// <param name="a">First input vector to compare values from.</param>
        /// <param name="b">Second input vector to compare values from.</param>
        /// <param name="max">Vector containing the greater values of each vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Max(ref System.Numerics.Vector4 a, ref System.Numerics.Vector4 b, out System.Numerics.Vector4 max)
        {
            max = System.Numerics.Vector4.Max(a, b);
        }

        /// <summary>
        /// Creates a vector from the greater values in each vector.
        /// </summary>
        /// <param name="a">First input vector to compare values from.</param>
        /// <param name="b">Second input vector to compare values from.</param>
        /// <returns>Vector containing the greater values of each vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 Max(System.Numerics.Vector4 a, System.Numerics.Vector4 b)
        {
            return System.Numerics.Vector4.Max(a, b);
        }

        /// <summary>
        /// Computes an interpolated state between two vectors.
        /// </summary>
        /// <param name="start">Starting location of the interpolation.</param>
        /// <param name="end">Ending location of the interpolation.</param>
        /// <param name="interpolationAmount">Amount of the end location to use.</param>
        /// <returns>Interpolated intermediate state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 Lerp(System.Numerics.Vector4 start, System.Numerics.Vector4 end, float interpolationAmount)
        {
            return System.Numerics.Vector4.Lerp(start, end, interpolationAmount);
        }
        /// <summary>
        /// Computes an interpolated state between two vectors.
        /// </summary>
        /// <param name="start">Starting location of the interpolation.</param>
        /// <param name="end">Ending location of the interpolation.</param>
        /// <param name="interpolationAmount">Amount of the end location to use.</param>
        /// <param name="result">Interpolated intermediate state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Lerp(ref System.Numerics.Vector4 start, ref System.Numerics.Vector4 end, float interpolationAmount, out System.Numerics.Vector4 result)
        {
            result = System.Numerics.Vector4.Lerp(start, end, interpolationAmount);
        }

        /// <summary>
        /// Computes an intermediate location using hermite interpolation.
        /// </summary>
        /// <param name="value1">First position.</param>
        /// <param name="tangent1">Tangent associated with the first position.</param>
        /// <param name="value2">Second position.</param>
        /// <param name="tangent2">Tangent associated with the second position.</param>
        /// <param name="interpolationAmount">Amount of the second point to use.</param>
        /// <param name="result">Interpolated intermediate state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Hermite(ref System.Numerics.Vector4 value1, ref System.Numerics.Vector4 tangent1, ref System.Numerics.Vector4 value2, ref System.Numerics.Vector4 tangent2, float interpolationAmount, out System.Numerics.Vector4 result)
        {
            float weightSquared = interpolationAmount * interpolationAmount;
            float weightCubed = interpolationAmount * weightSquared;
            float value1Blend = 2 * weightCubed - 3 * weightSquared + 1;
            float tangent1Blend = weightCubed - 2 * weightSquared + interpolationAmount;
            float value2Blend = -2 * weightCubed + 3 * weightSquared;
            float tangent2Blend = weightCubed - weightSquared;
            result.X = value1.X * value1Blend + value2.X * value2Blend + tangent1.X * tangent1Blend + tangent2.X * tangent2Blend;
            result.Y = value1.Y * value1Blend + value2.Y * value2Blend + tangent1.Y * tangent1Blend + tangent2.Y * tangent2Blend;
            result.Z = value1.Z * value1Blend + value2.Z * value2Blend + tangent1.Z * tangent1Blend + tangent2.Z * tangent2Blend;
            result.W = value1.W * value1Blend + value2.W * value2Blend + tangent1.W * tangent1Blend + tangent2.W * tangent2Blend;
        }
        /// <summary>
        /// Computes an intermediate location using hermite interpolation.
        /// </summary>
        /// <param name="value1">First position.</param>
        /// <param name="tangent1">Tangent associated with the first position.</param>
        /// <param name="value2">Second position.</param>
        /// <param name="tangent2">Tangent associated with the second position.</param>
        /// <param name="interpolationAmount">Amount of the second point to use.</param>
        /// <returns>Interpolated intermediate state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 Hermite(System.Numerics.Vector4 value1, System.Numerics.Vector4 tangent1, System.Numerics.Vector4 value2, System.Numerics.Vector4 tangent2, float interpolationAmount)
        {
            System.Numerics.Vector4 toReturn;
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, interpolationAmount, out toReturn);
            return toReturn;
        }
    }
}
