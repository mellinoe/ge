using System;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using System.Numerics;

namespace Ge.Physics
{
    public class SphereCollider : Collider<Sphere>
    {
        private readonly Sphere _sphere;

        public SphereCollider(float radius)
        {
            _sphere = new Sphere(Vector3.Zero, radius);
        }

        public override Entity Entity => _sphere;
    }
}
