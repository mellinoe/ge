using System;
using System.Numerics;

namespace Ge.Physics
{
    public class CharacterController : Component
    {
        private PhysicsSystem _physics;
        public BEPUphysics.Character.CharacterController Controller { get; private set; }

        public override void Attached(SystemRegistry registry)
        {
            _physics = registry.GetSystem<PhysicsSystem>();
            Controller = new BEPUphysics.Character.CharacterController(Transform.Position, jumpSpeed: 8f);
            _physics.AddObject(Controller);
            Transform.SetPhysicsEntity(Controller.Body);
        }

        public override void Removed(SystemRegistry registry)
        {
            _physics.RemoveObject(Controller);
            Transform.RemovePhysicsEntity();
        }

        public void SetMotionDirection(Vector2 motion)
        {
            Controller.HorizontalMotionConstraint.MovementDirection = motion;
        }
    }
}
