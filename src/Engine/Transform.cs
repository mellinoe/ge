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

        internal void SetPhysicsEntity(Entity entity)
        {
            Debug.Assert(_physicsEntity == null);
            Debug.Assert(entity != null);
            _physicsEntity = entity;
            _physicsEntity.PositionUpdated += OnPhysicsEntityMoved;
        }

        private void OnPhysicsEntityMoved(Entity entity)
        {
            OnPositionChanged();
            OnRotationChanged();
        }

        internal void RemovePhysicsEntity()
        {
            Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
            _localPosition = _physicsEntity.Position - parentPos;

            Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
            _localRotation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), _physicsEntity.Orientation);

            _physicsEntity.PositionUpdated -= OnPhysicsEntityMoved;

            _physicsEntity = null;
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
                        pos += Parent.Position;
                    }

                    return pos;
                }
                else
                {
                    return _physicsEntity.Position;
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
                    Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
                    return _physicsEntity.Position - parentPos;
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
                        Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
                        _physicsEntity.Position = parentPos + value;
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

        //internal void OnPhysicsUpdated(Entity obj)
        //{
        //    Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
        //    _localPosition = obj.Position - parentPos;
        //    OnPositionChanged();

        //    Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
        //    _localRotation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), obj.Orientation);
        //    OnRotationChanged();
        //}

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
                    return _physicsEntity.Orientation;
                }
            }
            set
            {
                if (value != Rotation)
                {
                    if (_physicsEntity == null)
                    {
                        Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                        _localRotation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), value);
                    }
                    else
                    {
                        _physicsEntity.Orientation = value;
                    }

                    OnRotationManuallyChanged();
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
                    return Quaternion.Concatenate(Quaternion.Inverse(parentRot), _physicsEntity.Orientation);
                }
            }
            set
            {
                if (_physicsEntity == null)
                {
                    _localRotation = value;
                }
                else
                {
                    Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                    _physicsEntity.Orientation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), value);

                }

                OnRotationManuallyChanged();
                OnRotationChanged();
            }
        }

        public event Action<Quaternion> RotationManuallyChanged;
        private void OnRotationManuallyChanged()
        {
            RotationManuallyChanged?.Invoke(Rotation);
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

                if (_parent != null)
                {
                    _parent._children.Remove(this);
                }

                Transform oldParent = _parent;
                _parent = value;
                _parent._children.Add(this);
                ParentChanged?.Invoke(this, oldParent, _parent);
            }
        }

        public IReadOnlyList<Transform> Children => _children;

        public Matrix4x4 GetWorldMatrix()
        {
            if (_physicsEntity != null)
            {
                return Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(_physicsEntity.Orientation)
                * Matrix4x4.CreateTranslation(_physicsEntity.Position);
            }

            Matrix4x4 mat = Matrix4x4.CreateScale(_localScale)
                * Matrix4x4.CreateFromQuaternion(_localRotation)
                * Matrix4x4.CreateTranslation(_localPosition);

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