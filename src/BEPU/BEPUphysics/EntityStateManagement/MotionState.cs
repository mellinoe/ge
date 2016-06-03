 
using System;
using BEPUutilities;

namespace BEPUphysics.EntityStateManagement
{
    ///<summary>
    /// State describing the position, orientation, and velocity of an entity.
    ///</summary>
    public struct MotionState : IEquatable<MotionState>
    {
        ///<summary>
        /// Position of an entity.
        ///</summary>
        public System.Numerics.Vector3 Position;
        ///<summary>
        /// Orientation of an entity.
        ///</summary>
        public System.Numerics.Quaternion Orientation;
        ///<summary>
        /// Orientation matrix of an entity.
        ///</summary>
        public System.Numerics.Matrix4x4 OrientationMatrix
        {
            get
            {
                System.Numerics.Matrix4x4 toReturn;
                Matrix4x4Ex.CreateFromQuaternion(ref Orientation, out toReturn);
                return toReturn;
            }
        }
        ///<summary>
        /// World transform of an entity.
        ///</summary>
        public System.Numerics.Matrix4x4 WorldTransform
        {
            get
            {
                System.Numerics.Matrix4x4 toReturn;
                Matrix4x4Ex.CreateFromQuaternion(ref Orientation, out toReturn);
                toReturn.Translation = Position;
                return toReturn;
            }
        }
        ///<summary>
        /// Linear velocity of an entity.
        ///</summary>
        public System.Numerics.Vector3 LinearVelocity;
        ///<summary>
        /// Angular velocity of an entity.
        ///</summary>
        public System.Numerics.Vector3 AngularVelocity;


        public bool Equals(MotionState other)
        {
            return other.AngularVelocity == AngularVelocity &&
                   other.LinearVelocity == LinearVelocity &&
                   other.Position == Position &&
                   other.Orientation == Orientation;
        }
    }
}
