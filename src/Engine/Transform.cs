using System;
using System.Collections.Generic;
using System.Numerics;
using BEPUphysics.Entities;
using System.Diagnostics;

namespace Engine
{
    public class Transform : Component
    {
        private Vector3 _localPosition;
        private Quaternion _localRotation = Quaternion.Identity;
        private Vector3 _localScale = Vector3.One;
        private Transform _parent;
        private readonly List<Transform> _children = new List<Transform>(0);

        private Entity _physicsEntity;

        public delegate void ParentChangedHandler(Transform t, Transform oldParent, Transform newParent);
        public event ParentChangedHandler ParentChanged;

        private void OnPhysicsEntityMoved(Entity entity)
        {
            OnPositionChanged();
            OnRotationChanged();
        }

        internal void SetPhysicsEntity(Entity entity)
        {
            Debug.Assert(_physicsEntity == null);
            Debug.Assert(entity != null);
            _physicsEntity = entity;
            _physicsEntity.PositionUpdated += OnPhysicsEntityMoved;
        }

        internal void RemovePhysicsEntity()
        {
            Vector3 localPosition = LocalPosition;
            Quaternion localRotation = LocalRotation;
            _physicsEntity = null;
            _localPosition = localPosition;
            _localRotation = localRotation;
        }

        public Vector3 Position
        {
            get
            {
                if (_physicsEntity == null)
                {
                    Vector3 pos = _localPosition;
                    if (Parent != null)
                    {
                        pos = Vector3.Transform(pos, Parent.GetWorldMatrix());
                    }

                    return pos;
                }
                else
                {
                    return GetInterpolatedPosition();
                }
            }
            set
            {
                Vector3 oldPosition = Position;
                if (_physicsEntity == null)
                {
                    Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
                    _localPosition = value - parentPos;
                }
                else
                {
                    _physicsEntity.Position = value;
                }

                OnPositionManuallyChanged(oldPosition);
                OnPositionChanged();
            }
        }

        private Vector3 GetInterpolatedPosition()
        {
            return _physicsEntity.BufferedStates.InterpolatedStates.Position;
        }

        public Vector3 LocalPosition
        {
            get
            {
                if (_physicsEntity == null)
                {
                    return _localPosition;
                }
                else
                {
                    Matrix4x4 invWorld = Matrix4x4.Identity;
                    if (Parent != null)
                    {
                        Matrix4x4.Invert(Parent.GetWorldMatrix(), out invWorld);
                    }

                    return Vector3.Transform(GetInterpolatedPosition(), invWorld);

                }
            }
            set
            {
                Vector3 oldPosition = Position;
                if (value != oldPosition)
                {
                    if (_physicsEntity == null)
                    {
                        _localPosition = value;
                    }
                    else
                    {
                        Matrix4x4 world = Parent != null ? Parent.GetWorldMatrix() : Matrix4x4.Identity;
                        _physicsEntity.Position = Vector3.Transform(value, world);
                    }

                    OnPositionChanged();
                    OnPositionManuallyChanged(oldPosition);
                }
            }
        }

        public event Action<Vector3, Vector3> PositionManuallyChanged;
        private void OnPositionManuallyChanged(Vector3 oldPosition)
        {
            PositionManuallyChanged?.Invoke(oldPosition, Position);
        }

        public event Action<Transform> TransformChanged;
        public event Action<Vector3> PositionChanged;
        private void OnPositionChanged()
        {
            PositionChanged?.Invoke(Position);
            TransformChanged?.Invoke(this);
        }

