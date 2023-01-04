using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Core;
using System.Collections.Generic;

namespace SCC.Tools
{
    public class Helper : StartupScript
    {
        private static Helper instance;
        public static Helper Instance
        {
            get { return instance; }
            private set { instance ??= value; }
        }

        [DataMemberIgnore] public float FixedTimeStep { get; private set; }
        public override void Start()
        {
            Priority = -100;
            Instance = this;
            FixedTimeStep = Physics.Instance.Simulation.FixedTimeStep;
        }

        public static void LogWarning(string msg) => Instance.Log.Warning(msg);
        public static void LogError(string msg) => Instance.Log.Error(msg);
        public static void LogInfo(string msg) => Instance.Log.Info(msg);
        public static void LogScreen(string msg, Int2 pos) => Instance.DebugText.Print(msg, pos);
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

        /*public static int WordCount(this string str)
        {
            return str.Split(new char[] { ' ', '.', '?' },
                             StringSplitOptions.RemoveEmptyEntries).Length;
        }*/

        /*public static bool InUse(this ServerRoom room)
        {
            return room.hostPlayerID != 0;
        }*/
    }

    [DataContractIgnore]
    public static class Time
    {
        public static float DeltaTime => (float)Helper.Instance.Game.UpdateTime.Elapsed.TotalSeconds;
        public static float TimeSeconds => (float)Helper.Instance.Game.UpdateTime.Total.TotalSeconds;
        public static float FixedDeltaTime => Helper.Instance.FixedTimeStep;
    }

    public class NAryDictionary<TKey, TValue> :
        Dictionary<TKey, TValue>
    {
    }

    public class NAryDictionary<TKey1, TKey2, TValue> :
        Dictionary<TKey1, NAryDictionary<TKey2, TValue>>
    {
    }

    public class NAryDictionary<TKey1, TKey2, TKey3, TValue> :
        Dictionary<TKey1, NAryDictionary<TKey2, TKey3, TValue>>
    {
    }

    public static class NAryDictionaryExtensions
    {
        public static NAryDictionary<TKey2, TValue> New<TKey1, TKey2, TValue>(this NAryDictionary<TKey1, TKey2, TValue> dictionary)
        {
            return new NAryDictionary<TKey2, TValue>();
        }

        public static NAryDictionary<TKey2, TKey3, TValue> New<TKey1, TKey2, TKey3, TValue>(this NAryDictionary<TKey1, TKey2, TKey3, TValue> dictionary)
        {
            return new NAryDictionary<TKey2, TKey3, TValue>();
        }
    }
}
