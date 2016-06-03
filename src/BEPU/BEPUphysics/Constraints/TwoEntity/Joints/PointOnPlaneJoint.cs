using BEPUphysics.Entities;

using BEPUutilities;

namespace BEPUphysics.Constraints.TwoEntity.Joints
{
    /// <summary>
    /// Constrains a point on one body to be on a plane defined by another body.
    /// </summary>
    public class PointOnPlaneJoint : Joint, I1DImpulseConstraintWithError, I1DJacobianConstraint
    {
        private float accumulatedImpulse;
        private float biasVelocity;
        private float error;

        private System.Numerics.Vector3 localPlaneAnchor;
        private System.Numerics.Vector3 localPlaneNormal;
        private System.Numerics.Vector3 localPointAnchor;

        private System.Numerics.Vector3 worldPlaneAnchor;
        private System.Numerics.Vector3 worldPlaneNormal;
        private System.Numerics.Vector3 worldPointAnchor;
        private float negativeEffectiveMass;
        private System.Numerics.Vector3 rA;
        private System.Numerics.Vector3 rAcrossN;
        private System.Numerics.Vector3 rB;
        private System.Numerics.Vector3 rBcrossN;

        /// <summary>
        /// Constructs a new point on plane constraint.
        /// To finish the initialization, specify the connections (ConnectionA and ConnectionB) 
        /// as well as the PlaneAnchor, PlaneNormal, and PointAnchor (or their entity-local versions).
        /// This constructor sets the constraint's IsActive property to false by default.
        /// </summary>
        public PointOnPlaneJoint()
        {
            IsActive = false;
        }

        /// <summary>
        /// Constructs a new point on plane constraint.
        /// </summary>
        /// <param name="connectionA">Entity to which the constraint's plane is attached.</param>
        /// <param name="connectionB">Entity to which the constraint's point is attached.</param>
        /// <param name="planeAnchor">A point on the plane.</param>
        /// <param name="normal">Direction, attached to the first connected entity, defining the plane's normal</param>
        /// <param name="pointAnchor">The point to constrain to the plane, attached to the second connected object.</param>
        public PointOnPlaneJoint(Entity connectionA, Entity connectionB, System.Numerics.Vector3 planeAnchor, System.Numerics.Vector3 normal, System.Numerics.Vector3 pointAnchor)
        {
            ConnectionA = connectionA;
            ConnectionB = connectionB;

            PointAnchor = pointAnchor;
            PlaneAnchor = planeAnchor;
            PlaneNormal = normal;
        }

        /// <summary>
        /// Gets or sets the plane's anchor in entity A's local space.
        /// </summary>
        public System.Numerics.Vector3 LocalPlaneAnchor
        {
            get { return localPlaneAnchor; }
            set
            {
                localPlaneAnchor = value;
                Matrix3x3.Transform(ref localPlaneAnchor, ref connectionA.orientationMatrix, out worldPlaneAnchor);
                Vector3Ex.Add(ref connectionA.position, ref worldPlaneAnchor, out worldPlaneAnchor);
            }
        }

        /// <summary>
        /// Gets or sets the plane's normal in entity A's local space.
        /// </summary>
        public System.Numerics.Vector3 LocalPlaneNormal
        {
            get { return localPlaneNormal; }
            set
            {
                localPlaneNormal = System.Numerics.Vector3.Normalize(value);
                Matrix3x3.Transform(ref localPlaneNormal, ref connectionA.orientationMatrix, out worldPlaneNormal);
            }
        }

        /// <summary>
        /// Gets or sets the point anchor in entity B's local space.
        /// </summary>
        public System.Numerics.Vector3 LocalPointAnchor
        {
            get { return localPointAnchor; }
            set
            {
                localPointAnchor = value;
                Matrix3x3.Transform(ref localPointAnchor, ref connectionB.orientationMatrix, out worldPointAnchor);
                Vector3Ex.Add(ref worldPointAnchor, ref connectionB.position, out worldPointAnchor);
            }
        }

        /// <summary>
        /// Gets the offset from A to the connection point between the entities.
        /// </summary>
        public System.Numerics.Vector3 OffsetA
        {
            get { return rA; }
        }

        /// <summary>
        /// Gets the offset from B to the connection point between the entities.
        /// </summary>
        public System.Numerics.Vector3 OffsetB
        {
            get { return rB; }
        }

        /// <summary>
        /// Gets or sets the plane anchor in world space.
        /// </summary>
        public System.Numerics.Vector3 PlaneAnchor
        {
            get { return worldPlaneAnchor; }
            set
            {
                worldPlaneAnchor = value;
                localPlaneAnchor = value - connectionA.position;
                Matrix3x3.TransformTranspose(ref localPlaneAnchor, ref connectionA.orientationMatrix, out localPlaneAnchor);

            }
        }

