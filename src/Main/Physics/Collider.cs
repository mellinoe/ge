using System;
using System.Numerics;
using BEPUphysics;
using BEPUphysics.Entities;

namespace Ge.Physics
{
    public abstract class Collider<T> : Component where T : ISpaceObject
    {
        public abstract Entity Entity { get; }

        public sealed override void Attached(SystemRegistry registry)
        {
            registry.GetSystem<PhysicsSystem>().AddObject(Entity);
            Entity.Position = GameObject.Transform.Position;
            Entity.PositionUpdated += GameObject.Transform.OnPhysicsUpdated;
            Entity.Tag = GameObject;

            GameObject.Transform.RotationManuallyChanged += RotationManuallyChanged;
            GameObject.Transform.PositionManuallyChanged += PositionManuallyChanged;
        }

        private void PositionManuallyChanged(Vector3 position)
        {
            Entity.Position = position;
        }

        private void RotationManuallyChanged(Quaternion rotation)
        {
            Entity.Orientation = rotation;
        }

        public sealed override void Removed(SystemRegistry registry)
        {
            registry.GetSystem<PhysicsSystem>().RemoveObject(Entity);
            Entity.PositionUpdated -= GameObject.Transform.OnPhysicsUpdated;
        }
    }
}
