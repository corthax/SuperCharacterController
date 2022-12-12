using Stride.Engine;

namespace SuperCharacterController
{
    /// <summary>
    /// Extend this class to add in any further data you want to be able to access
    /// pertaining to an object the controller has collided with
    /// </summary>
    public class SuperCollisionType : StartupScript
    {
        public float StandAngle = 40.0f; // stand on
        public float SlopeLimit = 40.0f; // walk on
    }
}