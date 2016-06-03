using System;
using BEPUphysics.Entities;
 
using BEPUutilities;

namespace BEPUphysics.Constraints.TwoEntity.Joints
{
    /// <summary>
    /// Prevents the connected entities from twisting relative to each other.
    /// Acts like the angular part of a universal joint.
    /// </summary>
    public class TwistJoint : Joint, I1DImpulseConstraintWithError, I1DJacobianConstraint
    {
        private System.Numerics.Vector3 aLocalAxisY, aLocalAxisZ;
        private float accumulatedImpulse;
        private System.Numerics.Vector3 bLocalAxisY;
        private float biasVelocity;
        private System.Numerics.Vector3 jacobianA, jacobianB;
        private float error;
        private System.Numerics.Vector3 localAxisA;
        private System.Numerics.Vector3 localAxisB;
        private System.Numerics.Vector3 worldAxisA;
        private System.Numerics.Vector3 worldAxisB;
        private float velocityToImpulse;

        /// <summary>
        /// Constructs a new constraint which prevents the connected entities from twisting relative to each other.
        /// To finish the initialization, specify the connections (ConnectionA and ConnectionB) 
        /// as well as the WorldAxisA and WorldAxisB (or their entity-local versions).
        /// This constructor sets the constraint's IsActive property to false by default.
        /// </summary>
        public TwistJoint()
        {
            IsActive = false;
        }

        /// <summary>
        /// Constructs a new constraint which prevents the connected entities from twisting relative to each other.
        /// </summary>
        /// <param name="connectionA">First connection of the pair.</param>
        /// <param name="connectionB">Second connection of the pair.</param>
        /// <param name="axisA">Twist axis attached to the first connected entity.</param>
        /// <param name="axisB">Twist axis attached to the second connected entity.</param>
        public TwistJoint(Entity connectionA, Entity connectionB, System.Numerics.Vector3 axisA, System.Numerics.Vector3 axisB)
        {
            ConnectionA = connectionA;
            ConnectionB = connectionB;
            WorldAxisA = axisA;
            WorldAxisB = axisB;
        }

        /// <summary>
        /// Gets or sets the axis attached to the first connected entity in its local space.
        /// </summary>
        public System.Numerics.Vector3 LocalAxisA
        {
            get { return localAxisA; }
            set
            {
                localAxisA = System.Numerics.Vector3.Normalize(value);
                Matrix3x3.Transform(ref localAxisA, ref connectionA.orientationMatrix, out worldAxisA);
                Initialize();
            }
        }

        /// <summary>
        /// Gets or sets the axis attached to the first connected entity in its local space.
        /// </summary>
        public System.Numerics.Vector3 LocalAxisB
        {
            get { return localAxisB; }
            set
            {
                localAxisB = System.Numerics.Vector3.Normalize(value);
                Matrix3x3.Transform(ref localAxisB, ref connectionA.orientationMatrix, out worldAxisB);
                Initialize();
            }
        }

        /// <summary>
        /// Gets or sets the axis attached to the first connected entity in world space.
        /// </summary>
        public System.Numerics.Vector3 WorldAxisA
        {
            get { return worldAxisA; }
            set
            {
                worldAxisA = System.Numerics.Vector3.Normalize(value);
                System.Numerics.Quaternion conjugate;
                QuaternionEx.Conjugate(ref connectionA.orientation, out conjugate);
                QuaternionEx.Transform(ref worldAxisA, ref conjugate, out localAxisA);
                Initialize();
            }
        }

