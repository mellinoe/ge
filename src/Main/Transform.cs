using System;
using System.Collections.Generic;
using System.Numerics;
using BEPUphysics.Entities;

namespace Ge
{
    public class Transform : Component
    {
        private Vector3 _localPosition;
        private Quaternion _localRotation = Quaternion.Identity;
        private Vector3 _localScale = Vector3.One;
        private Transform _parent;
        private readonly List<Transform> _children = new List<Transform>(0);

        public Vector3 Position
        {
            get
            {
                Vector3 pos = _localPosition;
                if (Parent != null)
                {
                    pos += Parent.Position;
                }

                return pos;
            }
            set
            {
                Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
                _localPosition = value - parentPos;
                OnPositionManuallyChanged();
                OnPositionChanged();
            }
        }

        public Vector3 LocalPosition
        {
            get { return _localPosition; }
            set
            {
                _localPosition = value;
                OnPositionChanged();
                OnPositionManuallyChanged();
            }
        }

        public event Action<Vector3> PositionManuallyChanged;
        private void OnPositionManuallyChanged()
        {
            PositionManuallyChanged?.Invoke(Position);
        }

        public event Action<Transform> TransformChanged;
        public event Action<Vector3> PositionChanged;
        private void OnPositionChanged()
        {
            PositionChanged?.Invoke(Position);
            TransformChanged?.Invoke(this);
        }

        internal void OnPhysicsUpdated(Entity obj)
        {
            Position = obj.Position;
            Rotation = obj.Orientation;
            OnPositionChanged();
        }

        public Quaternion Rotation
        {
            get
            {
                Quaternion rot = _localRotation;
                if (Parent != null)
                {
                    rot = Quaternion.Concatenate(Parent.Rotation, rot);
                }

                return rot;
            }
            set
            {
                Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                _localRotation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), value);
                OnRotationManuallyChanged();
                OnRotationChanged();
            }
        }

        public Quaternion LocalRotation
        {
            get { return _localRotation; }
            set
            {
                _localRotation = value;
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

                _parent = value;
                _parent._children.Add(this);
            }
        }

        public IReadOnlyList<Transform> Children => _children;

        public Matrix4x4 GetWorldMatrix()
        {
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
                return Vector3.Transform(Vector3.UnitZ, Rotation);
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


        public override void Attached(SystemRegistry registry)
        {
        }

        public override void Removed(SystemRegistry registry)
        {
        }
    }
}