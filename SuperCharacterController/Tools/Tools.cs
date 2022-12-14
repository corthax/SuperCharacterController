using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Core;

namespace SCC.Tools
{
    public class Tools : StartupScript
    {
        private static Tools instance;
        public static Tools Instance
        {
            get { return instance; }
            private set { instance ??= value; }
        }

        public override void Start()
        {
            Instance = this;
        }

        public static void LogWarning(string msg) => Instance.Log.Warning(msg);
        public static void LogError(string msg) => Instance.Log.Error(msg);
        public static void LogInfo(string msg) => Instance.Log.Info(msg);
    }

    [DataContractIgnore]
    public static class Tool
    {
        public static void LookAt(Entity source, Entity target)
        {

            /*if (target.Transform.Position != source.Transform.Position)
            {
                var viewForward = target.Transform.Position - source.Transform.Position;
                viewForward.Normalize();

                // Now we get the perpendicular projection of the viewForward vector onto the world up vector
                // Uperp = U - ( U.V / V.V ) * V
                //viewUp = Vector3.UnitY - (Math3d.ProjectVectorOnPlane(Vector3.UnitY, viewForward));
                //viewUp.Normalize();

                // Alternatively for getting viewUp you could just use:
                var viewUp = source.Transform.WorldMatrix.Up;
                viewUp.Normalize();

                // Calculate rightVector using Cross Product of viewOut and viewUp
                // this is order is because we use left-handed coordinates
                var viewRight = Vector3.Cross(viewUp, viewForward);

                // set new vectors
                source.Transform.WorldMatrix.Right = new Vector3(viewRight.X, viewRight.Y, viewRight.Z);
                source.Transform.WorldMatrix.Up = new Vector3(viewUp.X, viewUp.Y, viewUp.Z);
                source.Transform.WorldMatrix.Forward = new Vector3(viewForward.X, viewForward.Y, viewForward.Z);
            }*/

            source.Transform.WorldMatrix = Matrix.LookAtLH(
                source.Transform.WorldMatrix.TranslationVector,
                target.Transform.WorldMatrix.TranslationVector,
                Vector3.UnitY);
        }

        public static T[] Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = value;
            }
            return arr;
        }

        public static Vector3 FlattenVector3(Vector3 vector) { return new Vector3(vector.X, 0, vector.Z); }
    }

    [DataContractIgnore]
    public static class Time
    {
        public static float DeltaTime => (float)Tools.Instance.Game.UpdateTime.Elapsed.TotalSeconds;
        public static float TimeSeconds => (float)Tools.Instance.Game.UpdateTime.Total.TotalSeconds;
    }
}
