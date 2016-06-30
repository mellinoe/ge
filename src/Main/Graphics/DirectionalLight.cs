using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Graphics;

namespace Ge.Graphics
{
    public class DirectionalLight : Component
    {
        private Vector3 _direction;
        private RgbaFloat _diffuseColor;
        private GraphicsSystem _gs;
        private DynamicDataProvider<LightInfo> _lightProvider = new DynamicDataProvider<LightInfo>();

        private DynamicDataProvider<Matrix4x4> _lightProjectionProvider = new DynamicDataProvider<Matrix4x4>();
        private DynamicDataProvider<Matrix4x4> _lightViewProvider = new DynamicDataProvider<Matrix4x4>();

        public RgbaFloat DiffuseColor { get { return _diffuseColor; } set { _diffuseColor = value; SetProvider(); } }
        public Vector3 Direction { get { return _direction; } set { _direction = Vector3.Normalize(value); SetProvider(); } }

        public DirectionalLight(RgbaFloat diffuseColor, Vector3 direction)
        {
            _diffuseColor = diffuseColor;
            _direction = direction;
            SetProvider();
        }

        public override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _gs.Context.DataProviders.Add("LightBuffer", _lightProvider);
            _gs.Context.DataProviders.Add("LightProjMatrix", _lightProjectionProvider);
            _gs.Context.DataProviders.Add("LightViewMatrix", _lightViewProvider);
        }

        public override void Removed(SystemRegistry registry)
        {
            _gs.Context.DataProviders.Remove("LightBuffer");
        }

        private void SetProvider()
        {
            _lightProvider.Data = new LightInfo(_diffuseColor, _direction);

            Vector3 lightPosition = -_direction * 20f;
            _lightViewProvider.Data = Matrix4x4.CreateLookAt(lightPosition, Vector3.Zero, Vector3.UnitY);
            _lightProjectionProvider.Data = Matrix4x4.CreateOrthographicOffCenter(-25, 25, -25, 25, 0, 50f);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightInfo
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
    }
}
