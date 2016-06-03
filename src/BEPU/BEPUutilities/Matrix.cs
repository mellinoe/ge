using System;

namespace BEPUutilities
{
    /// <summary>
    /// Provides XNA-like 4x4 matrix math.
    /// </summary>
    public static class Matrix4x4Ex
    {
        /// <summary>
        /// Gets or sets the backward vector of the matrix.
        /// </summary>
        public static System.Numerics.Vector3 GetBackward(this System.Numerics.Matrix4x4 mat)
        {
            System.Numerics.Vector3 vector;
            vector.X = mat.M31;
            vector.Y = mat.M32;
            vector.Z = mat.M33;
            return vector;
        }

        /// <summary>
        /// Gets or sets the backward vector of the matrix.
        /// </summary>
        public static void SetBackward(this System.Numerics.Matrix4x4 mat, System.Numerics.Vector3 value)
        {
            mat.M31 = value.X;
            mat.M32 = value.Y;
            mat.M33 = value.Z;
        }

        /// <summary>
        /// Gets or sets the down vector of the matrix.
        /// </summary>
        public static System.Numerics.Vector3 GetDown(this System.Numerics.Matrix4x4 mat)
        {
            System.Numerics.Vector3 vector;
            vector.X = -mat.M21;
            vector.Y = -mat.M22;
            vector.Z = -mat.M23;
            return vector;
        }

        /// <summary>
        /// Gets or sets the down vector of the matrix.
        /// </summary>
        public static void SetDown(this System.Numerics.Matrix4x4 mat, System.Numerics.Vector3 value)
        {
            mat.M21 = -value.X;
            mat.M22 = -value.Y;
            mat.M23 = -value.Z;
        }

        /// <summary>
        /// Gets or sets the forward vector of the matrix.
        /// </summary>
        public static System.Numerics.Vector3 GetForward(this System.Numerics.Matrix4x4 mat)
        {
            System.Numerics.Vector3 vector;
            vector.X = -mat.M31;
            vector.Y = -mat.M32;
            vector.Z = -mat.M33;
            return vector;
        }

        /// <summary>
        /// Gets or sets the forward vector of the matrix.
        /// </summary>
        public static void SetForward(this System.Numerics.Matrix4x4 mat, System.Numerics.Vector3 value)
        {
            mat.M31 = -value.X;
            mat.M32 = -value.Y;
            mat.M33 = -value.Z;
        }

        /// <summary>
        /// Gets or sets the left vector of the matrix.
        /// </summary>
        public static System.Numerics.Vector3 GetLeft(this System.Numerics.Matrix4x4 mat)
        {
            System.Numerics.Vector3 vector;
            vector.X = -mat.M11;
            vector.Y = -mat.M12;
            vector.Z = -mat.M13;
            return vector;
        }

        /// <summary>
        /// Gets or sets the left vector of the matrix.
        /// </summary>
        public static void SetLeft(this System.Numerics.Matrix4x4 mat, System.Numerics.Vector3 value)
        {
            mat.M11 = -value.X;
            mat.M12 = -value.Y;
            mat.M13 = -value.Z;
        }

        /// <summary>
        /// Gets or sets the right vector of the matrix.
        /// </summary>
        public static System.Numerics.Vector3 GetRight(this System.Numerics.Matrix4x4 mat)
        {
            System.Numerics.Vector3 vector;
            vector.X = mat.M11;
            vector.Y = mat.M12;
            vector.Z = mat.M13;
            return vector;
        }

        /// <summary>
        /// Gets or sets the right vector of the matrix.
        /// </summary>
        public static void SetRight(this System.Numerics.Matrix4x4 mat, System.Numerics.Vector3 value)
        {
            mat.M11 = value.X;
            mat.M12 = value.Y;
            mat.M13 = value.Z;
        }

        /// <summary>
        /// Gets or sets the up vector of the matrix.
        /// </summary>
        public static System.Numerics.Vector3 GetUp(this System.Numerics.Matrix4x4 mat)
        {
                System.Numerics.Vector3 vector;
                vector.X = mat.M21;
                vector.Y = mat.M22;
                vector.Z = mat.M23;
                return vector;
        }

        /// <summary>
        /// Gets or sets the up vector of the matrix.
        /// </summary>
        public static void SetUp(this System.Numerics.Matrix4x4 mat, System.Numerics.Vector3 value)
        {
                mat.M21 = value.X;
                mat.M22 = value.Y;
                mat.M23 = value.Z;
        }




