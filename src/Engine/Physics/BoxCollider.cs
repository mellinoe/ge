using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using System.Numerics;
using System;
using Newtonsoft.Json;

namespace Engine.Physics
{
    public class BoxCollider : Collider
    {
        [JsonProperty]
        private float _width;
        [JsonProperty]
        private float _height;
        [JsonProperty]
        private float _depth;

        public float Width
        {
            get { return _width; }
            set
            {
                _width = value;
                DimensionsChanged();
                
            }
        }

        public float Height
        {
            get { return _height; }
            set
            {
                _height = value;
                DimensionsChanged();

            }
        }

        public float Depth
        {
            get { return _depth; }
            set
            {
                _depth = value;
                DimensionsChanged();
            }
        }

        public BoxCollider() : this(1.0f, 1.0f, 1.0f, 1.0f) { }

        [JsonConstructor]
        public BoxCollider(float width, float height, float depth)
            : this(width, height, depth, 1.0f * (width * height * depth)) { }

        public BoxCollider(float width, float height, float depth, float mass)
            : base(mass)
        {
            _width = width;
            _height = height;
            _depth = depth;
        }

        protected override void PostAttached(SystemRegistry registry)
        {
            SetEntity(CreateEntity());
        }

        protected override Entity CreateEntity()
        {
            Vector3 scale = GameObject.Transform.Scale;
            return new Box(Vector3.Zero, _width * scale.X, _height * scale.Y, _depth * scale.Z, Mass);
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            Box box = (Box)Entity;
            box.Width = _width * scale.X;
            box.Height = _height * scale.Y;
            box.Length = _depth * scale.Z;
        }

        private void DimensionsChanged()
        {
            if (Entity != null)
            {
                Box box = (Box)Entity;
                Vector3 scale = Transform.Scale;
                box.Width = _width * scale.X;
                box.Height = _height * scale.Y;
                box.Length = _depth * scale.Z;
            }
        }
    }
}
