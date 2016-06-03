using System;

namespace BEPUutilities
{
    /// <summary>
    /// Provides XNA-like quaternion support.
    /// </summary>
    public static class QuaternionEx
    {
        public static void Add(ref System.Numerics.Quaternion a, ref System.Numerics.Quaternion b, out System.Numerics.Quaternion result)
        {
            result.X = a.X + b.X;
            result.Y = a.Y + b.Y;
            result.Z = a.Z + b.Z;
            result.W = a.W + b.W;
        }

        /// <summary>
        /// Multiplies two quaternions.
        /// </summary>
        /// <param name="a">First quaternion to multiply.</param>
        /// <param name="b">Second quaternion to multiply.</param>
        /// <param name="result">Product of the multiplication.</param>
        public static void Multiply(ref System.Numerics.Quaternion a, ref System.Numerics.Quaternion b, out System.Numerics.Quaternion result)
        {
            float x = a.X;
            float y = a.Y;
            float z = a.Z;
            float w = a.W;
            float bX = b.X;
            float bY = b.Y;
            float bZ = b.Z;
            float bW = b.W;
            result.X = x * bW + bX * w + y * bZ - z * bY;
            result.Y = y * bW + bY * w + z * bX - x * bZ;
            result.Z = z * bW + bZ * w + x * bY - y * bX;
            result.W = w * bW - x * bX - y * bY - z * bZ;
        }

        /// <summary>
        /// Scales a quaternion.
        /// </summary>
        /// <param name="q">System.Numerics.Quaternion to multiply.</param>
        /// <param name="scale">Amount to multiply each component of the quaternion by.</param>
        /// <param name="result">Scaled quaternion.</param>
        public static void Multiply(ref System.Numerics.Quaternion q, float scale, out System.Numerics.Quaternion result)
        {
            result.X = q.X * scale;
            result.Y = q.Y * scale;
            result.Z = q.Z * scale;
            result.W = q.W * scale;
        }

        /// <summary>
        /// Multiplies two quaternions together in opposite order.
        /// </summary>
        /// <param name="a">First quaternion to multiply.</param>
        /// <param name="b">Second quaternion to multiply.</param>
        /// <param name="result">Product of the multiplication.</param>
        public static void Concatenate(ref System.Numerics.Quaternion a, ref System.Numerics.Quaternion b, out System.Numerics.Quaternion result)
        {
            float aX = a.X;
            float aY = a.Y;
            float aZ = a.Z;
            float aW = a.W;
            float bX = b.X;
            float bY = b.Y;
            float bZ = b.Z;
            float bW = b.W;

            result.X = aW * bX + aX * bW + aZ * bY - aY * bZ;
            result.Y = aW * bY + aY * bW + aX * bZ - aZ * bX;
            result.Z = aW * bZ + aZ * bW + aY * bX - aX * bY;
            result.W = aW * bW - aX * bX - aY * bY - aZ * bZ;


        }

        /// <summary>
        /// Multiplies two quaternions together in opposite order.
        /// </summary>
        /// <param name="a">First quaternion to multiply.</param>
        /// <param name="b">Second quaternion to multiply.</param>
        /// <returns>Product of the multiplication.</returns>
        public static System.Numerics.Quaternion Concatenate(System.Numerics.Quaternion a, System.Numerics.Quaternion b)
        {
            System.Numerics.Quaternion result;
            Concatenate(ref a, ref b, out result);
            return result;
        }

        /// <summary>
        /// System.Numerics.Quaternion representing the identity transform.
        /// </summary>
        public static System.Numerics.Quaternion Identity
        {
            get
            {
                return new System.Numerics.Quaternion(0, 0, 0, 1);
            }
        }




