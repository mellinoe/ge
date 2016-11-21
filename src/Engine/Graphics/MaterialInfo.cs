using Newtonsoft.Json;
using System;
using System.Numerics;

namespace Engine.Graphics
{
    public struct MaterialInfo : IEquatable<MaterialInfo>
    {
        public readonly float Opacity;
        private readonly Vector3 __padding;

        [JsonConstructor]
        public MaterialInfo(float opacity)
        {
            Opacity = opacity;
            __padding = Vector3.Zero;
        }

        public bool Equals(MaterialInfo other)
        {
            return Opacity == other.Opacity;
        }
    }
}
