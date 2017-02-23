using Engine.Graphics;
using Engine.Physics;
using Veldrid.Graphics;
using System.Numerics;
using System;
using BEPUphysics.PositionUpdating;
using Veldrid.Platform;
using System.Threading.Tasks;

namespace Engine.Behaviors
{
    public class BallLauncher : Behavior
    {
        public float LaunchSpeed { get; set; } = 20f;
        public float Mass { get; set; } = 0.01f;
        public float Radius { get; set; } = 1f;
        public float Lifetime { get; set; } = 5f;
        public bool RapidFire { get; set; } = true;

        private InputSystem _input;
        private SynchronizationHelperSystem _shs;

        private Random _random = new Random();

        protected override void Start(SystemRegistry registry)
        {
            _input = registry.GetSystem<InputSystem>();
            _shs = registry.GetSystem<SynchronizationHelperSystem>();
        }

        public override void Update(float deltaSeconds)
        {
            if (_input.GetKeyDown(Key.F) || (RapidFire && _input.GetKey(Key.F)))
            {
                FireBoxForward();
            }
        }

        private void FireBoxForward()
        {
            Task.Run(() =>
            {
                var ball = new GameObject("Ball");
                var sc = new SphereCollider(1f, Mass);
                ball.AddComponent(sc);
                sc.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                var color = new RawTextureDataArray<RgbaFloat>(
                    new RgbaFloat[] { new RgbaFloat((float)_random.NextDouble(), (float)_random.NextDouble(), (float)_random.NextDouble(), 1.0f) },
                    1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);
                ball.AddComponent(new MeshRenderer(new SimpleMeshDataProvider(SphereModel.Vertices, SphereModel.Indices), color) { Wireframe = _random.NextDouble() > .99 });
                ball.Transform.Position = Transform.Position + Transform.Forward * Radius * 1.5f;
                ball.Transform.Scale = new Vector3(Radius);
                sc.Entity.LinearVelocity = Transform.Forward * LaunchSpeed;

                ball.AddComponent(new TimedDeath(Lifetime));
            });
        }
    }
}