        public Quaternion Rotation
        {
            get
            {
                if (_physicsEntity == null)
                {
                    Quaternion rot = _localRotation;
                    if (Parent != null)
                    {
                        rot = Quaternion.Concatenate(Parent.Rotation, rot);
                    }

                    return rot;
                }
                else
                {
                    return _physicsEntity.BufferedStates.InterpolatedStates.Orientation;
                }
            }
            set
            {
                if (value != Rotation)
                {
                    Quaternion oldRotation = Rotation;
                    if (_physicsEntity == null)
                    {
                        Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                        _localRotation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), value);
                    }
                    else
                    {
                        _physicsEntity.Orientation = value;
                    }

                    OnRotationManuallyChanged(oldRotation);
                    OnRotationChanged();
                }
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                if (_physicsEntity == null)
                {
                    return _localRotation;
                }
                else
                {
                    Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                    return Quaternion.Concatenate(Quaternion.Inverse(parentRot), _physicsEntity.BufferedStates.InterpolatedStates.Orientation);
                }
            }
            set
            {
                Quaternion oldRotation = Rotation;
                if (_physicsEntity == null)
                {
                    _localRotation = value;
                }
                else
                {
                    Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                    _physicsEntity.Orientation = Quaternion.Concatenate(parentRot, value);
                }

                OnRotationManuallyChanged(oldRotation);
                OnRotationChanged();
            }
        }

        public event Action<Quaternion, Quaternion> RotationManuallyChanged;
        private void OnRotationManuallyChanged(Quaternion oldRotation)
        {
            RotationManuallyChanged?.Invoke(oldRotation, Rotation);
        }

        public event Action<Quaternion> RotationChanged;
        private void OnRotationChanged()
        {
            RotationChanged?.Invoke(Rotation);
            TransformChanged?.Invoke(this);
        }

        public Vector3 Scale
        {
            get
            {
                Vector3 scale = _localScale;
                if (Parent != null)
                {
                    scale *= Parent.Scale;
                }

                return scale;
            }
            set
            {
                Vector3 parentScale = Parent != null ? Parent.Scale : Vector3.One;
                _localScale = value / parentScale;
                OnScalechanged();
            }
        }

        public Vector3 LocalScale
        {
            get { return _localScale; }
            set
            {
                _localScale = value;
                OnScalechanged();
            }
        }

        internal event Action<Vector3> ScaleChanged;
        private void OnScalechanged()
        {
            ScaleChanged?.Invoke(Scale);
            TransformChanged?.Invoke(this);
        }

        public Transform Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (value == this)
                {
                    throw new InvalidOperationException("Cannot set a Transform's parent to itself.");
                }

                SetParent(value);
            }
        }

        /// <summary>
        /// Called when this transform's parent changes.
        /// </summary>
        /// <param name="newParent">The new parent. May be null.</param>
        private void SetParent(Transform newParent)
        {
            var oldParent = _parent;
            if (oldParent != null)
            {
                oldParent._children.Remove(this);
                oldParent.TransformChanged -= OnParentTransformChanged;
                oldParent.PositionManuallyChanged -= OnParentPositionChanged;
                oldParent.RotationManuallyChanged -= OnParentRotationChanged;
            }

            _parent = newParent;
            if (newParent != null)
            {
                newParent._children.Add(this);
                newParent.TransformChanged += OnParentTransformChanged;
                newParent.PositionManuallyChanged += OnParentPositionChanged;
                newParent.RotationManuallyChanged += OnParentRotationChanged;
            }

            ParentChanged?.Invoke(this, oldParent, _parent);
            if (_physicsEntity == null)
            {
                TransformChanged?.Invoke(this);
                OnPositionChanged();
            }
        }

        private void OnParentTransformChanged(Transform obj)
        {
            TransformChanged?.Invoke(this);
        }

        private void OnParentPositionChanged(Vector3 oldPos, Vector3 newPos)
        {
            OnPositionChanged();
            OnPositionManuallyChanged(oldPos + _localPosition);
            if (_physicsEntity != null)
            {
                var diff = newPos - oldPos;
                _physicsEntity.Position += diff;
            }
        }

        private void OnParentRotationChanged(Quaternion oldParentRot, Quaternion newParentRot)
        {
            Quaternion oldRot;
            if (_physicsEntity != null)
            {
                Quaternion localRotation = Quaternion.Concatenate(Quaternion.Inverse(oldParentRot), _physicsEntity.BufferedStates.InterpolatedStates.Orientation);
                oldRot = Quaternion.Concatenate(oldParentRot, localRotation);
                Quaternion diff = Quaternion.Concatenate(Quaternion.Inverse(oldParentRot), newParentRot);
                _physicsEntity.Orientation = Quaternion.Concatenate(_physicsEntity.BufferedStates.InterpolatedStates.Orientation, diff);
                Vector3 basisDirection = Vector3.Transform(GetInterpolatedPosition() - _parent.Position, Quaternion.Inverse(oldRot));
                float distance = basisDirection.Length();
                Vector3 newDirection = Vector3.Transform(basisDirection, newParentRot);
                if (newDirection != Vector3.Zero)
                {
                    _physicsEntity.Position = _parent.Position + Vector3.Normalize(newDirection) * distance;
                }

            }
            else
            {
                oldRot = _localRotation;
            }

            OnRotationChanged();
            OnRotationManuallyChanged(oldRot);
        }

        public IReadOnlyList<Transform> Children => _children;

        public Matrix4x4 GetWorldMatrix()
        {
            if (_physicsEntity != null)
            {
                Vector3 parentScale = Parent != null ? Parent.Scale : Vector3.One;
                return Matrix4x4.CreateScale(_localScale * parentScale)
                * Matrix4x4.CreateFromQuaternion(_physicsEntity.BufferedStates.InterpolatedStates.Orientation)
                * Matrix4x4.CreateTranslation(GetInterpolatedPosition());
            }

            Matrix4x4 mat = Matrix4x4.CreateScale(_localScale)
                * Matrix4x4.CreateFromQuaternion(LocalRotation)
                * Matrix4x4.CreateTranslation(LocalPosition);

            if (Parent != null)
            {
                mat *= Parent.GetWorldMatrix();
            }

            return mat;
        }

        public Vector3 Forward
        {
            get
            {
                return Vector3.Transform(-Vector3.UnitZ, Rotation);
            }
        }

        public Vector3 Up
        {
            get
            {
                return Vector3.Transform(Vector3.UnitY, Rotation);
            }
        }

        public Vector3 Right
        {
            get
            {
                return Vector3.Transform(Vector3.UnitX, Rotation);
            }
        }

        public Vector3 GetLocalOrPhysicsEntityPosition()
        {
            return (_physicsEntity != null) ? GetInterpolatedPosition() : _localPosition;
        }

        public Quaternion GetLocalOrPhysicsEntityRotation()
        {
            return (_physicsEntity != null) ? _physicsEntity.BufferedStates.InterpolatedStates.Orientation : _localRotation;
        }

        protected override void Attached(SystemRegistry registry)
        {
        }

        protected override void Removed(SystemRegistry registry)
        {
            if (Parent != null)
            {
                bool result = Parent._children.Remove(this);
                Debug.Assert(result);
            }
        }

        protected override void OnEnabled()
        {
        }

        protected override void OnDisabled()
        {
        }

        public override string ToString()
        {
            return $"[Transform] {GameObject.ToString()}";
        }
    }
}