        /// <summary>
        /// Creates a matrix representing the given axis and angle rotation.
        /// </summary>
        /// <param name="axis">Axis around which to rotate.</param>
        /// <param name="angle">Angle to rotate around the axis.</param>
        /// <returns>System.Numerics.Matrix4x4 created from the axis and angle.</returns>
        public static System.Numerics.Matrix4x4 CreateFromAxisAngle(System.Numerics.Vector3 axis, float angle)
        {
            System.Numerics.Matrix4x4 toReturn;
            CreateFromAxisAngle(ref axis, angle, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Creates a matrix representing the given axis and angle rotation.
        /// </summary>
        /// <param name="axis">Axis around which to rotate.</param>
        /// <param name="angle">Angle to rotate around the axis.</param>
        /// <param name="result">System.Numerics.Matrix4x4 created from the axis and angle.</param>
        public static void CreateFromAxisAngle(ref System.Numerics.Vector3 axis, float angle, out System.Numerics.Matrix4x4 result)
        {
            float xx = axis.X * axis.X;
            float yy = axis.Y * axis.Y;
            float zz = axis.Z * axis.Z;
            float xy = axis.X * axis.Y;
            float xz = axis.X * axis.Z;
            float yz = axis.Y * axis.Z;

            float sinAngle = (float)Math.Sin(angle);
            float oneMinusCosAngle = 1 - (float)Math.Cos(angle);

            result.M11 = 1 + oneMinusCosAngle * (xx - 1);
            result.M21 = -axis.Z * sinAngle + oneMinusCosAngle * xy;
            result.M31 = axis.Y * sinAngle + oneMinusCosAngle * xz;
            result.M41 = 0;

            result.M12 = axis.Z * sinAngle + oneMinusCosAngle * xy;
            result.M22 = 1 + oneMinusCosAngle * (yy - 1);
            result.M32 = -axis.X * sinAngle + oneMinusCosAngle * yz;
            result.M42 = 0;

            result.M13 = -axis.Y * sinAngle + oneMinusCosAngle * xz;
            result.M23 = axis.X * sinAngle + oneMinusCosAngle * yz;
            result.M33 = 1 + oneMinusCosAngle * (zz - 1);
            result.M43 = 0;

            result.M14 = 0;
            result.M24 = 0;
            result.M34 = 0;
            result.M44 = 1;
        }

        /// <summary>
        /// Creates a rotation matrix from a quaternion.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to convert.</param>
        /// <param name="result">Rotation matrix created from the quaternion.</param>
        public static void CreateFromQuaternion(ref System.Numerics.Quaternion quaternion, out System.Numerics.Matrix4x4 result)
        {
            float qX2 = quaternion.X + quaternion.X;
            float qY2 = quaternion.Y + quaternion.Y;
            float qZ2 = quaternion.Z + quaternion.Z;
            float XX = qX2 * quaternion.X;
            float YY = qY2 * quaternion.Y;
            float ZZ = qZ2 * quaternion.Z;
            float XY = qX2 * quaternion.Y;
            float XZ = qX2 * quaternion.Z;
            float XW = qX2 * quaternion.W;
            float YZ = qY2 * quaternion.Z;
            float YW = qY2 * quaternion.W;
            float ZW = qZ2 * quaternion.W;

            result.M11 = 1 - YY - ZZ;
            result.M21 = XY - ZW;
            result.M31 = XZ + YW;
            result.M41 = 0;

            result.M12 = XY + ZW;
            result.M22 = 1 - XX - ZZ;
            result.M32 = YZ - XW;
            result.M42 = 0;

            result.M13 = XZ - YW;
            result.M23 = YZ + XW;
            result.M33 = 1 - XX - YY;
            result.M43 = 0;

            result.M14 = 0;
            result.M24 = 0;
            result.M34 = 0;
            result.M44 = 1;
        }

        /// <summary>
        /// Creates a rotation matrix from a quaternion.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to convert.</param>
        /// <returns>Rotation matrix created from the quaternion.</returns>
        public static System.Numerics.Matrix4x4 CreateFromQuaternion(System.Numerics.Quaternion quaternion)
        {
            System.Numerics.Matrix4x4 toReturn;
            CreateFromQuaternion(ref quaternion, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Multiplies two matrices together.
        /// </summary>
        /// <param name="a">First matrix to multiply.</param>
        /// <param name="b">Second matrix to multiply.</param>
        /// <param name="result">Combined transformation.</param>
        public static void Multiply(ref System.Numerics.Matrix4x4 a, ref System.Numerics.Matrix4x4 b, out System.Numerics.Matrix4x4 result)
        {
            float resultM11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            float resultM12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            float resultM13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            float resultM14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            float resultM21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            float resultM22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            float resultM23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            float resultM24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            float resultM31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            float resultM32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            float resultM33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            float resultM34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            float resultM41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            float resultM42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            float resultM43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            float resultM44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;

            result.M11 = resultM11;
            result.M12 = resultM12;
            result.M13 = resultM13;
            result.M14 = resultM14;

            result.M21 = resultM21;
            result.M22 = resultM22;
            result.M23 = resultM23;
            result.M24 = resultM24;

            result.M31 = resultM31;
            result.M32 = resultM32;
            result.M33 = resultM33;
            result.M34 = resultM34;

            result.M41 = resultM41;
            result.M42 = resultM42;
            result.M43 = resultM43;
            result.M44 = resultM44;
        }


        /// <summary>
        /// Multiplies two matrices together.
        /// </summary>
        /// <param name="a">First matrix to multiply.</param>
        /// <param name="b">Second matrix to multiply.</param>
        /// <returns>Combined transformation.</returns>
        public static System.Numerics.Matrix4x4 Multiply(System.Numerics.Matrix4x4 a, System.Numerics.Matrix4x4 b)
        {
            System.Numerics.Matrix4x4 result;
            Multiply(ref a, ref b, out result);
            return result;
        }


        /// <summary>
        /// Scales all components of the matrix.
        /// </summary>
        /// <param name="matrix">System.Numerics.Matrix4x4 to scale.</param>
        /// <param name="scale">Amount to scale.</param>
        /// <param name="result">Scaled matrix.</param>
        public static void Multiply(ref System.Numerics.Matrix4x4 matrix, float scale, out System.Numerics.Matrix4x4 result)
        {
            result.M11 = matrix.M11 * scale;
            result.M12 = matrix.M12 * scale;
            result.M13 = matrix.M13 * scale;
            result.M14 = matrix.M14 * scale;

            result.M21 = matrix.M21 * scale;
            result.M22 = matrix.M22 * scale;
            result.M23 = matrix.M23 * scale;
            result.M24 = matrix.M24 * scale;

            result.M31 = matrix.M31 * scale;
            result.M32 = matrix.M32 * scale;
            result.M33 = matrix.M33 * scale;
            result.M34 = matrix.M34 * scale;

            result.M41 = matrix.M41 * scale;
            result.M42 = matrix.M42 * scale;
            result.M43 = matrix.M43 * scale;
            result.M44 = matrix.M44 * scale;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void Transform(ref System.Numerics.Vector4 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector4 result)
        {
            float vX = v.X;
            float vY = v.Y;
            float vZ = v.Z;
            float vW = v.W;
            result.X = vX * matrix.M11 + vY * matrix.M21 + vZ * matrix.M31 + vW * matrix.M41;
            result.Y = vX * matrix.M12 + vY * matrix.M22 + vZ * matrix.M32 + vW * matrix.M42;
            result.Z = vX * matrix.M13 + vY * matrix.M23 + vZ * matrix.M33 + vW * matrix.M43;
            result.W = vX * matrix.M14 + vY * matrix.M24 + vZ * matrix.M34 + vW * matrix.M44;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector4 v, System.Numerics.Matrix4x4 matrix)
        {
            System.Numerics.Vector4 toReturn;
            Transform(ref v, ref matrix, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformTranspose(ref System.Numerics.Vector4 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector4 result)
        {
            float vX = v.X;
            float vY = v.Y;
            float vZ = v.Z;
            float vW = v.W;
            result.X = vX * matrix.M11 + vY * matrix.M12 + vZ * matrix.M13 + vW * matrix.M14;
            result.Y = vX * matrix.M21 + vY * matrix.M22 + vZ * matrix.M23 + vW * matrix.M24;
            result.Z = vX * matrix.M31 + vY * matrix.M32 + vZ * matrix.M33 + vW * matrix.M34;
            result.W = vX * matrix.M41 + vY * matrix.M42 + vZ * matrix.M43 + vW * matrix.M44;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector4 TransformTranspose(System.Numerics.Vector4 v, System.Numerics.Matrix4x4 matrix)
        {
            System.Numerics.Vector4 toReturn;
            TransformTranspose(ref v, ref matrix, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void Transform(ref System.Numerics.Vector3 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector4 result)
        {
            result.X = v.X * matrix.M11 + v.Y * matrix.M21 + v.Z * matrix.M31 + matrix.M41;
            result.Y = v.X * matrix.M12 + v.Y * matrix.M22 + v.Z * matrix.M32 + matrix.M42;
            result.Z = v.X * matrix.M13 + v.Y * matrix.M23 + v.Z * matrix.M33 + matrix.M43;
            result.W = v.X * matrix.M14 + v.Y * matrix.M24 + v.Z * matrix.M34 + matrix.M44;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector4 Transform(System.Numerics.Vector3 v, System.Numerics.Matrix4x4 matrix)
        {
            System.Numerics.Vector4 toReturn;
            Transform(ref v, ref matrix, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformTranspose(ref System.Numerics.Vector3 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector4 result)
        {
            result.X = v.X * matrix.M11 + v.Y * matrix.M12 + v.Z * matrix.M13 + matrix.M14;
            result.Y = v.X * matrix.M21 + v.Y * matrix.M22 + v.Z * matrix.M23 + matrix.M24;
            result.Z = v.X * matrix.M31 + v.Y * matrix.M32 + v.Z * matrix.M33 + matrix.M34;
            result.W = v.X * matrix.M41 + v.Y * matrix.M42 + v.Z * matrix.M43 + matrix.M44;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector4 TransformTranspose(System.Numerics.Vector3 v, System.Numerics.Matrix4x4 matrix)
        {
            System.Numerics.Vector4 toReturn;
            TransformTranspose(ref v, ref matrix, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void Transform(ref System.Numerics.Vector3 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector3 result)
        {
            float vX = v.X;
            float vY = v.Y;
            float vZ = v.Z;
            result.X = vX * matrix.M11 + vY * matrix.M21 + vZ * matrix.M31 + matrix.M41;
            result.Y = vX * matrix.M12 + vY * matrix.M22 + vZ * matrix.M32 + matrix.M42;
            result.Z = vX * matrix.M13 + vY * matrix.M23 + vZ * matrix.M33 + matrix.M43;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformTranspose(ref System.Numerics.Vector3 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector3 result)
        {
            float vX = v.X;
            float vY = v.Y;
            float vZ = v.Z;
            result.X = vX * matrix.M11 + vY * matrix.M12 + vZ * matrix.M13 + matrix.M14;
            result.Y = vX * matrix.M21 + vY * matrix.M22 + vZ * matrix.M23 + matrix.M24;
            result.Z = vX * matrix.M31 + vY * matrix.M32 + vZ * matrix.M33 + matrix.M34;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformNormal(ref System.Numerics.Vector3 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector3 result)
        {
            float vX = v.X;
            float vY = v.Y;
            float vZ = v.Z;
            result.X = vX * matrix.M11 + vY * matrix.M21 + vZ * matrix.M31 + matrix.M41;
            result.Y = vX * matrix.M12 + vY * matrix.M22 + vZ * matrix.M32 + matrix.M42;
            result.Z = vX * matrix.M13 + vY * matrix.M23 + vZ * matrix.M33 + matrix.M43;
        }

        /// <summary>
        /// Transforms a vector using a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector3 TransformNormal(System.Numerics.Vector3 v, System.Numerics.Matrix4x4 matrix)
        {
            System.Numerics.Vector3 toReturn;
            Transform(ref v, ref matrix, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformNormalTranspose(ref System.Numerics.Vector3 v, ref System.Numerics.Matrix4x4 matrix, out System.Numerics.Vector3 result)
        {
            float vX = v.X;
            float vY = v.Y;
            float vZ = v.Z;
            result.X = vX * matrix.M11 + vY * matrix.M12 + vZ * matrix.M13 + matrix.M14;
            result.Y = vX * matrix.M21 + vY * matrix.M22 + vZ * matrix.M23 + matrix.M24;
            result.Z = vX * matrix.M31 + vY * matrix.M32 + vZ * matrix.M33 + matrix.M34;
        }

        /// <summary>
        /// Transforms a vector using the transpose of a matrix.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="matrix">Transform to tranpose and apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector3 TransformNormalTranspose(System.Numerics.Vector3 v, System.Numerics.Matrix4x4 matrix)
        {
            System.Numerics.Vector3 toReturn;
            TransformTranspose(ref v, ref matrix, out toReturn);
            return toReturn;
        }


        /// <summary>
        /// Transposes the matrix.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to transpose.</param>
        /// <param name="transposed">System.Numerics.Matrix4x4 to transpose.</param>
        public static void Transpose(ref System.Numerics.Matrix4x4 m, out System.Numerics.Matrix4x4 transposed)
        {
            float intermediate = m.M12;
            transposed.M12 = m.M21;
            transposed.M21 = intermediate;

            intermediate = m.M13;
            transposed.M13 = m.M31;
            transposed.M31 = intermediate;

            intermediate = m.M14;
            transposed.M14 = m.M41;
            transposed.M41 = intermediate;

            intermediate = m.M23;
            transposed.M23 = m.M32;
            transposed.M32 = intermediate;

            intermediate = m.M24;
            transposed.M24 = m.M42;
            transposed.M42 = intermediate;

            intermediate = m.M34;
            transposed.M34 = m.M43;
            transposed.M43 = intermediate;

            transposed.M11 = m.M11;
            transposed.M22 = m.M22;
            transposed.M33 = m.M33;
            transposed.M44 = m.M44;
        }

        /// <summary>
        /// Inverts the matrix.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to invert.</param>
        /// <param name="inverted">Inverted version of the matrix.</param>
        public static void Invert(ref System.Numerics.Matrix4x4 m, out System.Numerics.Matrix4x4 inverted)
        {
            float s0 = m.M11 * m.M22 - m.M21 * m.M12;
            float s1 = m.M11 * m.M23 - m.M21 * m.M13;
            float s2 = m.M11 * m.M24 - m.M21 * m.M14;
            float s3 = m.M12 * m.M23 - m.M22 * m.M13;
            float s4 = m.M12 * m.M24 - m.M22 * m.M14;
            float s5 = m.M13 * m.M24 - m.M23 * m.M14;

            float c5 = m.M33 * m.M44 - m.M43 * m.M34;
            float c4 = m.M32 * m.M44 - m.M42 * m.M34;
            float c3 = m.M32 * m.M43 - m.M42 * m.M33;
            float c2 = m.M31 * m.M44 - m.M41 * m.M34;
            float c1 = m.M31 * m.M43 - m.M41 * m.M33;
            float c0 = m.M31 * m.M42 - m.M41 * m.M32;

            float inverseDeterminant = 1.0f / (s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0);

            float m11 = m.M11;
            float m12 = m.M12;
            float m13 = m.M13;
            float m14 = m.M14;
            float m21 = m.M21;
            float m22 = m.M22;
            float m23 = m.M23;
            float m31 = m.M31;
            float m32 = m.M32;
            float m33 = m.M33;

            float m41 = m.M41;
            float m42 = m.M42;

            inverted.M11 = (m.M22 * c5 - m.M23 * c4 + m.M24 * c3) * inverseDeterminant;
            inverted.M12 = (-m.M12 * c5 + m.M13 * c4 - m.M14 * c3) * inverseDeterminant;
            inverted.M13 = (m.M42 * s5 - m.M43 * s4 + m.M44 * s3) * inverseDeterminant;
            inverted.M14 = (-m.M32 * s5 + m.M33 * s4 - m.M34 * s3) * inverseDeterminant;

            inverted.M21 = (-m.M21 * c5 + m.M23 * c2 - m.M24 * c1) * inverseDeterminant;
            inverted.M22 = (m11 * c5 - m13 * c2 + m14 * c1) * inverseDeterminant;
            inverted.M23 = (-m.M41 * s5 + m.M43 * s2 - m.M44 * s1) * inverseDeterminant;
            inverted.M24 = (m.M31 * s5 - m.M33 * s2 + m.M34 * s1) * inverseDeterminant;

            inverted.M31 = (m21 * c4 - m22 * c2 + m.M24 * c0) * inverseDeterminant;
            inverted.M32 = (-m11 * c4 + m12 * c2 - m14 * c0) * inverseDeterminant;
            inverted.M33 = (m.M41 * s4 - m.M42 * s2 + m.M44 * s0) * inverseDeterminant;
            inverted.M34 = (-m31 * s4 + m32 * s2 - m.M34 * s0) * inverseDeterminant;

            inverted.M41 = (-m21 * c3 + m22 * c1 - m23 * c0) * inverseDeterminant;
            inverted.M42 = (m11 * c3 - m12 * c1 + m13 * c0) * inverseDeterminant;
            inverted.M43 = (-m41 * s3 + m42 * s1 - m.M43 * s0) * inverseDeterminant;
            inverted.M44 = (m31 * s3 - m32 * s1 + m33 * s0) * inverseDeterminant;
        }

        /// <summary>
        /// Inverts the matrix.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to invert.</param>
        /// <returns>Inverted version of the matrix.</returns>
        public static System.Numerics.Matrix4x4 Invert(System.Numerics.Matrix4x4 m)
        {
            System.Numerics.Matrix4x4 inverted;
            Invert(ref m, out inverted);
            return inverted;
        }

        /// <summary>
        /// Inverts the matrix using a process that only works for affine transforms (3x3 linear transform and translation).
        /// Ignores the M14, M24, M34, and M44 elements of the input matrix.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to invert.</param>
        /// <param name="inverted">Inverted version of the matrix.</param>
        public static void InvertAffine(ref System.Numerics.Matrix4x4 m, out System.Numerics.Matrix4x4 inverted)
        {
            //Invert the upper left 3x3 linear transform.

            //Compute the upper left 3x3 determinant. Some potential for microoptimization here.
            float determinantInverse = 1 /
                (m.M11 * m.M22 * m.M33 + m.M12 * m.M23 * m.M31 + m.M13 * m.M21 * m.M32 -
                 m.M31 * m.M22 * m.M13 - m.M32 * m.M23 * m.M11 - m.M33 * m.M21 * m.M12);

            float m11 = (m.M22 * m.M33 - m.M23 * m.M32) * determinantInverse;
            float m12 = (m.M13 * m.M32 - m.M33 * m.M12) * determinantInverse;
            float m13 = (m.M12 * m.M23 - m.M22 * m.M13) * determinantInverse;

            float m21 = (m.M23 * m.M31 - m.M21 * m.M33) * determinantInverse;
            float m22 = (m.M11 * m.M33 - m.M13 * m.M31) * determinantInverse;
            float m23 = (m.M13 * m.M21 - m.M11 * m.M23) * determinantInverse;

            float m31 = (m.M21 * m.M32 - m.M22 * m.M31) * determinantInverse;
            float m32 = (m.M12 * m.M31 - m.M11 * m.M32) * determinantInverse;
            float m33 = (m.M11 * m.M22 - m.M12 * m.M21) * determinantInverse;

            inverted.M11 = m11;
            inverted.M12 = m12;
            inverted.M13 = m13;

            inverted.M21 = m21;
            inverted.M22 = m22;
            inverted.M23 = m23;

            inverted.M31 = m31;
            inverted.M32 = m32;
            inverted.M33 = m33;

            //Translation component
            var vX = m.M41;
            var vY = m.M42;
            var vZ = m.M43;
            inverted.M41 = -(vX * inverted.M11 + vY * inverted.M21 + vZ * inverted.M31);
            inverted.M42 = -(vX * inverted.M12 + vY * inverted.M22 + vZ * inverted.M32);
            inverted.M43 = -(vX * inverted.M13 + vY * inverted.M23 + vZ * inverted.M33);

            //Last chunk.
            inverted.M14 = 0;
            inverted.M24 = 0;
            inverted.M34 = 0;
            inverted.M44 = 1;
        }

        /// <summary>
        /// Inverts the matrix using a process that only works for affine transforms (3x3 linear transform and translation).
        /// Ignores the M14, M24, M34, and M44 elements of the input matrix.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to invert.</param>
        /// <returns>Inverted version of the matrix.</returns>
        public static System.Numerics.Matrix4x4 InvertAffine(System.Numerics.Matrix4x4 m)
        {
            System.Numerics.Matrix4x4 inverted;
            InvertAffine(ref m, out inverted);
            return inverted;
        }

        /// <summary>
        /// Inverts the matrix using a process that only works for rigid transforms.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to invert.</param>
        /// <param name="inverted">Inverted version of the matrix.</param>
        public static void InvertRigid(ref System.Numerics.Matrix4x4 m, out System.Numerics.Matrix4x4 inverted)
        {
            //Invert (transpose) the upper left 3x3 rotation.
            float intermediate = m.M12;
            inverted.M12 = m.M21;
            inverted.M21 = intermediate;

            intermediate = m.M13;
            inverted.M13 = m.M31;
            inverted.M31 = intermediate;

            intermediate = m.M23;
            inverted.M23 = m.M32;
            inverted.M32 = intermediate;

            inverted.M11 = m.M11;
            inverted.M22 = m.M22;
            inverted.M33 = m.M33;

            //Translation component
            var vX = m.M41;
            var vY = m.M42;
            var vZ = m.M43;
            inverted.M41 = -(vX * inverted.M11 + vY * inverted.M21 + vZ * inverted.M31);
            inverted.M42 = -(vX * inverted.M12 + vY * inverted.M22 + vZ * inverted.M32);
            inverted.M43 = -(vX * inverted.M13 + vY * inverted.M23 + vZ * inverted.M33);

            //Last chunk.
            inverted.M14 = 0;
            inverted.M24 = 0;
            inverted.M34 = 0;
            inverted.M44 = 1;
        }

        /// <summary>
        /// Inverts the matrix using a process that only works for rigid transforms.
        /// </summary>
        /// <param name="m">System.Numerics.Matrix4x4 to invert.</param>
        /// <returns>Inverted version of the matrix.</returns>
        public static System.Numerics.Matrix4x4 InvertRigid(System.Numerics.Matrix4x4 m)
        {
            System.Numerics.Matrix4x4 inverse;
            InvertRigid(ref m, out inverse);
            return inverse;
        }

        /// <summary>
        /// Gets the 4x4 identity matrix.
        /// </summary>
        public static System.Numerics.Matrix4x4 Identity
        {
            get
            {
                System.Numerics.Matrix4x4 toReturn;
                toReturn.M11 = 1;
                toReturn.M12 = 0;
                toReturn.M13 = 0;
                toReturn.M14 = 0;

                toReturn.M21 = 0;
                toReturn.M22 = 1;
                toReturn.M23 = 0;
                toReturn.M24 = 0;

                toReturn.M31 = 0;
                toReturn.M32 = 0;
                toReturn.M33 = 1;
                toReturn.M34 = 0;

                toReturn.M41 = 0;
                toReturn.M42 = 0;
                toReturn.M43 = 0;
                toReturn.M44 = 1;
                return toReturn;
            }
        }

        /// <summary>
        /// Creates a right handed orthographic projection.
        /// </summary>
        /// <param name="left">Leftmost coordinate of the projected area.</param>
        /// <param name="right">Rightmost coordinate of the projected area.</param>
        /// <param name="bottom">Bottom coordinate of the projected area.</param>
        /// <param name="top">Top coordinate of the projected area.</param>
        /// <param name="zNear">Near plane of the projection.</param>
        /// <param name="zFar">Far plane of the projection.</param>
        /// <param name="projection">The resulting orthographic projection matrix.</param>
        public static void CreateOrthographicRH(float left, float right, float bottom, float top, float zNear, float zFar, out System.Numerics.Matrix4x4 projection)
        {
            float width = right - left;
            float height = top - bottom;
            float depth = zFar - zNear;
            projection.M11 = 2f / width;
            projection.M12 = 0;
            projection.M13 = 0;
            projection.M14 = 0;

            projection.M21 = 0;
            projection.M22 = 2f / height;
            projection.M23 = 0;
            projection.M24 = 0;

            projection.M31 = 0;
            projection.M32 = 0;
            projection.M33 = -1f / depth;
            projection.M34 = 0;

            projection.M41 = (left + right) / -width;
            projection.M42 = (top + bottom) / -height;
            projection.M43 = zNear / -depth;
            projection.M44 = 1f;

        }

        /// <summary>
        /// Creates a right-handed perspective matrix.
        /// </summary>
        /// <param name="fieldOfView">Field of view of the perspective in radians.</param>
        /// <param name="aspectRatio">Width of the viewport over the height of the viewport.</param>
        /// <param name="nearClip">Near clip plane of the perspective.</param>
        /// <param name="farClip">Far clip plane of the perspective.</param>
        /// <param name="perspective">Resulting perspective matrix.</param>
        public static void CreatePerspectiveFieldOfViewRH(float fieldOfView, float aspectRatio, float nearClip, float farClip, out System.Numerics.Matrix4x4 perspective)
        {
            float h = 1f / ((float)Math.Tan(fieldOfView * 0.5f));
            float w = h / aspectRatio;
            perspective.M11 = w;
            perspective.M12 = 0;
            perspective.M13 = 0;
            perspective.M14 = 0;

            perspective.M21 = 0;
            perspective.M22 = h;
            perspective.M23 = 0;
            perspective.M24 = 0;

            perspective.M31 = 0;
            perspective.M32 = 0;
            perspective.M33 = farClip / (nearClip - farClip);
            perspective.M34 = -1;

            perspective.M41 = 0;
            perspective.M42 = 0;
            perspective.M44 = 0;
            perspective.M43 = nearClip * perspective.M33;

        }

        /// <summary>
        /// Creates a right-handed perspective matrix.
        /// </summary>
        /// <param name="fieldOfView">Field of view of the perspective in radians.</param>
        /// <param name="aspectRatio">Width of the viewport over the height of the viewport.</param>
        /// <param name="nearClip">Near clip plane of the perspective.</param>
        /// <param name="farClip">Far clip plane of the perspective.</param>
        /// <returns>Resulting perspective matrix.</returns>
        public static System.Numerics.Matrix4x4 CreatePerspectiveFieldOfViewRH(float fieldOfView, float aspectRatio, float nearClip, float farClip)
        {
            System.Numerics.Matrix4x4 perspective;
            CreatePerspectiveFieldOfViewRH(fieldOfView, aspectRatio, nearClip, farClip, out perspective);
            return perspective;
        }

        /// <summary>
        /// Creates a view matrix pointing from a position to a target with the given up vector.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="target">Target of the camera.</param>
        /// <param name="upVector">Up vector of the camera.</param>
        /// <param name="viewSystem.Numerics.Matrix4x4">Look at matrix.</param>
        public static void CreateLookAtRH(ref System.Numerics.Vector3 position, ref System.Numerics.Vector3 target, ref System.Numerics.Vector3 upVector, out System.Numerics.Matrix4x4 viewSystemMatrix4x4)
        {
            System.Numerics.Vector3 forward;
            Vector3Ex.Subtract(ref target, ref position, out forward);
            CreateViewRH(ref position, ref forward, ref upVector, out viewSystemMatrix4x4);
        }

        /// <summary>
        /// Creates a view matrix pointing from a position to a target with the given up vector.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="target">Target of the camera.</param>
        /// <param name="upVector">Up vector of the camera.</param>
        /// <returns>Look at matrix.</returns>
        public static System.Numerics.Matrix4x4 CreateLookAtRH(System.Numerics.Vector3 position, System.Numerics.Vector3 target, System.Numerics.Vector3 upVector)
        {
            System.Numerics.Matrix4x4 lookAt;
            System.Numerics.Vector3 forward;
            Vector3Ex.Subtract(ref target, ref position, out forward);
            CreateViewRH(ref position, ref forward, ref upVector, out lookAt);
            return lookAt;
        }


        /// <summary>
        /// Creates a view matrix pointing looking in a direction with a given up vector.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="forward">Forward direction of the camera.</param>
        /// <param name="upVector">Up vector of the camera.</param>
        /// <param name="viewSystem.Numerics.Matrix4x4">Look at matrix.</param>
        public static void CreateViewRH(ref System.Numerics.Vector3 position, ref System.Numerics.Vector3 forward, ref System.Numerics.Vector3 upVector, out System.Numerics.Matrix4x4 viewSystemMatrix4x4)
        {
            System.Numerics.Vector3 z;
            float length = forward.Length();
            Vector3Ex.Divide(ref forward, -length, out z);
            System.Numerics.Vector3 x;
            Vector3Ex.Cross(ref upVector, ref z, out x);
            x.Normalize();
            System.Numerics.Vector3 y;
            Vector3Ex.Cross(ref z, ref x, out y);

            viewSystemMatrix4x4.M11 = x.X;
            viewSystemMatrix4x4.M12 = y.X;
            viewSystemMatrix4x4.M13 = z.X;
            viewSystemMatrix4x4.M14 = 0f;
            viewSystemMatrix4x4.M21 = x.Y;
            viewSystemMatrix4x4.M22 = y.Y;
            viewSystemMatrix4x4.M23 = z.Y;
            viewSystemMatrix4x4.M24 = 0f;
            viewSystemMatrix4x4.M31 = x.Z;
            viewSystemMatrix4x4.M32 = y.Z;
            viewSystemMatrix4x4.M33 = z.Z;
            viewSystemMatrix4x4.M34 = 0f;
            Vector3Ex.Dot(ref x, ref position, out viewSystemMatrix4x4.M41);
            Vector3Ex.Dot(ref y, ref position, out viewSystemMatrix4x4.M42);
            Vector3Ex.Dot(ref z, ref position, out viewSystemMatrix4x4.M43);
            viewSystemMatrix4x4.M41 = -viewSystemMatrix4x4.M41;
            viewSystemMatrix4x4.M42 = -viewSystemMatrix4x4.M42;
            viewSystemMatrix4x4.M43 = -viewSystemMatrix4x4.M43;
            viewSystemMatrix4x4.M44 = 1f;

        }

        /// <summary>
        /// Creates a view matrix pointing looking in a direction with a given up vector.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="forward">Forward direction of the camera.</param>
        /// <param name="upVector">Up vector of the camera.</param>
        /// <returns>Look at matrix.</returns>
        public static System.Numerics.Matrix4x4 CreateViewRH(System.Numerics.Vector3 position, System.Numerics.Vector3 forward, System.Numerics.Vector3 upVector)
        {
            System.Numerics.Matrix4x4 lookat;
            CreateViewRH(ref position, ref forward, ref upVector, out lookat);
            return lookat;
        }



        /// <summary>
        /// Creates a world matrix pointing from a position to a target with the given up vector.
        /// </summary>
        /// <param name="position">Position of the transform.</param>
        /// <param name="forward">Forward direction of the transformation.</param>
        /// <param name="upVector">Up vector which is crossed against the forward vector to compute the transform's basis.</param>
        /// <param name="worldSystemMatrix4x4">World matrix.</param>
        public static void CreateWorldRH(ref System.Numerics.Vector3 position, ref System.Numerics.Vector3 forward, ref System.Numerics.Vector3 upVector, out System.Numerics.Matrix4x4 worldSystemMatrix4x4)
        {
            System.Numerics.Vector3 z;
            float length = forward.Length();
            Vector3Ex.Divide(ref forward, -length, out z);
            System.Numerics.Vector3 x;
            Vector3Ex.Cross(ref upVector, ref z, out x);
            x.Normalize();
            System.Numerics.Vector3 y;
            Vector3Ex.Cross(ref z, ref x, out y);

            worldSystemMatrix4x4.M11 = x.X;
            worldSystemMatrix4x4.M12 = x.Y;
            worldSystemMatrix4x4.M13 = x.Z;
            worldSystemMatrix4x4.M14 = 0f;
            worldSystemMatrix4x4.M21 = y.X;
            worldSystemMatrix4x4.M22 = y.Y;
            worldSystemMatrix4x4.M23 = y.Z;
            worldSystemMatrix4x4.M24 = 0f;
            worldSystemMatrix4x4.M31 = z.X;
            worldSystemMatrix4x4.M32 = z.Y;
            worldSystemMatrix4x4.M33 = z.Z;
            worldSystemMatrix4x4.M34 = 0f;

            worldSystemMatrix4x4.M41 = position.X;
            worldSystemMatrix4x4.M42 = position.Y;
            worldSystemMatrix4x4.M43 = position.Z;
            worldSystemMatrix4x4.M44 = 1f;

        }


        /// <summary>
        /// Creates a world matrix pointing from a position to a target with the given up vector.
        /// </summary>
        /// <param name="position">Position of the transform.</param>
        /// <param name="forward">Forward direction of the transformation.</param>
        /// <param name="upVector">Up vector which is crossed against the forward vector to compute the transform's basis.</param>
        /// <returns>World matrix.</returns>
        public static System.Numerics.Matrix4x4 CreateWorldRH(System.Numerics.Vector3 position, System.Numerics.Vector3 forward, System.Numerics.Vector3 upVector)
        {
            System.Numerics.Matrix4x4 lookat;
            CreateWorldRH(ref position, ref forward, ref upVector, out lookat);
            return lookat;
        }



        /// <summary>
        /// Creates a matrix representing a translation.
        /// </summary>
        /// <param name="translation">Translation to be represented by the matrix.</param>
        /// <param name="translationSystem.Numerics.Matrix4x4">System.Numerics.Matrix4x4 representing the given translation.</param>
        public static void CreateTranslation(ref System.Numerics.Vector3 translation, out System.Numerics.Matrix4x4 translationSystemMatrix4x4)
        {
            translationSystemMatrix4x4 = new System.Numerics.Matrix4x4
            {
                M11 = 1,
                M22 = 1,
                M33 = 1,
                M44 = 1,
                M41 = translation.X,
                M42 = translation.Y,
                M43 = translation.Z
            };
        }

        /// <summary>
        /// Creates a matrix representing a translation.
        /// </summary>
        /// <param name="translation">Translation to be represented by the matrix.</param>
        /// <returns>System.Numerics.Matrix4x4 representing the given translation.</returns>
        public static System.Numerics.Matrix4x4 CreateTranslation(System.Numerics.Vector3 translation)
        {
            System.Numerics.Matrix4x4 translationSystemMatrix4x4;
            CreateTranslation(ref translation, out translationSystemMatrix4x4);
            return translationSystemMatrix4x4;
        }

        /// <summary>
        /// Creates a matrix representing the given axis aligned scale.
        /// </summary>
        /// <param name="scale">Scale to be represented by the matrix.</param>
        /// <param name="scaleSystem.Numerics.Matrix4x4">System.Numerics.Matrix4x4 representing the given scale.</param>
        public static void CreateScale(ref System.Numerics.Vector3 scale, out System.Numerics.Matrix4x4 scaleSystemMatrix4x4)
        {
            scaleSystemMatrix4x4 = new System.Numerics.Matrix4x4
                {
                    M11 = scale.X,
                    M22 = scale.Y,
                    M33 = scale.Z,
                    M44 = 1
                };
        }

        /// <summary>
        /// Creates a matrix representing the given axis aligned scale.
        /// </summary>
        /// <param name="scale">Scale to be represented by the matrix.</param>
        /// <returns>System.Numerics.Matrix4x4 representing the given scale.</returns>
        public static System.Numerics.Matrix4x4 CreateScale(System.Numerics.Vector3 scale)
        {
            System.Numerics.Matrix4x4 scaleSystemMatrix4x4;
            CreateScale(ref scale, out scaleSystemMatrix4x4);
            return scaleSystemMatrix4x4;
        }

        /// <summary>
        /// Creates a matrix representing the given axis aligned scale.
        /// </summary>
        /// <param name="x">Scale along the x axis.</param>
        /// <param name="y">Scale along the y axis.</param>
        /// <param name="z">Scale along the z axis.</param>
        /// <param name="scaleSystem.Numerics.Matrix4x4">System.Numerics.Matrix4x4 representing the given scale.</param>
        public static void CreateScale(float x, float y, float z, out System.Numerics.Matrix4x4 scaleSystemMatrix4x4)
        {
            scaleSystemMatrix4x4 = new System.Numerics.Matrix4x4
            {
                M11 = x,
                M22 = y,
                M33 = z,
                M44 = 1
            };
        }

        /// <summary>
        /// Creates a matrix representing the given axis aligned scale.
        /// </summary>
        /// <param name="x">Scale along the x axis.</param>
        /// <param name="y">Scale along the y axis.</param>
        /// <param name="z">Scale along the z axis.</param>
        /// <returns>System.Numerics.Matrix4x4 representing the given scale.</returns>
        public static System.Numerics.Matrix4x4 CreateScale(float x, float y, float z)
        {
            System.Numerics.Matrix4x4 scaleSystemMatrix4x4;
            CreateScale(x, y, z, out scaleSystemMatrix4x4);
            return scaleSystemMatrix4x4;
        }


    }
}
