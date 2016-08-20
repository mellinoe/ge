using Engine.Assets;
using Engine.Graphics;
using Engine.Physics;
using System;
using System.Numerics;
using Veldrid.Graphics;

namespace Engine.Behaviors
{
    public class ObjectRain : Behavior
    {
        private float _elapsed = 0f;
        private readonly Random _random = new Random();

        private int _totalObjects = 2;
        private int _numBoxes = 2;

        public override void Update(float deltaSeconds)
        {
            _elapsed += deltaSeconds;
            const float dropInterval = 0.1f;
            if (_elapsed >= dropInterval)
            {
                _elapsed -= dropInterval;
                DropRandomObject();
            }
        }

        private void DropRandomObject()
        {
            _totalObjects++;
            _numBoxes++;

            var color = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { new RgbaFloat((float)_random.NextDouble(), (float)_random.NextDouble(), (float)_random.NextDouble(), 1.0f) },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);

            bool isBox = _random.NextDouble() <= 0.8;
            var newGo = new GameObject((isBox ? "Cube" : "Sphere") + (_totalObjects));
            newGo.Transform.Position = new Vector3((float)_random.NextDouble() * 29f - 14f, (float)_random.NextDouble() * 10f, (float)_random.NextDouble() * 29f - 14f);
            var mr = isBox
                ? new MeshRenderer(new SimpleMeshDataProvider(CubeModel.Vertices, CubeModel.Indices), color)
                : new MeshRenderer(EngineEmbeddedAssets.SphereModelID, color);
            mr.Wireframe = _random.NextDouble() > 0.9;
            newGo.AddComponent(mr);
            float radius = 0.3f + (float)_random.NextDouble() * .75f;
            if (!isBox)
            {
                newGo.Transform.Scale = new Vector3(radius);
            }
            Collider collider = isBox ? (Collider)new BoxCollider(1f, 1f, 1f) : new SphereCollider(1.0f);

            newGo.AddComponent(collider);
            newGo.AddComponent(new TimedDeath(30.0f));
            newGo.Destroyed += (go) => _numBoxes--;
            newGo.Transform.Rotation = Quaternion.CreateFromYawPitchRoll((float)_random.NextDouble() * 10f, 0f, 0f);
        }
    }
}