        /// <summary>
        /// Constructs a quaternion from a rotation matrix.
        /// </summary>
        /// <param name="r">Rotation matrix to create the quaternion from.</param>
        /// <param name="q">System.Numerics.Quaternion based on the rotation matrix.</param>
        public static void CreateFromRotationMatrix(ref Matrix3x3 r, out System.Numerics.Quaternion q)
        {
            float trace = r.M11 + r.M22 + r.M33;
#if !WINDOWS
            q = new System.Numerics.Quaternion();
#endif
            if (trace >= 0)
            {
                var S = (float)Math.Sqrt(trace + 1.0) * 2; // S=4*qw 
                var inverseS = 1 / S;
                q.W = 0.25f * S;
                q.X = (r.M23 - r.M32) * inverseS;
                q.Y = (r.M31 - r.M13) * inverseS;
                q.Z = (r.M12 - r.M21) * inverseS;
            }
            else if ((r.M11 > r.M22) & (r.M11 > r.M33))
            {
                var S = (float)Math.Sqrt(1.0 + r.M11 - r.M22 - r.M33) * 2; // S=4*qx 
                var inverseS = 1 / S;
                q.W = (r.M23 - r.M32) * inverseS;
                q.X = 0.25f * S;
                q.Y = (r.M21 + r.M12) * inverseS;
                q.Z = (r.M31 + r.M13) * inverseS;
            }
            else if (r.M22 > r.M33)
            {
                var S = (float)Math.Sqrt(1.0 + r.M22 - r.M11 - r.M33) * 2; // S=4*qy
                var inverseS = 1 / S;
                q.W = (r.M31 - r.M13) * inverseS;
                q.X = (r.M21 + r.M12) * inverseS;
                q.Y = 0.25f * S;
                q.Z = (r.M32 + r.M23) * inverseS;
            }
            else
            {
                var S = (float)Math.Sqrt(1.0 + r.M33 - r.M11 - r.M22) * 2; // S=4*qz
                var inverseS = 1 / S;
                q.W = (r.M12 - r.M21) * inverseS;
                q.X = (r.M31 + r.M13) * inverseS;
                q.Y = (r.M32 + r.M23) * inverseS;
                q.Z = 0.25f * S;
            }
        }

