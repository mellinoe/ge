using System;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using System.Numerics;

namespace Ge.Physics
{
    public class SphereCollider : Collider
    {
        private readonly Sphere _sphere;

        public SphereCollider(float radius) : this(radius, 1.0f)
        { }

        public SphereCollider(float radius, float mass)
        {
            _sphere = new Sphere(Vector3.Zero, radius, mass);
        }

        public override Entity Entity => _sphere;
    }
}
