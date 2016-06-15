using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using System.Numerics;

namespace Ge.Physics
{
    public class BoxCollider : Collider<Box>
    {
        private readonly Box _box;
        public override Entity Entity => _box;

        public BoxCollider(float width, float height, float depth)
            : this(width, height, depth, 1.0f) { }

        public BoxCollider(float width, float height, float depth, float mass)
        {
            _box = new Box(Vector3.Zero, width, height, depth, mass);
        }
    }
}
