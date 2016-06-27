using Ge.Graphics;
using Ge.Physics;
using Veldrid.Graphics;
using System.Numerics;

namespace Ge.Behaviors
{
    public class BallLauncher : Behavior
    {
        private InputSystem _input;
        private RawTextureDataArray<RgbaFloat> _color;
        private float _launchSpeed = 20.0f;

        protected override void Start(SystemRegistry registry)
        {
            _input = registry.GetSystem<InputSystem>();
            _color = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { RgbaFloat.Cyan },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);
        }

        public override void Update(float deltaSeconds)
        {
            if (_input.GetKey(OpenTK.Input.Key.F))
            {
                FireBoxForward();
            }
        }

        private void FireBoxForward()
        {
            var box = new GameObject("Box");
            var bc = new SphereCollider(1.0f, .01f);
            box.AddComponent(bc);
            box.AddComponent(new MeshRenderer(SphereModel.Vertices, SphereModel.Indices, _color));
            box.Transform.Position = Transform.Position + Transform.Forward * 1.0f;
            box.Transform.Scale = new Vector3(0.3f);
            bc.Entity.LinearVelocity = Transform.Forward * _launchSpeed;
        }
    }
}
