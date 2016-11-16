using Newtonsoft.Json;
using System;
using System.Numerics;

namespace Engine.Graphics
{
    public struct TintInfo : IEquatable<TintInfo>
    {
        public readonly Vector3 Color;
        public readonly float TintFactor;

        [JsonConstructor]
        public TintInfo(Vector3 color, float tintFactor)
        {
            Color = color;
            TintFactor = tintFactor;
        }

        public bool Equals(TintInfo other)
        {
            return Color == other.Color && TintFactor == other.TintFactor;
        }
    }
}
