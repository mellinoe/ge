using System;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using System.Numerics;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Engine.Physics
{
    public class SphereCollider : Collider
    {
        [JsonProperty]
        private float _radius;

        public float Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                Debug.Assert(Entity != null);
                ((Sphere)Entity).Radius = value; // Will Entity always be non-null?
            }
        }

        public SphereCollider() : this(1.0f) { }

        public SphereCollider(float radius) : this(radius, (4f / 3f) * (float)Math.PI * radius * radius * radius)
        { }

        [JsonConstructor]
        public SphereCollider(float radius, float mass)
            : base(mass)
        {
            _radius = radius;
        }

        protected override void PostAttached(SystemRegistry registry)
        {
            SetEntity(CreateEntity());
        }

        protected override Entity CreateEntity()
        {
            Vector3 scale = GameObject.Transform.Scale;
            return new Sphere(Vector3.Zero, scale.X * _radius, Mass);
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            Sphere sphere = (Sphere)Entity;
            sphere.Radius = _radius * scale.X;
        }
    }
}
