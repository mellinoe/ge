using System.Diagnostics;
using System.Numerics;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Ge.Physics
{
    public delegate void TriggerEvent(Collider other);

    public abstract class Collider : Component
    {
        private bool _isTrigger = false;
        private PhysicsSystem _physicsSystem;
        public Entity Entity { get; private set; }

        protected abstract Entity CreateEntity();

        public event TriggerEvent TriggerEntered;

        public event TriggerEvent TriggerExited;

        public bool IsTrigger
        {
            get { return _isTrigger; }
            set
            {
                if (_isTrigger && !value)
                {
                    Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.NoSolver;
                    // TODO: Only subscribe to this if there are listeners; otherwise defer.
                    SubscribeToEvents();
                }
                else if (!_isTrigger && value)
                {
                    Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.Normal;
                    // TODO: Only unsubscribe from this if there are listeners.
                    UnsubscribeFromEvents();
                }

                _isTrigger = value;
            }
        }

        private void UnsubscribeFromEvents()
        {
            Entity.CollisionInformation.Events.PairCreated -= OnCollisionPairCreated;
            Entity.CollisionInformation.Events.PairRemoved -= OnCollisionPairRemoved;
        }

        private void SubscribeToEvents()
        {
            Entity.CollisionInformation.Events.PairCreated += OnCollisionPairCreated;
            Entity.CollisionInformation.Events.PairRemoved += OnCollisionPairRemoved;
        }

        public sealed override void Attached(SystemRegistry registry)
        {
            _physicsSystem = registry.GetSystem<PhysicsSystem>();

            Entity = CreateEntity();
            AddAndInitializeEntity();

            GameObject.Transform.RotationManuallyChanged += RotationManuallyChanged;
            GameObject.Transform.PositionManuallyChanged += PositionManuallyChanged;
            GameObject.Transform.ScaleChanged += ScaleChanged;
        }

        private void AddAndInitializeEntity()
        {
            _physicsSystem.AddObject(Entity);
            Entity.Position = GameObject.Transform.Position;
            Entity.PositionUpdated += GameObject.Transform.OnPhysicsUpdated;
            Entity.Tag = this;
            Entity.CollisionInformation.Tag = this;
            Entity = Entity;
        }

        public sealed override void Removed(SystemRegistry registry)
        {
            _physicsSystem.RemoveObject(Entity);
            Entity.PositionUpdated -= GameObject.Transform.OnPhysicsUpdated;
        }

        protected abstract void ScaleChanged(Vector3 scale);

        private void PositionManuallyChanged(Vector3 position)
        {
            Entity.Position = position;
        }

        private void RotationManuallyChanged(Quaternion rotation)
        {
            Entity.Orientation = rotation;
        }

        private void OnCollisionPairCreated(EntityCollidable sender, BroadPhaseEntry other, NarrowPhasePair pair)
        {
            Debug.Assert(other.Tag is Collider);
            Collider otherCollider = (Collider)other.Tag;
            TriggerEntered?.Invoke(otherCollider);
        }

        private void OnCollisionPairRemoved(EntityCollidable sender, BroadPhaseEntry other)
        {
            Debug.Assert(other.Tag is Collider);
            Collider otherCollider = (Collider)other.Tag;
            TriggerExited?.Invoke(otherCollider);
        }
    }
}
