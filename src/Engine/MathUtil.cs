using System;

namespace Ge
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
    }
}