        /// <summary>
        /// Creates a quaternion from a rotation matrix.
        /// </summary>
        /// <param name="r">Rotation matrix used to create a new quaternion.</param>
        /// <returns>System.Numerics.Quaternion representing the same rotation as the matrix.</returns>
        public static System.Numerics.Quaternion CreateFromRotationMatrix(Matrix3x3 r)
        {
            System.Numerics.Quaternion toReturn;
            CreateFromRotationMatrix(ref r, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Constructs a quaternion from a rotation matrix.
        /// </summary>
        /// <param name="r">Rotation matrix to create the quaternion from.</param>
        /// <param name="q">System.Numerics.Quaternion based on the rotation matrix.</param>
        public static void CreateFromRotationMatrix(ref System.Numerics.Matrix4x4 r, out System.Numerics.Quaternion q)
        {
            Matrix3x3 downsizedMatrix;
            Matrix3x3.CreateFromMatrix(ref r, out downsizedMatrix);
            CreateFromRotationMatrix(ref downsizedMatrix, out q);
        }

        /// <summary>
        /// Creates a quaternion from a rotation matrix.
        /// </summary>
        /// <param name="r">Rotation matrix used to create a new quaternion.</param>
        /// <returns>System.Numerics.Quaternion representing the same rotation as the matrix.</returns>
        public static System.Numerics.Quaternion CreateFromRotationMatrix(System.Numerics.Matrix4x4 r)
        {
            System.Numerics.Quaternion toReturn;
            CreateFromRotationMatrix(ref r, out toReturn);
            return toReturn;
        }


        /// <summary>
        /// Ensures the quaternion has unit length.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to normalize.</param>
        /// <returns>Normalized quaternion.</returns>
        public static System.Numerics.Quaternion Normalize(System.Numerics.Quaternion quaternion)
        {
            System.Numerics.Quaternion toReturn;
            Normalize(ref quaternion, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Ensures the quaternion has unit length.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to normalize.</param>
        /// <param name="toReturn">Normalized quaternion.</param>
        public static void Normalize(ref System.Numerics.Quaternion quaternion, out System.Numerics.Quaternion toReturn)
        {
            float inverse = (float)(1 / Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W));
            toReturn.X = quaternion.X * inverse;
            toReturn.Y = quaternion.Y * inverse;
            toReturn.Z = quaternion.Z * inverse;
            toReturn.W = quaternion.W * inverse;
        }

        /// <summary>
        /// Blends two quaternions together to get an intermediate state.
        /// </summary>
        /// <param name="start">Starting point of the interpolation.</param>
        /// <param name="end">Ending point of the interpolation.</param>
        /// <param name="interpolationAmount">Amount of the end point to use.</param>
        /// <param name="result">Interpolated intermediate quaternion.</param>
        public static void Slerp(ref System.Numerics.Quaternion start, ref System.Numerics.Quaternion end, float interpolationAmount, out System.Numerics.Quaternion result)
        {
            double cosHalfTheta = start.W * end.W + start.X * end.X + start.Y * end.Y + start.Z * end.Z;
            if (cosHalfTheta < 0)
            {
                //Negating a quaternion results in the same orientation, 
                //but we need cosHalfTheta to be positive to get the shortest path.
                end.X = -end.X;
                end.Y = -end.Y;
                end.Z = -end.Z;
                end.W = -end.W;
                cosHalfTheta = -cosHalfTheta;
            }
            // If the orientations are similar enough, then just pick one of the inputs.
            if (cosHalfTheta > .999999)
            {
                result.W = start.W;
                result.X = start.X;
                result.Y = start.Y;
                result.Z = start.Z;
                return;
            }
            // Calculate temporary values.
            double halfTheta = Math.Acos(cosHalfTheta);
            double sinHalfTheta = Math.Sqrt(1.0 - cosHalfTheta * cosHalfTheta);

            double aFraction = Math.Sin((1 - interpolationAmount) * halfTheta) / sinHalfTheta;
            double bFraction = Math.Sin(interpolationAmount * halfTheta) / sinHalfTheta;

            //Blend the two quaternions to get the result!
            result.X = (float)(start.X * aFraction + end.X * bFraction);
            result.Y = (float)(start.Y * aFraction + end.Y * bFraction);
            result.Z = (float)(start.Z * aFraction + end.Z * bFraction);
            result.W = (float)(start.W * aFraction + end.W * bFraction);




        }

        /// <summary>
        /// Blends two quaternions together to get an intermediate state.
        /// </summary>
        /// <param name="start">Starting point of the interpolation.</param>
        /// <param name="end">Ending point of the interpolation.</param>
        /// <param name="interpolationAmount">Amount of the end point to use.</param>
        /// <returns>Interpolated intermediate quaternion.</returns>
        public static System.Numerics.Quaternion Slerp(System.Numerics.Quaternion start, System.Numerics.Quaternion end, float interpolationAmount)
        {
            System.Numerics.Quaternion toReturn;
            Slerp(ref start, ref end, interpolationAmount, out toReturn);
            return toReturn;
        }


        /// <summary>
        /// Computes the conjugate of the quaternion.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to conjugate.</param>
        /// <param name="result">Conjugated quaternion.</param>
        public static void Conjugate(ref System.Numerics.Quaternion quaternion, out System.Numerics.Quaternion result)
        {
            result.X = -quaternion.X;
            result.Y = -quaternion.Y;
            result.Z = -quaternion.Z;
            result.W = quaternion.W;
        }

        /// <summary>
        /// Computes the conjugate of the quaternion.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to conjugate.</param>
        /// <returns>Conjugated quaternion.</returns>
        public static System.Numerics.Quaternion Conjugate(System.Numerics.Quaternion quaternion)
        {
            System.Numerics.Quaternion toReturn;
            Conjugate(ref quaternion, out toReturn);
            return toReturn;
        }



        /// <summary>
        /// Computes the inverse of the quaternion.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to invert.</param>
        /// <param name="result">Result of the inversion.</param>
        public static void Inverse(ref System.Numerics.Quaternion quaternion, out System.Numerics.Quaternion result)
        {
            float inverseSquaredNorm = quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W;
            result.X = -quaternion.X * inverseSquaredNorm;
            result.Y = -quaternion.Y * inverseSquaredNorm;
            result.Z = -quaternion.Z * inverseSquaredNorm;
            result.W = quaternion.W * inverseSquaredNorm;
        }

        /// <summary>
        /// Computes the inverse of the quaternion.
        /// </summary>
        /// <param name="quaternion">System.Numerics.Quaternion to invert.</param>
        /// <returns>Result of the inversion.</returns>
        public static System.Numerics.Quaternion Inverse(System.Numerics.Quaternion quaternion)
        {
            System.Numerics.Quaternion result;
            Inverse(ref quaternion, out result);
            return result;

        }

        /// <summary>
        /// Transforms the vector using a quaternion.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void Transform(ref System.Numerics.Vector3 v, ref System.Numerics.Quaternion rotation, out System.Numerics.Vector3 result)
        {
            //This operation is an optimized-down version of v' = q * v * q^-1.
            //The expanded form would be to treat v as an 'axis only' quaternion
            //and perform standard quaternion multiplication.  Assuming q is normalized,
            //q^-1 can be replaced by a conjugation.
            float x2 = rotation.X + rotation.X;
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;
            float xx2 = rotation.X * x2;
            float xy2 = rotation.X * y2;
            float xz2 = rotation.X * z2;
            float yy2 = rotation.Y * y2;
            float yz2 = rotation.Y * z2;
            float zz2 = rotation.Z * z2;
            float wx2 = rotation.W * x2;
            float wy2 = rotation.W * y2;
            float wz2 = rotation.W * z2;
            //Defer the component setting since they're used in computation.
            float transformedX = v.X * (1f - yy2 - zz2) + v.Y * (xy2 - wz2) + v.Z * (xz2 + wy2);
            float transformedY = v.X * (xy2 + wz2) + v.Y * (1f - xx2 - zz2) + v.Z * (yz2 - wx2);
            float transformedZ = v.X * (xz2 - wy2) + v.Y * (yz2 + wx2) + v.Z * (1f - xx2 - yy2);
            result.X = transformedX;
            result.Y = transformedY;
            result.Z = transformedZ;

        }

        /// <summary>
        /// Transforms the vector using a quaternion.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public static System.Numerics.Vector3 Transform(System.Numerics.Vector3 v, System.Numerics.Quaternion rotation)
        {
            System.Numerics.Vector3 toReturn;
            Transform(ref v, ref rotation, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Transforms a vector using a quaternion. Specialized for x,0,0 vectors.
        /// </summary>
        /// <param name="x">X component of the vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformX(float x, ref System.Numerics.Quaternion rotation, out System.Numerics.Vector3 result)
        {
            //This operation is an optimized-down version of v' = q * v * q^-1.
            //The expanded form would be to treat v as an 'axis only' quaternion
            //and perform standard quaternion multiplication.  Assuming q is normalized,
            //q^-1 can be replaced by a conjugation.
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;
            float xy2 = rotation.X * y2;
            float xz2 = rotation.X * z2;
            float yy2 = rotation.Y * y2;
            float zz2 = rotation.Z * z2;
            float wy2 = rotation.W * y2;
            float wz2 = rotation.W * z2;
            //Defer the component setting since they're used in computation.
            float transformedX = x * (1f - yy2 - zz2);
            float transformedY = x * (xy2 + wz2);
            float transformedZ = x * (xz2 - wy2);
            result.X = transformedX;
            result.Y = transformedY;
            result.Z = transformedZ;

        }

        /// <summary>
        /// Transforms a vector using a quaternion. Specialized for 0,y,0 vectors.
        /// </summary>
        /// <param name="y">Y component of the vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformY(float y, ref System.Numerics.Quaternion rotation, out System.Numerics.Vector3 result)
        {
            //This operation is an optimized-down version of v' = q * v * q^-1.
            //The expanded form would be to treat v as an 'axis only' quaternion
            //and perform standard quaternion multiplication.  Assuming q is normalized,
            //q^-1 can be replaced by a conjugation.
            float x2 = rotation.X + rotation.X;
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;
            float xx2 = rotation.X * x2;
            float xy2 = rotation.X * y2;
            float yz2 = rotation.Y * z2;
            float zz2 = rotation.Z * z2;
            float wx2 = rotation.W * x2;
            float wz2 = rotation.W * z2;
            //Defer the component setting since they're used in computation.
            float transformedX = y * (xy2 - wz2);
            float transformedY = y * (1f - xx2 - zz2);
            float transformedZ = y * (yz2 + wx2);
            result.X = transformedX;
            result.Y = transformedY;
            result.Z = transformedZ;

        }

        /// <summary>
        /// Transforms a vector using a quaternion. Specialized for 0,0,z vectors.
        /// </summary>
        /// <param name="z">Z component of the vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void TransformZ(float z, ref System.Numerics.Quaternion rotation, out System.Numerics.Vector3 result)
        {
            //This operation is an optimized-down version of v' = q * v * q^-1.
            //The expanded form would be to treat v as an 'axis only' quaternion
            //and perform standard quaternion multiplication.  Assuming q is normalized,
            //q^-1 can be replaced by a conjugation.
            float x2 = rotation.X + rotation.X;
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;
            float xx2 = rotation.X * x2;
            float xz2 = rotation.X * z2;
            float yy2 = rotation.Y * y2;
            float yz2 = rotation.Y * z2;
            float wx2 = rotation.W * x2;
            float wy2 = rotation.W * y2;
            //Defer the component setting since they're used in computation.
            float transformedX = z * (xz2 + wy2);
            float transformedY = z * (yz2 - wx2);
            float transformedZ = z * (1f - xx2 - yy2);
            result.X = transformedX;
            result.Y = transformedY;
            result.Z = transformedZ;

        }

        /// <summary>
        /// Creates a quaternion from an axis and angle.
        /// </summary>
        /// <param name="axis">Axis of rotation.</param>
        /// <param name="angle">Angle to rotate around the axis.</param>
        /// <returns>System.Numerics.Quaternion representing the axis and angle rotation.</returns>
        public static System.Numerics.Quaternion CreateFromAxisAngle(System.Numerics.Vector3 axis, float angle)
        {
            float halfAngle = angle * .5f;
            float s = (float)Math.Sin(halfAngle);
            System.Numerics.Quaternion q;
            q.X = axis.X * s;
            q.Y = axis.Y * s;
            q.Z = axis.Z * s;
            q.W = (float)Math.Cos(halfAngle);
            return q;
        }

        /// <summary>
        /// Creates a quaternion from an axis and angle.
        /// </summary>
        /// <param name="axis">Axis of rotation.</param>
        /// <param name="angle">Angle to rotate around the axis.</param>
        /// <param name="q">System.Numerics.Quaternion representing the axis and angle rotation.</param>
        public static void CreateFromAxisAngle(ref System.Numerics.Vector3 axis, float angle, out System.Numerics.Quaternion q)
        {
            float halfAngle = angle * .5f;
            float s = (float)Math.Sin(halfAngle);
            q.X = axis.X * s;
            q.Y = axis.Y * s;
            q.Z = axis.Z * s;
            q.W = (float)Math.Cos(halfAngle);
        }

        /// <summary>
        /// Constructs a quaternion from yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Yaw of the rotation.</param>
        /// <param name="pitch">Pitch of the rotation.</param>
        /// <param name="roll">Roll of the rotation.</param>
        /// <returns>System.Numerics.Quaternion representing the yaw, pitch, and roll.</returns>
        public static System.Numerics.Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            System.Numerics.Quaternion toReturn;
            CreateFromYawPitchRoll(yaw, pitch, roll, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Constructs a quaternion from yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Yaw of the rotation.</param>
        /// <param name="pitch">Pitch of the rotation.</param>
        /// <param name="roll">Roll of the rotation.</param>
        /// <param name="q">System.Numerics.Quaternion representing the yaw, pitch, and roll.</param>
        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out System.Numerics.Quaternion q)
        {
            double halfRoll = roll * 0.5;
            double halfPitch = pitch * 0.5;
            double halfYaw = yaw * 0.5;

            double sinRoll = Math.Sin(halfRoll);
            double sinPitch = Math.Sin(halfPitch);
            double sinYaw = Math.Sin(halfYaw);

            double cosRoll = Math.Cos(halfRoll);
            double cosPitch = Math.Cos(halfPitch);
            double cosYaw = Math.Cos(halfYaw);

            double cosYawCosPitch = cosYaw * cosPitch;
            double cosYawSinPitch = cosYaw * sinPitch;
            double sinYawCosPitch = sinYaw * cosPitch;
            double sinYawSinPitch = sinYaw * sinPitch;

            q.X = (float)(cosYawSinPitch * cosRoll + sinYawCosPitch * sinRoll);
            q.Y = (float)(sinYawCosPitch * cosRoll - cosYawSinPitch * sinRoll);
            q.Z = (float)(cosYawCosPitch * sinRoll - sinYawSinPitch * cosRoll);
            q.W = (float)(cosYawCosPitch * cosRoll + sinYawSinPitch * sinRoll);

        }

        /// <summary>
        /// Computes the angle change represented by a normalized quaternion.
        /// </summary>
        /// <param name="q">System.Numerics.Quaternion to be converted.</param>
        /// <returns>Angle around the axis represented by the quaternion.</returns>
        public static float GetAngleFromQuaternion(ref System.Numerics.Quaternion q)
        {
            float qw = Math.Abs(q.W);
            if (qw > 1)
                return 0;
            return 2 * (float)Math.Acos(qw);
        }

        /// <summary>
        /// Computes the axis angle representation of a normalized quaternion.
        /// </summary>
        /// <param name="q">System.Numerics.Quaternion to be converted.</param>
        /// <param name="axis">Axis represented by the quaternion.</param>
        /// <param name="angle">Angle around the axis represented by the quaternion.</param>
        public static void GetAxisAngleFromQuaternion(ref System.Numerics.Quaternion q, out System.Numerics.Vector3 axis, out float angle)
        {
#if !WINDOWS
            axis = new System.Numerics.Vector3();
#endif
            float qx = q.X;
            float qy = q.Y;
            float qz = q.Z;
            float qw = q.W;
            if (qw < 0)
            {
                qx = -qx;
                qy = -qy;
                qz = -qz;
                qw = -qw;
            }
            if (qw > 1 - 1e-6)
            {
                axis = Toolbox.UpVector;
                angle = 0;
            }
            else
            {
                angle = 2 * (float)Math.Acos(qw);
                float denominator = 1 / (float)Math.Sqrt(1 - qw * qw);
                axis.X = qx * denominator;
                axis.Y = qy * denominator;
                axis.Z = qz * denominator;
            }
        }

        /// <summary>
        /// Computes the quaternion rotation between two normalized vectors.
        /// </summary>
        /// <param name="v1">First unit-length vector.</param>
        /// <param name="v2">Second unit-length vector.</param>
        /// <param name="q">System.Numerics.Quaternion representing the rotation from v1 to v2.</param>
        public static void GetQuaternionBetweenNormalizedVectors(ref System.Numerics.Vector3 v1, ref System.Numerics.Vector3 v2, out System.Numerics.Quaternion q)
        {
            float dot;
            Vector3Ex.Dot(ref v1, ref v2, out dot);
            //For non-normal vectors, the multiplying the axes length squared would be necessary:
            //float w = dot + (float)Math.Sqrt(v1.LengthSquared() * v2.LengthSquared());
            if (dot < -0.9999f) //parallel, opposing direction
            {
                //If this occurs, the rotation required is ~180 degrees.
                //The problem is that we could choose any perpendicular axis for the rotation. It's not uniquely defined.
                //The solution is to pick an arbitrary perpendicular axis.
                //Project onto the plane which has the lowest component magnitude.
                //On that 2d plane, perform a 90 degree rotation.
                float absX = Math.Abs(v1.X);
                float absY = Math.Abs(v1.Y);
                float absZ = Math.Abs(v1.Z);
                if (absX < absY && absX < absZ)
                    q = new System.Numerics.Quaternion(0, -v1.Z, v1.Y, 0);
                else if (absY < absZ)
                    q = new System.Numerics.Quaternion(-v1.Z, 0, v1.X, 0);
                else
                    q = new System.Numerics.Quaternion(-v1.Y, v1.X, 0, 0);
            }
            else
            {
                System.Numerics.Vector3 axis;
                Vector3Ex.Cross(ref v1, ref v2, out axis);
                q = new System.Numerics.Quaternion(axis.X, axis.Y, axis.Z, dot + 1);
            }
            q = QuaternionEx.Normalize(q);
        }

        //The following two functions are highly similar, but it's a bit of a brain teaser to phrase one in terms of the other.
        //Providing both simplifies things.

        /// <summary>
        /// Computes the rotation from the start orientation to the end orientation such that end = QuaternionEx.Concatenate(start, relative).
        /// </summary>
        /// <param name="start">Starting orientation.</param>
        /// <param name="end">Ending orientation.</param>
        /// <param name="relative">Relative rotation from the start to the end orientation.</param>
        public static void GetRelativeRotation(ref System.Numerics.Quaternion start, ref System.Numerics.Quaternion end, out System.Numerics.Quaternion relative)
        {
            System.Numerics.Quaternion startInverse;
            Conjugate(ref start, out startInverse);
            Concatenate(ref startInverse, ref end, out relative);
        }

        /// <summary>
        /// Transforms the rotation into the local space of the target basis such that rotation = QuaternionEx.Concatenate(localRotation, targetBasis)
        /// </summary>
        /// <param name="rotation">Rotation in the original frame of reference.</param>
        /// <param name="targetBasis">Basis in the original frame of reference to transform the rotation into.</param>
        /// <param name="localRotation">Rotation in the local space of the target basis.</param>
        public static void GetLocalRotation(ref System.Numerics.Quaternion rotation, ref System.Numerics.Quaternion targetBasis, out System.Numerics.Quaternion localRotation)
        {
            System.Numerics.Quaternion basisInverse;
            Conjugate(ref targetBasis, out basisInverse);
            Concatenate(ref rotation, ref basisInverse, out localRotation);
        }
    }
}
