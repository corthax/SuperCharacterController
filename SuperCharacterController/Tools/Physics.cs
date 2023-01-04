using SCC.Tools;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System.Collections.Generic;

namespace SCC
{
    public class Physics : StartupScript
    {
        private static Physics instance;
        public static Physics Instance
        {
            get { return instance; }
            private set { instance ??= value; }
        }

        [DataMemberIgnore] public Simulation Simulation { get; private set; }

        public void Init()
        {
            Log.Info($"Physics init.");

            Simulation = this.GetSimulation(); // must have any physics component on entity
            while (Simulation == null) { Log.Error("Simulation is null!"); Simulation = this.GetSimulation(); }
        }

        public override void Start()
        {
            Priority = -100;
            Instance ??= this;
            Init();
        }

        #region Unity physics emulation

        // **********************
        // CapsuleCast
        // **********************

        /// <summary>
        /// Sweeps an Y aligned capsule.
        /// </summary>
        /// <param name="capsuleFeet"></param>
        /// <param name="capsuleHead"></param>
        /// <param name="radius"></param>
        /// <param name="direction"></param>
        /// <param name="hitResult"></param>
        /// <param name="hitDistanceCenter"></param>
        /// <param name="maxDistance"></param>
        /// <param name="simulation"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded, hitResult, hitDistanceCenter, hitDistancePoint</returns>
        public static bool CapsuleCast(
            Vector3 capsuleFeet,
            Vector3 capsuleHead,
            float radius,
            Vector3 direction,
            float maxDistance,
            Simulation simulation,
            out HitResult hitResult,
            out float hitDistanceCenter,
            out float hitDistancePoint,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitResult = new(); hitDistanceCenter = hitDistancePoint = float.NaN; return false; }

            var length = capsuleHead.Y - capsuleFeet.Y; // always end > start
            CapsuleColliderShape shape = new(false, radius, length, ShapeOrientation.UpY);

