using System.Diagnostics;
using System.Numerics;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.Constraints.SolverGroups;
using System;
using BEPUphysics.PositionUpdating;
using Newtonsoft.Json;

namespace Engine.Physics
{
    public delegate void TriggerEvent(Collider other);

    public abstract class Collider : Component
    {
        private bool _isTrigger = false;
        private PhysicsSystem _physicsSystem;
        private WeldJoint _parentJoint;
        private Collider _parentCollider;

        [JsonProperty]
        private float _mass;
        public float Mass
        {
            get { return _mass; }
            set
            {
                _mass = value;
                if (Entity != null)
                {
                    Entity.Mass = value;
                }
            }
        }

        private bool _isAffectedByGravity = true;
        public bool IsAffectedByGravity
        {
            get { return _isAffectedByGravity; }
            set
            {
                _isAffectedByGravity = value;
                if (Entity != null)
                {
                    Entity.IsAffectedByGravity = value;
                }
            }
        }

        [JsonIgnore]
        public Entity Entity { get; private set; }

        public Collider(float mass)
        {
            _mass = mass;
        }

        protected abstract Entity CreateEntity();

        public event TriggerEvent TriggerEntered;

        public event TriggerEvent TriggerExited;

        public bool IsTrigger
        {
            get { return _isTrigger; }
            set
            {
                if (_isTrigger != value)
                {
                    SetIsTrigger(value);
                }
            }
        }

        private void SetIsTrigger(bool value)
        {
            Debug.Assert(_isTrigger != value);
            _isTrigger = value;

            if (Entity != null && EnabledInHierarchy)
            {
                if (_isTrigger)
                {
                    SetEntityTrigger();
                }
                else
                {
                    UnsetEntityTrigger();
                }
            }
        }

        private void SetEntityTrigger()
        {
            Entity.IsAffectedByGravity = false;
            Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.NoSolver;
            Entity.LinearVelocity = Vector3.Zero;
            // TODO: Only subscribe to this if there are listeners; otherwise defer.
            SubscribeToEvents();
        }

        private void UnsetEntityTrigger()
        {
            Entity.IsAffectedByGravity = true;
            Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.Normal;
            Entity.LinearVelocity = Vector3.Zero;
            // TODO: Only unsubscribe from this if there are listeners.
            UnsubscribeFromEvents();
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

        protected sealed override void Attached(SystemRegistry registry)
        {
            _physicsSystem = registry.GetSystem<PhysicsSystem>();
            Entity = CreateEntity();
        }

        protected sealed override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            AddAndInitializeEntity();
            GameObject.Transform.ScaleChanged += ScaleChanged;
        }

        protected override void OnDisabled()
        {
            _physicsSystem.RemoveObject(Entity);
            GameObject.Transform.ScaleChanged -= ScaleChanged;
            Transform.RemovePhysicsEntity();
            if (_parentCollider != null)
            {
                _parentCollider.Transform.PositionManuallyChanged -= OnAttachedParentManuallyMoved;
            }

            if (_isTrigger)
            {
                UnsetEntityTrigger();
            }
        }

        private void AddAndInitializeEntity()
        {
            _physicsSystem.AddObject(Entity);
            Entity.Position = Transform.Position;
            Entity.Orientation = Transform.Rotation;
            Entity.IsAffectedByGravity = IsAffectedByGravity;
            Entity.Tag = this;
            Entity.CollisionInformation.Tag = this;

            _parentCollider = GameObject.GetComponentInParent<Collider>();
            if (_parentCollider != null)
            {
                CollisionRules.AddRule(Entity, _parentCollider.Entity, CollisionRule.NoBroadPhase);

                var jointPosition = (Entity.Position + _parentCollider.Entity.Position) / 2;
                _parentJoint = new WeldJoint(_parentCollider.Entity, Entity, jointPosition);
                _parentJoint.BallSocketJoint.SpringSettings.Stiffness = float.MaxValue;
                _parentJoint.BallSocketJoint.SpringSettings.Damping = float.MaxValue;
                _parentJoint.BallSocketJoint.IsActive = true;

                _parentJoint.NoRotationJoint.SpringSettings.Damping = float.MaxValue;
                _parentJoint.NoRotationJoint.SpringSettings.Stiffness = float.MaxValue;
                _parentJoint.NoRotationJoint.IsActive = true;

                _parentJoint.IsActive = true;
                _physicsSystem.AddObject(_parentJoint);
                _parentCollider.Transform.PositionManuallyChanged += OnAttachedParentManuallyMoved;
            }

            Transform.SetPhysicsEntity(Entity);

            if (_isTrigger)
            {
                SetEntityTrigger();
            }
        }

        private void OnAttachedParentManuallyMoved(Vector3 oldParentPos, Vector3 newParentPos)
        {
            Entity.Position += (newParentPos - oldParentPos);
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
            if (!(other.Tag is Collider))
            {
                Console.WriteLine("ERROR: TAG WAS NOT COLLIDER.");
            }
            else
            {
                Collider otherCollider = (Collider)other.Tag;
                TriggerEntered?.Invoke(otherCollider);
            }
        }

        private void OnCollisionPairRemoved(EntityCollidable sender, BroadPhaseEntry other)
        {
            if (other.Tag is Collider)
            {
                Collider otherCollider = (Collider)other.Tag;
                TriggerExited?.Invoke(otherCollider);
            }
            else
            {
                Console.WriteLine("ERROR: TAG WAS NOT COLLIDER.");
            }
        }
    }
}
