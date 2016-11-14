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
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.EntityStateManagement;

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

        private UpdateMode _updateMode = UpdateMode.Discrete;
        public UpdateMode UpdateMode
        {
            get
            {
                return _updateMode;
            }
            set
            {
                _updateMode = value;
                if (Entity != null)
                {
                    Entity.PositionUpdateMode = MapUpdateMode(_updateMode);
                }
            }
        }

        private float _angularDamping = .15f;
        public float AngularDamping
        {
            get { return _angularDamping; }
            set
            {
                _angularDamping = value;
                if (Entity != null)
                {
                    Entity.AngularDamping = value;
                }
            }
        }

        private float _linearDamping = .03f;
        public float LinearDamping
        {
            get { return _linearDamping; }
            set
            {
                _linearDamping = value;
                if (Entity != null)
                {
                    Entity.LinearDamping = value;
                }
            }
        }

        private float _bounciness = BEPUphysics.Materials.MaterialManager.DefaultBounciness;
        public float Bounciness
        {
            get { return _bounciness; }
            set
            {
                _bounciness = value;
                if (Entity != null)
                {
                    Entity.Material.Bounciness = value;
                }
            }
        }

        private float _staticFriction = BEPUphysics.Materials.MaterialManager.DefaultStaticFriction;
        public float StaticFriction
        {
            get { return _staticFriction; }
            set
            {
                _staticFriction = value;
                if (Entity != null)
                {
                    Entity.Material.StaticFriction = value;
                }
            }
        }

        private float _kineticFriction = BEPUphysics.Materials.MaterialManager.DefaultKineticFriction;
        public float KineticFriction
        {
            get { return _kineticFriction; }
            set
            {
                _kineticFriction = value;
                if (Entity != null)
                {
                    Entity.Material.KineticFriction = value;
                }
            }
        }

        private bool _restrictLinearMotion;
        private MaximumLinearSpeedConstraint _linearSpeedConstraint;
        public bool RestrictLinearMotion
        {
            get { return _restrictLinearMotion; }
            set
            {
                _restrictLinearMotion = value;
                if (Entity != null)
                {
                    if (_restrictLinearMotion && _linearSpeedConstraint == null)
                    {
                        _linearSpeedConstraint = new MaximumLinearSpeedConstraint(Entity, 0f);
                        _physicsSystem.AddObject(_linearSpeedConstraint);
                    }
                    else if (!_restrictLinearMotion && _linearSpeedConstraint != null)
                    {
                        _physicsSystem.RemoveObject(_linearSpeedConstraint);
                        _linearSpeedConstraint = null;
                    }
                }
            }
        }

        private int _layer;
        public int Layer
        {
            get { return _layer; }
            set
            {
                if (_layer < 0 || (_physicsSystem != null && _layer >= _physicsSystem.LayerCount))
                {
                    throw new ArgumentOutOfRangeException(nameof(Layer));
                }

                _layer = value;
                if (Entity != null)
                {
                    Entity.CollisionInformation.CollisionRules.Group = _physicsSystem.GetCollisionGroup(_layer);
                }
            }
        }

        [JsonIgnore]
        public Entity Entity { get; private set; }

        public Collider(float mass)
        {
            _mass = mass;
        }

        public void WakeUp()
        {
            if (Entity != null)
            {
                Entity.ActivityInformation.Activate();
            }
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
            PostAttached(registry);
        }

        protected virtual void PostAttached(SystemRegistry registry) { }

        protected sealed override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            GameObject.Transform.ScaleChanged += ScaleChanged;
            if (Entity != null)
            {
                _physicsSystem.AddObject(Entity);
                Transform.SetPhysicsEntity(Entity);
            }

            GameObject.Transform.ScaleChanged += ScaleChanged;
            if (_parentCollider != null)
            {
                _parentCollider.Transform.PositionManuallyChanged += OnAttachedParentManuallyMoved;
            }

            if (_isTrigger)
            {
                SetEntityTrigger();
            }
        }

        protected override void OnDisabled()
        {
            if (Entity != null)
            {
                if (_linearSpeedConstraint != null)
                {
                    _physicsSystem.RemoveObject(_linearSpeedConstraint);
                }

                _physicsSystem.RemoveObject(Entity);
                Transform.RemovePhysicsEntity();
            }

            GameObject.Transform.ScaleChanged -= ScaleChanged;
            if (_parentCollider != null)
            {
                _parentCollider.Transform.PositionManuallyChanged -= OnAttachedParentManuallyMoved;
            }

            if (_isTrigger)
            {
                UnsetEntityTrigger();
            }
        }

        // Must be called by subclasses when they are able to construct an Entity.
        protected void SetEntity(Entity entity)
        {
            if (Entity != null)
            {
                _physicsSystem.RemoveObject(Entity);
                Transform.RemovePhysicsEntity();
            }

            Entity = entity;
            Entity.Position = Transform.Position;
            Entity.Orientation = Transform.Rotation;
            Entity.IsAffectedByGravity = IsAffectedByGravity;
            Entity.PositionUpdateMode = MapUpdateMode(UpdateMode);
            Entity.AngularDamping = AngularDamping;
            Entity.LinearDamping = LinearDamping;
            Entity.Material.Bounciness = Bounciness;
            Entity.Material.StaticFriction = StaticFriction;
            Entity.Material.KineticFriction = KineticFriction;

            Entity.Tag = this;
            Entity.CollisionInformation.Tag = this;
            Entity.CollisionInformation.CollisionRules.Group = _physicsSystem.GetCollisionGroup(_layer);

            if (EnabledInHierarchy)
            {
                _physicsSystem.AddObject(Entity);
                Transform.SetPhysicsEntity(Entity);
                if (_isTrigger)
                {
                    SetEntityTrigger();
                }
            }

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
        }

        private PositionUpdateMode MapUpdateMode(UpdateMode updateMode)
        {
            switch (_updateMode)
            {
                case UpdateMode.Discrete:
                    return PositionUpdateMode.Discrete;
                case UpdateMode.Continuous:
                    return PositionUpdateMode.Continuous;
                case UpdateMode.Passive:
                    return PositionUpdateMode.Passive;
                default:
                    throw new InvalidOperationException("Invalid update mode: " + _updateMode);
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
