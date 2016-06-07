using System;
using System.Numerics;

namespace Ge
{
    public class Transform : Component
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public Matrix4x4 GetWorldMatrix()
        {
            return Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(Rotation)
                * Matrix4x4.CreateTranslation(Position);
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

        protected override void Initialize(SystemRegistry registry)
        {
        }
    }
}