        /// <summary>
        /// Gets or sets the axis attached to the first connected entity in world space.
        /// </summary>
        public System.Numerics.Vector3 WorldAxisB
        {
            get { return worldAxisB; }
            set
            {
                worldAxisB = System.Numerics.Vector3.Normalize(value);
                System.Numerics.Quaternion conjugate;
                QuaternionEx.Conjugate(ref connectionA.orientation, out conjugate);
                QuaternionEx.Transform(ref worldAxisB, ref conjugate, out localAxisB);
                Initialize();
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
                float velocityA, velocityB;
                Vector3Ex.Dot(ref connectionA.angularVelocity, ref jacobianA, out velocityA);
                Vector3Ex.Dot(ref connectionB.angularVelocity, ref jacobianB, out velocityB);
                return velocityA + velocityB;
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
            jacobian = Toolbox.ZeroVector;
        }

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the second connected entity.</param>
        public void GetLinearJacobianB(out System.Numerics.Vector3 jacobian)
        {
            jacobian = Toolbox.ZeroVector;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the first connected entity.</param>
        public void GetAngularJacobianA(out System.Numerics.Vector3 jacobian)
        {
            jacobian = jacobianA;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the second connected entity.</param>
        public void GetAngularJacobianB(out System.Numerics.Vector3 jacobian)
        {
            jacobian = jacobianB;
        }

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        public void GetMassMatrix(out float outputMassMatrix)
        {
            outputMassMatrix = velocityToImpulse;
        }

        #endregion

        /// <summary>
        /// Solves for velocity.
        /// </summary>
        public override float SolveIteration()
        {
            float velocityA, velocityB;
            //Find the velocity contribution from each connection
            Vector3Ex.Dot(ref connectionA.angularVelocity, ref jacobianA, out velocityA);
            Vector3Ex.Dot(ref connectionB.angularVelocity, ref jacobianB, out velocityB);
            //Add in the constraint space bias velocity
            float lambda = -(velocityA + velocityB) + biasVelocity - softness * accumulatedImpulse;

            //Transform to an impulse
            lambda *= velocityToImpulse;

            //Accumulate the impulse
            accumulatedImpulse += lambda;

            //Apply the impulse
            System.Numerics.Vector3 impulse;
            if (connectionA.isDynamic)
            {
                Vector3Ex.Multiply(ref jacobianA, lambda, out impulse);
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3Ex.Multiply(ref jacobianB, lambda, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }

            return (Math.Abs(lambda));
        }

        /// <summary>
        /// Do any necessary computations to prepare the constraint for this frame.
        /// </summary>
        /// <param name="dt">Simulation step length.</param>
        public override void Update(float dt)
        {
            System.Numerics.Vector3 aAxisY, aAxisZ;
            System.Numerics.Vector3 bAxisY;
            Matrix3x3.Transform(ref localAxisA, ref connectionA.orientationMatrix, out worldAxisA);
            Matrix3x3.Transform(ref aLocalAxisY, ref connectionA.orientationMatrix, out aAxisY);
            Matrix3x3.Transform(ref aLocalAxisZ, ref connectionA.orientationMatrix, out aAxisZ);
            Matrix3x3.Transform(ref localAxisB, ref connectionB.orientationMatrix, out worldAxisB);
            Matrix3x3.Transform(ref bLocalAxisY, ref connectionB.orientationMatrix, out bAxisY);

            System.Numerics.Quaternion rotation;
            QuaternionEx.GetQuaternionBetweenNormalizedVectors(ref worldAxisB, ref worldAxisA, out rotation);

            //Transform b's 'Y' axis so that it is perpendicular with a's 'X' axis for measurement.
            System.Numerics.Vector3 twistMeasureAxis;
            QuaternionEx.Transform(ref bAxisY, ref rotation, out twistMeasureAxis);

            //By dotting the measurement vector with a 2d plane's axes, we can get a local X and Y value.
            float y, x;
            Vector3Ex.Dot(ref twistMeasureAxis, ref aAxisZ, out y);
            Vector3Ex.Dot(ref twistMeasureAxis, ref aAxisY, out x);
            error = (float) Math.Atan2(y, x);

            //Debug.WriteLine("Angle: " + angle);

            //The nice thing about this approach is that the jacobian entry doesn't flip.
            //Instead, the error can be negative due to the use of Atan2.
            //This is important for limits which have a unique high and low value.

            //Compute the jacobian.
            Vector3Ex.Add(ref worldAxisA, ref worldAxisB, out jacobianB);
            if (jacobianB.LengthSquared() < Toolbox.Epsilon)
            {
                //A nasty singularity can show up if the axes are aligned perfectly.
                //In a 'real' situation, this is impossible, so just ignore it.
                isActiveInSolver = false;
                return;
            }

            jacobianB.Normalize();
            jacobianA.X = -jacobianB.X;
            jacobianA.Y = -jacobianB.Y;
            jacobianA.Z = -jacobianB.Z;

            //****** VELOCITY BIAS ******//
            //Compute the correction velocity.
            float errorReduction;
            springSettings.ComputeErrorReductionAndSoftness(dt, 1 / dt, out errorReduction, out softness);
            biasVelocity = MathHelper.Clamp(-error * errorReduction, -maxCorrectiveVelocity, maxCorrectiveVelocity);

            //****** EFFECTIVE MASS MATRIX ******//
            //Connection A's contribution to the mass matrix
            float entryA;
            System.Numerics.Vector3 transformedAxis;
            if (connectionA.isDynamic)
            {
                Matrix3x3.Transform(ref jacobianA, ref connectionA.inertiaTensorInverse, out transformedAxis);
                Vector3Ex.Dot(ref transformedAxis, ref jacobianA, out entryA);
            }
            else
                entryA = 0;

            //Connection B's contribution to the mass matrix
            float entryB;
            if (connectionB.isDynamic)
            {
                Matrix3x3.Transform(ref jacobianB, ref connectionB.inertiaTensorInverse, out transformedAxis);
                Vector3Ex.Dot(ref transformedAxis, ref jacobianB, out entryB);
            }
            else
                entryB = 0;

            //Compute the inverse mass matrix
            velocityToImpulse = 1 / (softness + entryA + entryB);

            
        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //****** WARM STARTING ******//
            //Apply accumulated impulse
            System.Numerics.Vector3 impulse;
            if (connectionA.isDynamic)
            {
                Vector3Ex.Multiply(ref jacobianA, accumulatedImpulse, out impulse);
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3Ex.Multiply(ref jacobianB, accumulatedImpulse, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }
        }

        private void Initialize()
        {
            //Compute a vector which is perpendicular to the axis.  It'll be added in local space to both connections.
            System.Numerics.Vector3 yAxis;
            Vector3Ex.Cross(ref worldAxisA, ref Toolbox.UpVector, out yAxis);
            float length = yAxis.LengthSquared();
            if (length < Toolbox.Epsilon)
            {
                Vector3Ex.Cross(ref worldAxisA, ref Toolbox.RightVector, out yAxis);
            }
            yAxis.Normalize();

            //Put the axis into the local space of A.
            System.Numerics.Quaternion conjugate;
            QuaternionEx.Conjugate(ref connectionA.orientation, out conjugate);
            QuaternionEx.Transform(ref yAxis, ref conjugate, out aLocalAxisY);

            //Complete A's basis.
            Vector3Ex.Cross(ref localAxisA, ref aLocalAxisY, out aLocalAxisZ);

            //Rotate the axis to B since it could be arbitrarily rotated.
            System.Numerics.Quaternion rotation;
            QuaternionEx.GetQuaternionBetweenNormalizedVectors(ref worldAxisA, ref worldAxisB, out rotation);
            QuaternionEx.Transform(ref yAxis, ref rotation, out yAxis);

            //Put it into local space.
            QuaternionEx.Conjugate(ref connectionB.orientation, out conjugate);
            QuaternionEx.Transform(ref yAxis, ref conjugate, out bLocalAxisY);
        }
    }
}