using System;
using System.Collections.Generic;
using System.Numerics;

namespace Engine
{
    public static class MathUtil
    {
        public static readonly float TwoPi = (float)(Math.PI * 2.0);

        public static float Clamp(float value, float min, float max)
        {
            if (value <= min)
            {
                return min;
            }
            else if (value >= max)
            {
                return max;
            }
            else
            {
                return value;
            }
        }

        public static float Lerp(float from, float to, float t)
        {
            return (from * (1 - t)) + (to * t);
        }

        public static bool ContainsNaN(Vector3 v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z);
        }

        public static float RadiansToDegrees(float radians)
        {
            return (float)(radians * 180 / Math.PI);
        }

        public static float DegreesToRadians(float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }

        public static Vector3 RadiansToDegrees(Vector3 radians)
        {
            return new Vector3(
                (float)(radians.X * 180 / Math.PI),
                (float)(radians.Y * 180 / Math.PI),
                (float)(radians.Z * 180 / Math.PI));
        }

        public static Vector3 DegreesToRadians(Vector3 degrees)
        {
            return new Vector3(
               (float)(degrees.X * Math.PI / 180),
               (float)(degrees.Y * Math.PI / 180),
               (float)(degrees.Z * Math.PI / 180));
        }

        public static Vector3 GetEulerAngles(this Quaternion q)
        {
            float attitude = (float)Math.Asin(2 * q.X * q.Y + 2 * q.Z * q.W);
            float heading;
            float bank;

            double edgeCase = q.X * q.Y + q.Z * q.W;
            if (edgeCase == 0.5)
            {
                heading = (float)(2 * Math.Atan2(q.X, q.W));
                bank = 0;
            }
            else if (edgeCase == -0.5)
            {
                heading = (float)(-2 * Math.Atan2(q.X, q.W));
                bank = 0;
            }
            else
            {
                heading = (float)Math.Atan2(2 * q.Y * q.W - 2 * q.X * q.Z, 1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z);
                bank = (float)Math.Atan2(2 * q.X * q.W - 2 * q.Y * q.Z, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z);
            }

            return new Vector3(bank, heading, attitude);
        }

        public static Vector3 SumAll(IEnumerable<Vector3> vectors)
        {
            Vector3 sum = Vector3.Zero;
            foreach (var v in vectors)
            {
                sum += v;
            }

            return sum;
        }

        // Code adapted from https://bitbucket.org/sinbad/ogre/src/9db75e3ba05c/OgreMain/include/OgreVector3.h

        public static Quaternion FromToRotation(Vector3 from, Vector3 to, Vector3 fallbackAxis = default(Vector3))
        {
            // Based on Stan Melax's article in Game Programming Gems
            Quaternion q;
            // Copy, since cannot modify local
            Vector3 v0 = from;
            Vector3 v1 = to;
            v0 = Vector3.Normalize(v0);
            v1 = Vector3.Normalize(v1);

            var d = Vector3.Dot(v0, v1);
            // If dot == 1, vectors are the same
            if (d >= 1.0f)
            {
                return Quaternion.Identity;
            }
            if (d < (1e-6f - 1.0f))
            {
                if (fallbackAxis != Vector3.Zero)
                {
                    // rotate 180 degrees about the fallback axis
                    q = Quaternion.CreateFromAxisAngle(fallbackAxis, (float)Math.PI);
                }
                else
                {
                    // Generate an axis
                    Vector3 axis = Vector3.Cross(Vector3.UnitX, from);
                    if (axis.LengthSquared() == 0) // pick another if colinear
                    {
                        axis = Vector3.Cross(Vector3.UnitY, from);
                    }

                    axis = Vector3.Normalize(axis);
                    q = Quaternion.CreateFromAxisAngle(axis, (float)Math.PI);
                }
            }
            else
            {
                float s = (float)Math.Sqrt((1 + d) * 2);
                float invs = 1.0f / s;

                Vector3 c = Vector3.Cross(v0, v1);

                q.X = c.X * invs;
                q.Y = c.Y * invs;
                q.Z = c.Z * invs;
                q.W = s * 0.5f;
                q = Quaternion.Normalize(q);
            }
            return q;
        }

        public static Vector3 Projection(Vector3 source, Vector3 direction)
        {
            return (Vector3.Dot(source, direction) / Vector3.Dot(direction, direction)) * direction;
        }
    }
}