        /// <summary>
        /// Gets or sets the plane's normal in world space.
        /// </summary>
        public System.Numerics.Vector3 PlaneNormal
        {
            get { return worldPlaneNormal; }
            set
            {
                worldPlaneNormal = System.Numerics.Vector3.Normalize(value);
                Matrix3x3.TransformTranspose(ref worldPlaneNormal, ref connectionA.orientationMatrix, out localPlaneNormal);
            }
        }

        /// <summary>
        /// Gets or sets the point anchor in world space.
        /// </summary>
        public System.Numerics.Vector3 PointAnchor
        {
            get { return worldPointAnchor; }
            set
            {
                worldPointAnchor = value;
                localPointAnchor = value - connectionB.position;
                Matrix3x3.TransformTranspose(ref localPointAnchor, ref connectionB.orientationMatrix, out localPointAnchor);

            }
        }

        #region I1DImpulseConstraintWithError Members

        /// <summary>
        /// Gets the current relative velocity between the connected entities with respect to the constraint.
        /// </summary>
        public float RelativeVelocity
        {
            get
            {
                System.Numerics.Vector3 dv;
                System.Numerics.Vector3 aVel, bVel;
                Vector3Ex.Cross(ref connectionA.angularVelocity, ref rA, out aVel);
                Vector3Ex.Add(ref aVel, ref connectionA.linearVelocity, out aVel);
                Vector3Ex.Cross(ref connectionB.angularVelocity, ref rB, out bVel);
                Vector3Ex.Add(ref bVel, ref connectionB.linearVelocity, out bVel);
                Vector3Ex.Subtract(ref aVel, ref bVel, out dv);
                float velocityDifference;
                Vector3Ex.Dot(ref dv, ref worldPlaneNormal, out velocityDifference);
                return velocityDifference;
            }
        }


        /// <summary>
        /// Gets the total impulse applied by this constraint.
        /// </summary>
        public float TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        /// <summary>
        /// Gets the current constraint error.
        /// </summary>
        public float Error
        {
            get { return error; }
        }

        #endregion

