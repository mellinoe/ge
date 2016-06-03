using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Entities;
using BEPUphysics.CollisionRuleManagement;
using BEPUutilities;
using BEPUphysics.Materials;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Uses a cylinder cast as the shape of a wheel.
    /// </summary>
    public class CylinderCastWheelShape : WheelShape
    {
        private CylinderShape shape;

        private System.Numerics.Quaternion localWheelOrientation;
        /// <summary>
        /// Gets or sets the unsteered orientation of the wheel in the vehicle's local space.
        /// </summary>
        public System.Numerics.Quaternion LocalWheelOrientation
        {
            get { return localWheelOrientation; }
            set { localWheelOrientation = value; }
        }

        /// <summary>
        /// Creates a new cylinder cast based wheel shape.
        /// </summary>
        /// <param name="radius">Radius of the wheel.</param>
        /// <param name="width">Width of the wheel.</param>
        /// <param name="localWheelOrientation">Unsteered orientation of the wheel in the vehicle's local space.</param>
        /// <param name="localGraphicTransform">Local graphic transform of the wheel shape.
        /// This transform is applied first when creating the shape's worldTransform.</param>
        /// <param name="includeSteeringTransformInCast">Whether or not to include the steering transform in the wheel shape cast. If false, the casted wheel shape will always point straight forward.
        /// If true, it will rotate with steering. Sometimes, setting this to false is helpful when the cast shape would otherwise become exposed when steering.</param>
        public CylinderCastWheelShape(float radius, float width, System.Numerics.Quaternion localWheelOrientation, System.Numerics.Matrix4x4 localGraphicTransform, bool includeSteeringTransformInCast)
        {
            shape = new CylinderShape(width, radius);
            this.LocalWheelOrientation = localWheelOrientation;
            LocalGraphicTransform = localGraphicTransform;
            this.IncludeSteeringTransformInCast = includeSteeringTransformInCast;
        }

        /// <summary>
        /// Gets or sets the radius of the wheel.
        /// </summary>
        public override sealed float Radius
        {
            get { return shape.Radius; }
            set
            {
                shape.Radius = MathHelper.Max(value, 0);
                Initialize();
            }
        }

        /// <summary>
        /// Gets or sets the width of the wheel.
        /// </summary>
        public float Width
        {
            get { return shape.Height; }
            set
            {
                shape.Height = MathHelper.Max(value, 0);
                Initialize();
            }
        }

        /// <summary>
        /// Gets or sets whether or not to include the rotation from steering in the cast. If false, the casted wheel shape will always point straight forward.
        /// If true, it will rotate with steering. Sometimes, setting this to false is helpful when the cast shape would otherwise become exposed when steering.
        /// </summary>
        public bool IncludeSteeringTransformInCast { get; set; }

        /// <summary>
        /// Updates the wheel's world transform for graphics.
        /// Called automatically by the owning wheel at the end of each frame.
        /// If the engine is updating asynchronously, you can call this inside of a space read buffer lock
        /// and update the wheel transforms safely.
        /// </summary>
        public override void UpdateWorldTransform()
        {
#if !WINDOWS
            System.Numerics.Vector3 newPosition = new System.Numerics.Vector3();
#else
            System.Numerics.Vector3 newPosition;
#endif
            System.Numerics.Vector3 worldAttachmentPoint;
            System.Numerics.Vector3 localAttach;
            Vector3Ex.Add(ref wheel.suspension.localAttachmentPoint, ref wheel.vehicle.Body.CollisionInformation.localPosition, out localAttach);
            worldTransform = Matrix3x3.ToMatrix4X4(wheel.vehicle.Body.BufferedStates.InterpolatedStates.OrientationMatrix);

            Matrix4x4Ex.TransformNormal(ref localAttach, ref worldTransform, out worldAttachmentPoint);
            worldAttachmentPoint += wheel.vehicle.Body.BufferedStates.InterpolatedStates.Position;

            System.Numerics.Vector3 worldDirection;
            Matrix4x4Ex.Transform(ref wheel.suspension.localDirection, ref worldTransform, out worldDirection);

            float length = wheel.suspension.currentLength;
            newPosition.X = worldAttachmentPoint.X + worldDirection.X * length;
            newPosition.Y = worldAttachmentPoint.Y + worldDirection.Y * length;
            newPosition.Z = worldAttachmentPoint.Z + worldDirection.Z * length;

            System.Numerics.Matrix4x4 spinTransform;

            System.Numerics.Vector3 localSpinAxis;
            QuaternionEx.Transform(ref Toolbox.UpVector, ref localWheelOrientation, out localSpinAxis);
            Matrix4x4Ex.CreateFromAxisAngle(ref localSpinAxis, spinAngle, out spinTransform);


            System.Numerics.Matrix4x4 localTurnTransform;
            Matrix4x4Ex.Multiply(ref localGraphicTransform, ref spinTransform, out localTurnTransform);
            Matrix4x4Ex.Multiply(ref localTurnTransform, ref steeringTransform, out localTurnTransform);
            //System.Numerics.Matrix4x4.Multiply(ref localTurnTransform, ref spinTransform, out localTurnTransform);
            Matrix4x4Ex.Multiply(ref localTurnTransform, ref worldTransform, out worldTransform);
            worldTransform.Translation += newPosition;
        }

        /// <summary>
        /// Finds a supporting entity, the contact location, and the contact normal.
        /// </summary>
        /// <param name="location">Contact point between the wheel and the support.</param>
        /// <param name="normal">Contact normal between the wheel and the support.</param>
        /// <param name="suspensionLength">Length of the suspension at the contact.</param>
        /// <param name="supportingCollidable">Collidable supporting the wheel, if any.</param>
        /// <param name="entity">Supporting object.</param>
        /// <param name="material">Material of the wheel.</param>
        /// <returns>Whether or not any support was found.</returns>
        protected internal override bool FindSupport(out System.Numerics.Vector3 location, out System.Numerics.Vector3 normal, out float suspensionLength, out Collidable supportingCollidable, out Entity entity, out Material material)
        {
            suspensionLength = float.MaxValue;
            location = Toolbox.NoVector;
            supportingCollidable = null;
            entity = null;
            normal = Toolbox.NoVector;
            material = null;

            Collidable testCollidable;
            RayHit rayHit;

            bool hit = false;

            System.Numerics.Quaternion localSteeringTransform;
            QuaternionEx.CreateFromAxisAngle(ref wheel.suspension.localDirection, steeringAngle, out localSteeringTransform);
            var startingTransform = new RigidTransform
            {
                Position = wheel.suspension.worldAttachmentPoint,
                Orientation = QuaternionEx.Concatenate(QuaternionEx.Concatenate(LocalWheelOrientation, IncludeSteeringTransformInCast ? localSteeringTransform : System.Numerics.Quaternion.Identity), wheel.vehicle.Body.orientation)
            };
            System.Numerics.Vector3 sweep;
            Vector3Ex.Multiply(ref wheel.suspension.worldDirection, wheel.suspension.restLength, out sweep);

            for (int i = 0; i < detector.CollisionInformation.pairs.Count; i++)
            {
                var pair = detector.CollisionInformation.pairs[i];
                testCollidable = (pair.BroadPhaseOverlap.entryA == detector.CollisionInformation ? pair.BroadPhaseOverlap.entryB : pair.BroadPhaseOverlap.entryA) as Collidable;
                if (testCollidable != null)
                {
                    if (CollisionRules.CollisionRuleCalculator(this, testCollidable) == CollisionRule.Normal &&
                        testCollidable.ConvexCast(shape, ref startingTransform, ref sweep, out rayHit) &&
                        rayHit.T * wheel.suspension.restLength < suspensionLength)
                    {
                        suspensionLength = rayHit.T * wheel.suspension.restLength;
                        EntityCollidable entityCollidable;
                        if ((entityCollidable = testCollidable as EntityCollidable) != null)
                        {
                            entity = entityCollidable.Entity;
                            material = entityCollidable.Entity.Material;
                        }
                        else
                        {
                            entity = null;
                            supportingCollidable = testCollidable;
                            var materialOwner = testCollidable as IMaterialOwner;
                            if (materialOwner != null)
                                material = materialOwner.Material;
                        }
                        location = rayHit.Location;
                        normal = rayHit.Normal;
                        hit = true;
                    }
                }
            }
            if (hit)
            {
                if (suspensionLength > 0)
                {
                    float dot;
                    Vector3Ex.Dot(ref normal, ref wheel.suspension.worldDirection, out dot);
                    if (dot > 0)
                    {
                        //The cylinder cast produced a normal which is opposite of what we expect.
                        Vector3Ex.Negate(ref normal, out normal);
                    }
                    normal.Normalize();
                }
                else
                    Vector3Ex.Negate(ref wheel.suspension.worldDirection, out normal);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Initializes the detector entity and any other necessary logic.
        /// </summary>
        protected internal override void Initialize()
        {
            //Setup the dimensions of the detector.
            var initialTransform = new RigidTransform { Orientation = LocalWheelOrientation };
            BoundingBox boundingBox;
            shape.GetBoundingBox(ref initialTransform, out boundingBox);
            var expansion = wheel.suspension.localDirection * wheel.suspension.restLength;
            if (expansion.X > 0)
                boundingBox.Max.X += expansion.X;
            else if (expansion.X < 0)
                boundingBox.Min.X += expansion.X;

            if (expansion.Y > 0)
                boundingBox.Max.Y += expansion.Y;
            else if (expansion.Y < 0)
                boundingBox.Min.Y += expansion.Y;

            if (expansion.Z > 0)
                boundingBox.Max.Z += expansion.Z;
            else if (expansion.Z < 0)
                boundingBox.Min.Z += expansion.Z;


            detector.Width = boundingBox.Max.X - boundingBox.Min.X;
            detector.Height = boundingBox.Max.Y - boundingBox.Min.Y;
            detector.Length = boundingBox.Max.Z - boundingBox.Min.Z;
        }

        /// <summary>
        /// Updates the position of the detector before each step.
        /// </summary>
        protected internal override void UpdateDetectorPosition()
        {
#if !WINDOWS
            System.Numerics.Vector3 newPosition = new System.Numerics.Vector3();
#else
            System.Numerics.Vector3 newPosition;
#endif

            newPosition.X = wheel.suspension.worldAttachmentPoint.X + wheel.suspension.worldDirection.X * wheel.suspension.restLength * .5f;
            newPosition.Y = wheel.suspension.worldAttachmentPoint.Y + wheel.suspension.worldDirection.Y * wheel.suspension.restLength * .5f;
            newPosition.Z = wheel.suspension.worldAttachmentPoint.Z + wheel.suspension.worldDirection.Z * wheel.suspension.restLength * .5f;

            detector.Position = newPosition;
            if (IncludeSteeringTransformInCast)
            {
                System.Numerics.Quaternion localSteeringTransform;
                QuaternionEx.CreateFromAxisAngle(ref wheel.suspension.localDirection, steeringAngle, out localSteeringTransform);

                detector.Orientation = QuaternionEx.Concatenate(localSteeringTransform, wheel.Vehicle.Body.orientation);
            }
            else
            {
                detector.Orientation = wheel.Vehicle.Body.orientation;
            }
            System.Numerics.Vector3 linearVelocity;
            Vector3Ex.Subtract(ref newPosition, ref wheel.vehicle.Body.position, out linearVelocity);
            Vector3Ex.Cross(ref linearVelocity, ref wheel.vehicle.Body.angularVelocity, out linearVelocity);
            Vector3Ex.Add(ref linearVelocity, ref wheel.vehicle.Body.linearVelocity, out linearVelocity);
            detector.LinearVelocity = linearVelocity;
            detector.AngularVelocity = wheel.vehicle.Body.angularVelocity;
        }
    }
}