using System.Numerics;
using BEPUphysics.Entities;
using System.Collections.Generic;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.CollisionShapes;
using Newtonsoft.Json;
using System.Linq;
using BEPUphysics.CollisionShapes.ConvexShapes;

namespace Engine.Physics
{
    public class CompoundShapeCollider : Collider
    {
        [JsonProperty]
        private readonly IList<BoxShapeDescription> _shapes;

        public Vector3 EntityCenter { get; private set; }

        [JsonConstructor]
        public CompoundShapeCollider(IList<BoxShapeDescription> shapes, float mass)
            : base(mass)
        {
            _shapes = shapes;
        }

        protected override void PostAttached(SystemRegistry registry)
        {
            SetEntity(CreateEntity());
        }

        protected override Entity CreateEntity()
        {
            CompoundBody cb = new CompoundBody(
                _shapes.Select(bse => BoxShapeDescription.Scale(bse, Transform.Scale).GetShapeEntry()).ToList(), Mass);
            EntityCenter = -cb.Position;
            return cb;
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            SetEntity(CreateEntity());
        }
    }

    public abstract class ShapeDescription
    {
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }

        public ShapeDescription(Vector3 position, Quaternion orientation)
        {
            Position = position;
            Orientation = orientation;
        }

        public CompoundShapeEntry GetShapeEntry()
        {
            return new CompoundShapeEntry(GetEnityShape(), new BEPUutilities.RigidTransform(Position, Orientation));
        }

        public abstract EntityShape GetEnityShape();
    }

    public class BoxShapeDescription : ShapeDescription
    {
        public Vector3 Dimensions { get; set; }

        public BoxShapeDescription(Vector3 dimensions, Vector3 position)
            : this(dimensions, position, Quaternion.Identity) { }

        [JsonConstructor]
        public BoxShapeDescription(Vector3 dimensions, Vector3 position, Quaternion orientation)
            : base(position, orientation)
        {
            Dimensions = dimensions;
            Position = position;
            Orientation = orientation;
        }

        public override EntityShape GetEnityShape()
        {
            return new BoxShape(Dimensions.X, Dimensions.Y, Dimensions.Z);
        }

        public static BoxShapeDescription Scale(BoxShapeDescription bse, Vector3 scale)
        {
            return new BoxShapeDescription(bse.Dimensions * scale, bse.Position * scale, bse.Orientation);
        }
    }

    public class SphereShapeDescription : ShapeDescription
    {
        public float Radius { get; private set; }

        public SphereShapeDescription(float radius, Vector3 position, Quaternion orientation) : base(position, orientation)
        {
            Radius = radius;
        }

        public override EntityShape GetEnityShape()
        {
            return new SphereShape(Radius);
        }
    }
}
