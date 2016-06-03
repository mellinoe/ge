using System;
using BEPUphysics.Entities;
 
using BEPUutilities;

namespace BEPUphysics.Constraints.TwoEntity.JointLimits
{
    /// <summary>
    /// Constrains the distance along an axis between anchor points attached to two entities.
    /// </summary>
    public class LinearAxisLimit : JointLimit, I1DImpulseConstraintWithError, I1DJacobianConstraint
    {
        private float accumulatedImpulse;
        private float biasVelocity;
        private System.Numerics.Vector3 jAngularA, jAngularB;
        private System.Numerics.Vector3 jLinearA, jLinearB;
        private System.Numerics.Vector3 localAnchorA;
        private System.Numerics.Vector3 localAnchorB;
        private float massMatrix;
        private float error;
        private System.Numerics.Vector3 localAxis;
        private float maximum;
        private float minimum;
        private System.Numerics.Vector3 worldAxis;
        private System.Numerics.Vector3 rA; //Jacobian entry for entity A.
        private float unadjustedError;
        private System.Numerics.Vector3 worldAnchorA;
        private System.Numerics.Vector3 worldAnchorB;
        private System.Numerics.Vector3 worldOffsetA, worldOffsetB;

        /// <summary>
        /// Constructs a constraint which tries to keep anchors on two entities within a certain distance of each other along an axis.
        /// To finish the initialization, specify the connections (ConnectionA and ConnectionB) 
        /// as well as the AnchorA, AnchorB, and Axis (or their entity-local versions),
        /// and the Minimum and Maximum.
        /// This constructor sets the constraint's IsActive property to false by default.
        /// </summary>
        public LinearAxisLimit()
        {
            IsActive = false;
        }

