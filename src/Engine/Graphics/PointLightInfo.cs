using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PointLightInfo : IEquatable<PointLightInfo>
    {
        public Vector3 Position;
        public float Range;
        public Vector3 Color;
        public float Intensity;

        public PointLightInfo(Vector3 position, Vector3 color, float range, float intensity)
        {
            Position = position;
            Color = color;
            Range = range;
            Intensity = intensity;
        }

        public bool Equals(PointLightInfo other)
        {
            return other.Position == Position && other.Range == Range && other.Color == Color && other.Intensity == Intensity;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointLightsBuffer : IEquatable<PointLightsBuffer>
    {
        public const int MaxLights = 4;

        public int NumActivePointLights;
        private Vector3 __padding;

        public PointLightInfo LightInfo0;
        public PointLightInfo LightInfo1;
        public PointLightInfo LightInfo2;
        public PointLightInfo LightInfo3;

        public bool Equals(PointLightsBuffer other)
        {
            return other.NumActivePointLights == NumActivePointLights
                && other.LightInfo0.Equals(LightInfo0)
                && other.LightInfo1.Equals(LightInfo1)
                && other.LightInfo2.Equals(LightInfo2)
                && other.LightInfo3.Equals(LightInfo3);
        }

        public PointLightInfo this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return LightInfo0;
                    case 1: return LightInfo1;
                    case 2: return LightInfo2;
                    case 3: return LightInfo3;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0: LightInfo0 = value; break;
                    case 1: LightInfo1 = value; break;
                    case 2: LightInfo2 = value; break;
                    case 3: LightInfo3 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
