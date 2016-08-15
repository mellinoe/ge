using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class DirectionalLight : Component
    {
        private Vector3 _direction;
        private RgbaFloat _diffuseColor;
        private GraphicsSystem _gs;
        private DynamicDataProvider<LightInfo> _lightProvider = new DynamicDataProvider<LightInfo>();

        public RgbaFloat DiffuseColor { get { return _diffuseColor; } set { _diffuseColor = value; SetProvider(); } }
        public Vector3 Direction { get { return _direction; } set { _direction = Vector3.Normalize(value); SetProvider(); } }

        public DirectionalLight(RgbaFloat diffuseColor, Vector3 direction)
        {
            _diffuseColor = diffuseColor;
            _direction = direction;
        }

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _gs.Context.RegisterGlobalDataProvider("LightBuffer", _lightProvider);
        }

        protected override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            _gs.SetDirectionalLight(this);
            SetProvider();
        }

        protected override void OnDisabled()
        {
        }

        private void SetProvider()
        {
            _lightProvider.Data = new LightInfo(_diffuseColor, _direction);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightInfo : IEquatable<LightInfo>
    {
        public readonly RgbaFloat DiffuseColor;
        public readonly Vector3 Direction;
        private float __buffer;

        public LightInfo(RgbaFloat color, Vector3 direction)
        {
            DiffuseColor = color;
            Direction = direction;

            __buffer = 0;
        }

        public bool Equals(LightInfo other)
        {
            return DiffuseColor.Equals(other.DiffuseColor) && Direction.Equals(other.Direction);
        }
    }
}
