using System;
using System.Collections.Generic;
using System.Numerics;

namespace Ge
{
    public class Transform : Component
    {
        private Vector3 _position;
        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                OnPositionChanged();
            }
        }

        public event Action<Transform> TransformChanged;

        public event Action<Vector3> PositionChanged;
        private void OnPositionChanged()
        {
            PositionChanged?.Invoke(_position);
            TransformChanged?.Invoke(this);
        }

        public Quaternion _rotation = Quaternion.Identity;
        public Quaternion Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                OnRotationChanged();
            }
        }

        public event Action<Quaternion> RotationChanged;
        private void OnRotationChanged()
        {
            RotationChanged?.Invoke(_rotation);
            TransformChanged?.Invoke(this);
        }

        private Vector3 _scale = Vector3.One;
        public Vector3 Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                OnScalechanged();
            }
        }

        internal event Action<Vector3> ScaleChanged;
        private void OnScalechanged()
        {
            ScaleChanged?.Invoke(_scale);
            TransformChanged?.Invoke(this);
        }

        private Transform _parent;
        private readonly List<Transform> _children = new List<Transform>(0);
        public Transform Parent
        {
            get
            {
                return _parent;
            }
            set
            {
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
            Matrix4x4 mat = Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(Rotation)
                * Matrix4x4.CreateTranslation(Position);

            if (_parent != null)
            {
                mat *= _parent.GetWorldMatrix();
            }

            return mat;
        }

        public Vector3 Forward
        {
            get
            {
                return Vector3.Transform(Vector3.UnitZ, _rotation);
            }
        }

        public Vector3 Up
        {
            get
            {
                return Vector3.Transform(Vector3.UnitY, _rotation);
            }
        }

        public Vector3 Right
        {
            get
            {
                return Vector3.Transform(Vector3.UnitX, _rotation);
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