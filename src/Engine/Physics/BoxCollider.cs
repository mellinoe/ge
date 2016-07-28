using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using System.Numerics;
using System;

namespace Ge.Physics
{
    public class BoxCollider : Collider
    {
        private float _width;
        private float _height;
        private float _depth;
        private float _mass;

        public BoxCollider(float width, float height, float depth)
            : this(width, height, depth, 1.0f * (width * height * depth)) { }

        public BoxCollider(float width, float height, float depth, float mass)
        {
            _width = width;
            _height = height;
            _depth = depth;
            _mass = mass;
        }

        protected override Entity CreateEntity()
        {
            Vector3 scale = GameObject.Transform.Scale;
            return new Box(Vector3.Zero, _width * scale.X, _height * scale.Y, _depth * scale.Z, _mass);
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            Box box = (Box)Entity;
            box.Width = _width * scale.X;
            box.Height = _height * scale.Y;
            box.Length = _depth * scale.Z;
        }
    }
}
