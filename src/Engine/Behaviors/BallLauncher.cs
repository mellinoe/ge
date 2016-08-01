using Engine.Graphics;
using Engine.Physics;
using Veldrid.Graphics;
using System.Numerics;
using System;
using BEPUphysics.PositionUpdating;
using Veldrid.Platform;

namespace Engine.Behaviors
{
    public class BallLauncher : Behavior
    {
        private InputSystem _input;
        private float _launchSpeed = 20.0f;

        private Random _random = new Random();

        internal override void Start(SystemRegistry registry)
        {
            _input = registry.GetSystem<InputSystem>();
        }

        public override void Update(float deltaSeconds)
        {
            if (_input.GetKey(Key.F))
            {
                FireBoxForward();
            }
        }

        private void FireBoxForward()
        {
            var ball = new GameObject("Ball");
            var sc = new SphereCollider(1.0f, .01f);
            ball.AddComponent(sc);
            sc.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
            var color = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { new RgbaFloat((float)_random.NextDouble(), (float)_random.NextDouble(), (float)_random.NextDouble(), 1.0f) },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);
            ball.AddComponent(new MeshRenderer(SphereModel.Vertices, SphereModel.Indices, color) { Wireframe = _random.NextDouble() > .99 });
            ball.Transform.Position = Transform.Position + Transform.Forward * 1.0f;
            ball.Transform.Scale = new Vector3(0.1f);
            sc.Entity.LinearVelocity = Transform.Forward * _launchSpeed;

            ball.AddComponent(new TimedDeath(5.0f));
        }
    }
}
