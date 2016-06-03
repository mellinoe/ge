using BEPUutilities;
 

namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Denotes a class that uses a single linear jacobian axis.
    /// </summary>
    public interface I1DJacobianConstraint
    {
        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the first connected entity.</param>
        void GetAngularJacobianA(out System.Numerics.Vector3 jacobian);

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the second connected entity.</param>
        void GetAngularJacobianB(out System.Numerics.Vector3 jacobian);

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the first connected entity.</param>
        void GetLinearJacobianA(out System.Numerics.Vector3 jacobian);

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the second connected entity.</param>
        void GetLinearJacobianB(out System.Numerics.Vector3 jacobian);

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        void GetMassMatrix(out float outputMassMatrix);
    }

    /// <summary>
    /// Denotes a class that uses two linear jacobian axes.
    /// </summary>
    public interface I2DJacobianConstraint
    {
        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobianX">First angular jacobian entry for the first connected entity.</param>
        /// <param name="jacobianY">Second angular jacobian entry for the first connected entity.</param>
        void GetAngularJacobianA(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY);

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobianX">First angular jacobian entry for the second connected entity.</param>
        /// <param name="jacobianY">Second angular jacobian entry for the second connected entity.</param>
        void GetAngularJacobianB(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY);

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobianX">First linear jacobian entry for the first connected entity.</param>
        /// <param name="jacobianY">Second linear jacobian entry for the first connected entity.</param>
        void GetLinearJacobianA(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY);

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobianX">First linear jacobian entry for the second connected entity.</param>
        /// <param name="jacobianY">Second linear jacobian entry for the second connected entity.</param>
        void GetLinearJacobianB(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY);

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="massMatrix">Constraint's mass matrix.</param>
        void GetMassMatrix(out Matrix2x2 massMatrix);
    }

    /// <summary>
    /// Denotes a class that uses three linear jacobian axes.
    /// </summary>
    public interface I3DJacobianConstraint
    {
        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobianX">First angular jacobian entry for the first connected entity.</param>
        /// <param name="jacobianY">Second angular jacobian entry for the first connected entity.</param>
        /// <param name="jacobianZ">Third angular jacobian entry for the first connected entity.</param>
        void GetAngularJacobianA(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY, out System.Numerics.Vector3 jacobianZ);

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobianX">First angular jacobian entry for the second connected entity.</param>
        /// <param name="jacobianY">Second angular jacobian entry for the second connected entity.</param>
        /// <param name="jacobianZ">Third angular jacobian entry for the second connected entity.</param>
        void GetAngularJacobianB(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY, out System.Numerics.Vector3 jacobianZ);

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobianX">First linear jacobian entry for the first connected entity.</param>
        /// <param name="jacobianY">Second linear jacobian entry for the first connected entity.</param>
        /// <param name="jacobianZ">Third linear jacobian entry for the first connected entity.</param>
        void GetLinearJacobianA(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY, out System.Numerics.Vector3 jacobianZ);

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobianX">First linear jacobian entry for the second connected entity.</param>
        /// <param name="jacobianY">Second linear jacobian entry for the second connected entity.</param>
        /// <param name="jacobianZ">Third linear jacobian entry for the second connected entity.</param>
        void GetLinearJacobianB(out System.Numerics.Vector3 jacobianX, out System.Numerics.Vector3 jacobianY, out System.Numerics.Vector3 jacobianZ);

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        void GetMassMatrix(out Matrix3x3 outputMassMatrix);
    }
}