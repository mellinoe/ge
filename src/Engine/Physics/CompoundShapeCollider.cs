using System;
using System.Numerics;
using BEPUphysics.Entities;
using System.Collections.Generic;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.CollisionShapes;

namespace Ge.Physics
{
    public class CompoundShapeCollider : Collider
    {
        private readonly IList<CompoundShapeEntry> _shapes;
        private readonly float _mass;

        public Vector3 EntityCenter { get; private set; }

        public CompoundShapeCollider(IList<CompoundShapeEntry> shapes, float mass)
        {
            _shapes = shapes;
            _mass = mass;
        }

        protected override Entity CreateEntity()
        {
            CompoundBody cb = new CompoundBody(_shapes, _mass);
            EntityCenter = -cb.Position;
            return cb;
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            throw new NotSupportedException();
        }
    }
}
