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
        private static readonly DynamicDataProvider<LightInfo> _noLightProvider
            = new DynamicDataProvider<LightInfo>(new LightInfo(RgbaFloat.Black, Vector3.Zero));

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
        }

        protected override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            _gs.SetDirectionalLight(this);
            _gs.Context.RegisterGlobalDataProvider("LightBuffer", _lightProvider);
            SetProvider();
        }

        protected override void OnDisabled()
        {
            _gs.Context.RegisterGlobalDataProvider("LightBuffer", _noLightProvider);
            _gs.SetDirectionalLight(null);
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
