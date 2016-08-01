using System.Numerics;

namespace Engine.Graphics
{
    public struct TintInfo
    {
        public readonly Vector3 Color;
        public readonly float TintFactor;

        public TintInfo(Vector3 color, float tintFactor)
        {
            Color = color;
            TintFactor = tintFactor;
        }
    }
}
