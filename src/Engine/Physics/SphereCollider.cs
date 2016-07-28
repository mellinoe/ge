using System;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using System.Numerics;

namespace Ge.Physics
{
    public class SphereCollider : Collider
    {
        private float _radius;
        private float _mass;

        public SphereCollider(float radius) : this(radius, (4f / 3f) * (float)Math.PI * radius * radius * radius)
        { }

        public SphereCollider(float radius, float mass)
        {
            _radius = radius;
            _mass = mass;
        }

        protected override Entity CreateEntity()
        {
            Vector3 scale = GameObject.Transform.Scale;
            return new Sphere(Vector3.Zero, scale.X * _radius, _mass) ;
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            Sphere sphere = (Sphere)Entity;
            sphere.Radius = _radius * scale.X;
        }
    }
}