            var pos = capsuleFeet + Vector3.UnitY * (length * .5f); // capsule center
            var pos1 = Matrix.Translation(pos);
            var pos2 = Matrix.Translation(Vector3.Add(pos, Vector3.Multiply(direction, maxDistance)));
            hitResult = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, filterFlags);
            hitDistancePoint = Vector3.Distance(pos, hitResult.Point); // distance to hit point
            hitDistanceCenter = 0; // distance to hit capsule center. wrong
            return hitResult.Succeeded;
        }

        // **********************
        // OverlapSphere
        // **********************

        public static List<HitResult> OverlapSphere(
            Vector3 origin,
            float radius,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            List<HitResult> hitInfo = new();
            if (simulation == null) { Helper.LogError("Simulation is null!"); return hitInfo; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.AffineTransformation(1, Quaternion.RotationY(1.570796326794897f), origin); // 90 degrees

            simulation.ShapeSweepPenetrating(shape, pos1, pos2, hitInfo, filterGroups, layerMask);
            //if (hitInfo.Count > 0) Tools.LogInfo($"Sphere overlap {pos1.TranslationVector} : Count = {hitInfo.Count}");
            return hitInfo;
        }

        public static List<HitResult> OverlapSphere(
            Vector3 origin,
            float radius,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            return OverlapSphere(origin, radius, Instance.Simulation, filterGroups, filterFlags);
        }

        // **********************
        // SphereCast
        // **********************

        public static bool SphereCast(
            Ray ray,
            float radius,
            float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            return simulation.ShapeSweep(shape, pos1, pos2, filterGroups, filterFlags).Succeeded;
        }

        public static bool SphereCast(
            Ray ray,
            float radius,
            float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            return SphereCast(ray, radius, maxDistance, Instance.Simulation, filterGroups, filterFlags);
        }

        public static void SphereCast(
            Ray ray,
            float radius,
            out HitResult hitInfo,
            float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, filterFlags);
        }

        public static void SphereCast(
            Ray ray,
            float radius,
            out HitResult hitInfo,
            float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            SphereCast(ray, radius, out hitInfo, maxDistance, Instance.Simulation, filterGroups, filterFlags);
        }

        /// <summary>
        /// Sweeps a sphere.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="simulation"></param>
        /// <param name="hitResult"></param>
        /// <param name="hitDistanceCenter"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded, hitResult, hitDistanceCenter</returns>
        public static bool SphereCast(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float maxDistance,
            Simulation simulation,
            out HitResult hitResult,
            out float hitDistanceCenter,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitDistanceCenter = float.NaN; hitResult = new(); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.Translation(Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)));
            hitResult = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, filterFlags);
            hitDistanceCenter = Vector3.Distance(origin, hitResult.Point + (radius * hitResult.Normal)); // to hit sphere center
            return hitResult.Succeeded;
        }

        /// <summary>
        /// Sweeps a sphere. Using default simulaion.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="simulation"></param>
        /// <param name="hitResult"></param>
        /// <param name="hitDistanceCenter"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded, hitResult, hitDistanceCenter</returns>
        public static bool SphereCast(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float maxDistance,
            out HitResult hitResult,
            out float hitDistanceCenter,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            return SphereCast(origin, radius, direction, maxDistance, Instance.Simulation, out hitResult, out hitDistanceCenter, filterGroups, filterFlags);
        }

        public static bool SphereCast(
            Vector3 origin,
            float radius,
            Vector3 direction,
            out HitResult hitInfo,
            float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.Translation(Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)));
            hitInfo = Instance.Simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
            return hitInfo.Succeeded;
        }

        // **********************
        // CheckSphere
        // **********************

        /// <summary>
        /// Check if there is anything within the sphere radius.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        /// <param name="simulation"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded</returns>
        public static bool CheckSphere(
            Vector3 origin,
            float radius,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.AffineTransformation(1, Quaternion.RotationY(1.570796326794897f), origin); // 90 degrees
            return simulation.ShapeSweep(shape, pos1, pos2, filterGroups, filterFlags).Succeeded;
        }

        /// <summary>
        /// Check if there is anything within the sphere radius. Using default simulation.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        /// <param name="simulation"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded</returns>
        public static bool CheckSphere(
            Vector3 origin,
            float radius,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            return CheckSphere(origin, radius, Instance.Simulation, filterGroups, filterFlags);
        }

        // **********************
        // Raycast
        // **********************

        /// <summary>
        /// Raycast from <origin> towards <direction (not auto normalized)> by <maxDistance>.
        /// Outputs: simulation success, hit result.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="hitInfo"></param>
        /// <param name="maxDistance"></param>
        /// <param name="simulation"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded, hitResult, hitDistance</returns>
        public static bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            Simulation simulation,
            out HitResult hitInfo,
            out float hitDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitDistance = float.NaN; hitInfo = new(); return false; }

            hitInfo = simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, filterFlags);
            hitDistance = Vector3.Distance(origin, hitInfo.Point);

            return hitInfo.Succeeded;
        }

        /// <summary>
        /// Raycast from <origin> towards <direction (not auto normalized)> by <maxDistance>. Using default simulation.
        /// Outputs: simulation success, hit result.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="hitInfo"></param>
        /// <param name="maxDistance"></param>
        /// <param name="simulation"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>succeeded, hitResult, hitDistance</returns>
        public static bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out HitResult hitInfo,
            out float hitDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            return Raycast(origin, direction, maxDistance, Instance.Simulation, out hitInfo, out hitDistance, filterGroups, filterFlags);
        }

        /// <summary>
        /// Raycast from <origin> towards <direction (not auto normalized)> by <maxDistance>.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="simulation"></param>
        /// <param name="hitDistance"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>hitResult, hitDistance</returns>
        public static HitResult Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            Simulation simulation,
            out float hitDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitDistance = float.NaN; return new HitResult(); }

            var hitResult = simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, filterFlags);
            hitDistance = Vector3.Distance(origin, hitResult.Point);
            return hitResult;
        }

        /// <summary>
        /// Raycast from <origin> towards <direction (not auto normalized)> by <maxDistance>. Using default simulation.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="hitDistance"></param>
        /// <param name="filterGroups"></param>
        /// <param name="filterFlags"></param>
        /// <returns>hitResult, hitDistance</returns>
        public static HitResult Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out float hitDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags filterFlags = CollisionFilterGroupFlags.DefaultFilter)
        {
            return Raycast(origin, direction, maxDistance, Instance.Simulation, out hitDistance, filterGroups, filterFlags);
        }

        #endregion
    }
}