        #region I1DJacobianConstraint Members

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the first connected entity.</param>
        public void GetLinearJacobianA(out System.Numerics.Vector3 jacobian)
        {
            jacobian = worldPlaneNormal;
        }

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the second connected entity.</param>
        public void GetLinearJacobianB(out System.Numerics.Vector3 jacobian)
        {
            jacobian = -worldPlaneNormal;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the first connected entity.</param>
        public void GetAngularJacobianA(out System.Numerics.Vector3 jacobian)
        {
            jacobian = rAcrossN;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the second connected entity.</param>
        public void GetAngularJacobianB(out System.Numerics.Vector3 jacobian)
        {
            jacobian = -rBcrossN;
        }

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        public void GetMassMatrix(out float outputMassMatrix)
        {
            outputMassMatrix = -negativeEffectiveMass;
        }

        #endregion

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {
            //TODO: This could technically be faster.
            //Form the jacobian explicitly.
            //Cross cross add add subtract dot
            //vs
            //dot dot dot dot and then scalar adds
            System.Numerics.Vector3 dv;
            System.Numerics.Vector3 aVel, bVel;
            Vector3Ex.Cross(ref connectionA.angularVelocity, ref rA, out aVel);
            Vector3Ex.Add(ref aVel, ref connectionA.linearVelocity, out aVel);
            Vector3Ex.Cross(ref connectionB.angularVelocity, ref rB, out bVel);
            Vector3Ex.Add(ref bVel, ref connectionB.linearVelocity, out bVel);
            Vector3Ex.Subtract(ref aVel, ref bVel, out dv);
            float velocityDifference;
            Vector3Ex.Dot(ref dv, ref worldPlaneNormal, out velocityDifference);
            //if(velocityDifference > 0)
            //    Debug.WriteLine("Velocity difference: " + velocityDifference);
            //Debug.WriteLine("softness velocity: " + softness * accumulatedImpulse);
            float lambda = negativeEffectiveMass * (velocityDifference + biasVelocity + softness * accumulatedImpulse);
            accumulatedImpulse += lambda;

            System.Numerics.Vector3 impulse;
            System.Numerics.Vector3 torque;
            Vector3Ex.Multiply(ref worldPlaneNormal, lambda, out impulse);
            if (connectionA.isDynamic)
            {
                Vector3Ex.Multiply(ref rAcrossN, lambda, out torque);
                connectionA.ApplyLinearImpulse(ref impulse);
                connectionA.ApplyAngularImpulse(ref torque);
            }
            if (connectionB.isDynamic)
            {
                Vector3Ex.Negate(ref impulse, out impulse);
                Vector3Ex.Multiply(ref rBcrossN, lambda, out torque);
                connectionB.ApplyLinearImpulse(ref impulse);
                connectionB.ApplyAngularImpulse(ref torque);
            }

            return lambda;
        }

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {
            Matrix3x3.Transform(ref localPlaneNormal, ref connectionA.orientationMatrix, out worldPlaneNormal);
            Matrix3x3.Transform(ref localPlaneAnchor, ref connectionA.orientationMatrix, out worldPlaneAnchor);
            Vector3Ex.Add(ref worldPlaneAnchor, ref connectionA.position, out worldPlaneAnchor);

            Matrix3x3.Transform(ref localPointAnchor, ref connectionB.orientationMatrix, out rB);
            Vector3Ex.Add(ref rB, ref connectionB.position, out worldPointAnchor);

            //Find rA and rB.
            //So find the closest point on the plane to worldPointAnchor.
            float pointDistance, planeDistance;
            Vector3Ex.Dot(ref worldPointAnchor, ref worldPlaneNormal, out pointDistance);
            Vector3Ex.Dot(ref worldPlaneAnchor, ref worldPlaneNormal, out planeDistance);
            float distanceChange = planeDistance - pointDistance;
            System.Numerics.Vector3 closestPointOnPlane;
            Vector3Ex.Multiply(ref worldPlaneNormal, distanceChange, out closestPointOnPlane);
            Vector3Ex.Add(ref closestPointOnPlane, ref worldPointAnchor, out closestPointOnPlane);

            Vector3Ex.Subtract(ref closestPointOnPlane, ref connectionA.position, out rA);

            Vector3Ex.Cross(ref rA, ref worldPlaneNormal, out rAcrossN);
            Vector3Ex.Cross(ref rB, ref worldPlaneNormal, out rBcrossN);
            Vector3Ex.Negate(ref rBcrossN, out rBcrossN);

            System.Numerics.Vector3 offset;
            Vector3Ex.Subtract(ref worldPointAnchor, ref closestPointOnPlane, out offset);
            Vector3Ex.Dot(ref offset, ref worldPlaneNormal, out error);
            float errorReduction;
            springSettings.ComputeErrorReductionAndSoftness(dt, 1 / dt, out errorReduction, out softness);
            biasVelocity = MathHelper.Clamp(-errorReduction * error, -maxCorrectiveVelocity, maxCorrectiveVelocity);

            if (connectionA.IsDynamic && connectionB.IsDynamic)
            {
                System.Numerics.Vector3 IrACrossN, IrBCrossN;
                Matrix3x3.Transform(ref rAcrossN, ref connectionA.inertiaTensorInverse, out IrACrossN);
                Matrix3x3.Transform(ref rBcrossN, ref connectionB.inertiaTensorInverse, out IrBCrossN);
                float angularA, angularB;
                Vector3Ex.Dot(ref rAcrossN, ref IrACrossN, out angularA);
                Vector3Ex.Dot(ref rBcrossN, ref IrBCrossN, out angularB);
                negativeEffectiveMass = connectionA.inverseMass + connectionB.inverseMass + angularA + angularB;
                negativeEffectiveMass = -1 / (negativeEffectiveMass + softness);
            }
            else if (connectionA.IsDynamic && !connectionB.IsDynamic)
            {
                System.Numerics.Vector3 IrACrossN;
                Matrix3x3.Transform(ref rAcrossN, ref connectionA.inertiaTensorInverse, out IrACrossN);
                float angularA;
                Vector3Ex.Dot(ref rAcrossN, ref IrACrossN, out angularA);
                negativeEffectiveMass = connectionA.inverseMass + angularA;
                negativeEffectiveMass = -1 / (negativeEffectiveMass + softness);
            }
            else if (!connectionA.IsDynamic && connectionB.IsDynamic)
            {
                System.Numerics.Vector3 IrBCrossN;
                Matrix3x3.Transform(ref rBcrossN, ref connectionB.inertiaTensorInverse, out IrBCrossN);
                float angularB;
                Vector3Ex.Dot(ref rBcrossN, ref IrBCrossN, out angularB);
                negativeEffectiveMass = connectionB.inverseMass + angularB;
                negativeEffectiveMass = -1 / (negativeEffectiveMass + softness);
            }
            else
                negativeEffectiveMass = 0;


        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //Warm Starting
            System.Numerics.Vector3 impulse;
            System.Numerics.Vector3 torque;
            Vector3Ex.Multiply(ref worldPlaneNormal, accumulatedImpulse, out impulse);
            if (connectionA.isDynamic)
            {
                Vector3Ex.Multiply(ref rAcrossN, accumulatedImpulse, out torque);
                connectionA.ApplyLinearImpulse(ref impulse);
                connectionA.ApplyAngularImpulse(ref torque);
            }
            if (connectionB.isDynamic)
            {
                Vector3Ex.Negate(ref impulse, out impulse);
                Vector3Ex.Multiply(ref rBcrossN, accumulatedImpulse, out torque);
                connectionB.ApplyLinearImpulse(ref impulse);
                connectionB.ApplyAngularImpulse(ref torque);
            }
        }
    }
}