        /// <summary>
        /// Constructs a constraint which tries to keep anchors on two entities within a certain distance of each other along an axis.
        /// </summary>
        /// <param name="connectionA">First connection of the pair.</param>
        /// <param name="connectionB">Second connection of the pair.</param>
        /// <param name="anchorA">World space point to attach to connection A that will be constrained.</param>
        /// <param name="anchorB">World space point to attach to connection B that will be constrained.</param>
        /// <param name="axis">Limited axis in world space to attach to connection A.</param>
        /// <param name="minimum">Minimum allowed position along the axis.</param>
        /// <param name="maximum">Maximum allowed position along the axis.</param>
        public LinearAxisLimit(Entity connectionA, Entity connectionB, System.Numerics.Vector3 anchorA, System.Numerics.Vector3 anchorB, System.Numerics.Vector3 axis, float minimum, float maximum)
        {
            ConnectionA = connectionA;
            ConnectionB = connectionB;
            AnchorA = anchorA;
            AnchorB = anchorB;
            Axis = axis;
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// Gets or sets the anchor point attached to entity A in world space.
        /// </summary>
        public System.Numerics.Vector3 AnchorA
        {
            get { return worldAnchorA; }
            set
            {
                worldAnchorA = value;
                worldOffsetA = worldAnchorA - connectionA.position;
                Matrix3x3.TransformTranspose(ref worldOffsetA, ref connectionA.orientationMatrix, out localAnchorA);
            }
        }

        /// <summary>
        /// Gets or sets the anchor point attached to entity A in world space.
        /// </summary>
        public System.Numerics.Vector3 AnchorB
        {
            get { return worldAnchorB; }
            set
            {
                worldAnchorB = value;
                worldOffsetB = worldAnchorB - connectionB.position;
                Matrix3x3.TransformTranspose(ref worldOffsetB, ref connectionB.orientationMatrix, out localAnchorB);
            }
        }

        /// <summary>
        /// Gets or sets the limited axis in world space.
        /// </summary>
        public System.Numerics.Vector3 Axis
        {
            get { return worldAxis; }
            set
            {
                worldAxis = System.Numerics.Vector3.Normalize(value);
                Matrix3x3.TransformTranspose(ref worldAxis, ref connectionA.orientationMatrix, out localAxis);
            }
        }

        /// <summary>
        /// Gets or sets the limited axis in the local space of connection A.
        /// </summary>
        public System.Numerics.Vector3 LocalAxis
        {
            get { return localAxis; }
            set
            {
                localAxis = System.Numerics.Vector3.Normalize(value);
                Matrix3x3.Transform(ref localAxis, ref connectionA.orientationMatrix, out worldAxis);
            }
        }

        /// <summary>
        /// Gets or sets the offset from the first entity's center of mass to the anchor point in its local space.
        /// </summary>
        public System.Numerics.Vector3 LocalOffsetA
        {
            get { return localAnchorA; }
            set
            {
                localAnchorA = value;
                Matrix3x3.Transform(ref localAnchorA, ref connectionA.orientationMatrix, out worldOffsetA);
                worldAnchorA = connectionA.position + worldOffsetA;
            }
        }

        /// <summary>
        /// Gets or sets the offset from the second entity's center of mass to the anchor point in its local space.
        /// </summary>
        public System.Numerics.Vector3 LocalOffsetB
        {
            get { return localAnchorB; }
            set
            {
                localAnchorB = value;
                Matrix3x3.Transform(ref localAnchorB, ref connectionB.orientationMatrix, out worldOffsetB);
                worldAnchorB = connectionB.position + worldOffsetB;
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed distance along the axis.
        /// </summary>
        public float Maximum
        {
            get { return maximum; }
            set
            {
                maximum = value;
                minimum = MathHelper.Min(minimum, maximum);
            }
        }

        /// <summary>
        /// Gets or sets the minimum allowed distance along the axis.
        /// </summary>
        public float Minimum
        {
            get { return minimum; }
            set
            {
                minimum = value;
                maximum = MathHelper.Max(minimum, maximum);
            }
        }

        /// <summary>
        /// Gets or sets the offset from the first entity's center of mass to the anchor point in world space.
        /// </summary>
        public System.Numerics.Vector3 OffsetA
        {
            get { return worldOffsetA; }
            set
            {
                worldOffsetA = value;
                worldAnchorA = connectionA.position + worldOffsetA;
                Matrix3x3.TransformTranspose(ref worldOffsetA, ref connectionA.orientationMatrix, out localAnchorA);
            }
        }

        /// <summary>
        /// Gets or sets the offset from the second entity's center of mass to the anchor point in world space.
        /// </summary>
        public System.Numerics.Vector3 OffsetB
        {
            get { return worldOffsetB; }
            set
            {
                worldOffsetB = value;
                worldAnchorB = connectionB.position + worldOffsetB;
                Matrix3x3.TransformTranspose(ref worldOffsetB, ref connectionB.orientationMatrix, out localAnchorB);
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
                if (isLimitActive)
                {
                    float lambda, dot;
                    Vector3Ex.Dot(ref jLinearA, ref connectionA.linearVelocity, out lambda);
                    Vector3Ex.Dot(ref jAngularA, ref connectionA.angularVelocity, out dot);
                    lambda += dot;
                    Vector3Ex.Dot(ref jLinearB, ref connectionB.linearVelocity, out dot);
                    lambda += dot;
                    Vector3Ex.Dot(ref jAngularB, ref connectionB.angularVelocity, out dot);
                    lambda += dot;
                    return lambda;
                }
                return 0;
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

        //Jacobians

        #region I1DJacobianConstraint Members

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the first connected entity.</param>
        public void GetLinearJacobianA(out System.Numerics.Vector3 jacobian)
        {
            jacobian = jLinearA;
        }

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the second connected entity.</param>
        public void GetLinearJacobianB(out System.Numerics.Vector3 jacobian)
        {
            jacobian = jLinearB;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the first connected entity.</param>
        public void GetAngularJacobianA(out System.Numerics.Vector3 jacobian)
        {
            jacobian = jAngularA;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the second connected entity.</param>
        public void GetAngularJacobianB(out System.Numerics.Vector3 jacobian)
        {
            jacobian = jAngularB;
        }

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        public void GetMassMatrix(out float outputMassMatrix)
        {
            outputMassMatrix = massMatrix;
        }

        #endregion

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {
            //Compute the current relative velocity.
            float lambda, dot;
            Vector3Ex.Dot(ref jLinearA, ref connectionA.linearVelocity, out lambda);
            Vector3Ex.Dot(ref jAngularA, ref connectionA.angularVelocity, out dot);
            lambda += dot;
            Vector3Ex.Dot(ref jLinearB, ref connectionB.linearVelocity, out dot);
            lambda += dot;
            Vector3Ex.Dot(ref jAngularB, ref connectionB.angularVelocity, out dot);
            lambda += dot;

            //Add in the constraint space bias velocity
            lambda = -lambda + biasVelocity - softness * accumulatedImpulse;

            //Transform to an impulse
            lambda *= massMatrix;

            //Clamp accumulated impulse (can't go negative)
            float previousAccumulatedImpulse = accumulatedImpulse;
            if (unadjustedError < 0)
                accumulatedImpulse = MathHelper.Min(accumulatedImpulse + lambda, 0);
            else
                accumulatedImpulse = MathHelper.Max(accumulatedImpulse + lambda, 0);
            lambda = accumulatedImpulse - previousAccumulatedImpulse;

            //Apply the impulse
            System.Numerics.Vector3 impulse;
            if (connectionA.isDynamic)
            {
                Vector3Ex.Multiply(ref jLinearA, lambda, out impulse);
                connectionA.ApplyLinearImpulse(ref impulse);
                Vector3Ex.Multiply(ref jAngularA, lambda, out impulse);
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3Ex.Multiply(ref jLinearB, lambda, out impulse);
                connectionB.ApplyLinearImpulse(ref impulse);
                Vector3Ex.Multiply(ref jAngularB, lambda, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }

            return (Math.Abs(lambda));
        }

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {
            //Compute the 'pre'-jacobians
            Matrix3x3.Transform(ref localAnchorA, ref connectionA.orientationMatrix, out worldOffsetA);
            Matrix3x3.Transform(ref localAnchorB, ref connectionB.orientationMatrix, out worldOffsetB);
            Vector3Ex.Add(ref worldOffsetA, ref connectionA.position, out worldAnchorA);
            Vector3Ex.Add(ref worldOffsetB, ref connectionB.position, out worldAnchorB);
            Vector3Ex.Subtract(ref worldAnchorB, ref connectionA.position, out rA);
            Matrix3x3.Transform(ref localAxis, ref connectionA.orientationMatrix, out worldAxis);

            //Compute error
#if !WINDOWS
            System.Numerics.Vector3 separation = new System.Numerics.Vector3();
#else
            System.Numerics.Vector3 separation;
#endif
            separation.X = worldAnchorB.X - worldAnchorA.X;
            separation.Y = worldAnchorB.Y - worldAnchorA.Y;
            separation.Z = worldAnchorB.Z - worldAnchorA.Z;

            Vector3Ex.Dot(ref separation, ref worldAxis, out unadjustedError);

            //Compute error
            if (unadjustedError < minimum)
                unadjustedError = minimum - unadjustedError;
            else if (unadjustedError > maximum)
                unadjustedError = maximum - unadjustedError;
            else
            {
                unadjustedError = 0;
                isActiveInSolver = false;
                accumulatedImpulse = 0;
                isLimitActive = false;
                return;
            }
            isLimitActive = true;

            unadjustedError = -unadjustedError;
            //Adjust Error
            if (unadjustedError > 0)
                error = MathHelper.Max(0, unadjustedError - margin);
            else if (unadjustedError < 0)
                error = MathHelper.Min(0, unadjustedError + margin);

            //Compute jacobians
            jLinearA = worldAxis;
            jLinearB.X = -jLinearA.X;
            jLinearB.Y = -jLinearA.Y;
            jLinearB.Z = -jLinearA.Z;
            Vector3Ex.Cross(ref rA, ref jLinearA, out jAngularA);
            Vector3Ex.Cross(ref worldOffsetB, ref jLinearB, out jAngularB);

            //Compute bias
            float errorReductionParameter;
            springSettings.ComputeErrorReductionAndSoftness(dt, 1 / dt, out errorReductionParameter, out softness);

            biasVelocity = MathHelper.Clamp(errorReductionParameter * error, -maxCorrectiveVelocity, maxCorrectiveVelocity);
            if (bounciness > 0)
            {
                //Compute currently relative velocity for bounciness.
                float relativeVelocity, dot;
                Vector3Ex.Dot(ref jLinearA, ref connectionA.linearVelocity, out relativeVelocity);
                Vector3Ex.Dot(ref jAngularA, ref connectionA.angularVelocity, out dot);
                relativeVelocity += dot;
                Vector3Ex.Dot(ref jLinearB, ref connectionB.linearVelocity, out dot);
                relativeVelocity += dot;
                Vector3Ex.Dot(ref jAngularB, ref connectionB.angularVelocity, out dot);
                relativeVelocity += dot;
                if (unadjustedError > 0 && -relativeVelocity > bounceVelocityThreshold)
                    biasVelocity = Math.Max(biasVelocity, ComputeBounceVelocity(-relativeVelocity));
                else if (unadjustedError < 0 && relativeVelocity > bounceVelocityThreshold)
                    biasVelocity = Math.Min(biasVelocity, -ComputeBounceVelocity(relativeVelocity));
            }


            //compute mass matrix
            float entryA, entryB;
            System.Numerics.Vector3 intermediate;
            if (connectionA.isDynamic)
            {
                Matrix3x3.Transform(ref jAngularA, ref connectionA.inertiaTensorInverse, out intermediate);
                Vector3Ex.Dot(ref intermediate, ref jAngularA, out entryA);
                entryA += connectionA.inverseMass;
            }
            else
                entryA = 0;
            if (connectionB.isDynamic)
            {
                Matrix3x3.Transform(ref jAngularB, ref connectionB.inertiaTensorInverse, out intermediate);
                Vector3Ex.Dot(ref intermediate, ref jAngularB, out entryB);
                entryB += connectionB.inverseMass;
            }
            else
                entryB = 0;
            massMatrix = 1 / (entryA + entryB + softness);


            
        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //Warm starting
            System.Numerics.Vector3 impulse;
            if (connectionA.isDynamic)
            {
                Vector3Ex.Multiply(ref jLinearA, accumulatedImpulse, out impulse);
                connectionA.ApplyLinearImpulse(ref impulse);
                Vector3Ex.Multiply(ref jAngularA, accumulatedImpulse, out impulse);
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3Ex.Multiply(ref jLinearB, accumulatedImpulse, out impulse);
                connectionB.ApplyLinearImpulse(ref impulse);
                Vector3Ex.Multiply(ref jAngularB, accumulatedImpulse, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }
        } 
    }
}