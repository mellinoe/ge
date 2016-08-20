using System;
using System.Numerics;

namespace Engine
{
    public static class MathUtil
    {
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
    }
}
