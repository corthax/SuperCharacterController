using SCC.GameTools;
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

        // CapsuleCast

        // in world space. start = feet, end = head. including offset
        public static bool CapsuleCast(Vector3 start, Vector3 end, float radius, Vector3 direction,
            out HitResult hitInfo,
            out float hitDistance,
            float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); hitDistance = float.NaN; return false; }

            var length = end.Y - start.Y; // always end > start
            CapsuleColliderShape shape = new(false, radius, length, ShapeOrientation.UpY);

            var pos = start + Vector3.UnitY * (length * .5f);
            var pos1 = Matrix.Translation(pos);
            var pos2 = Matrix.Translation(Vector3.Add(pos, Vector3.Multiply(direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
            //hitDistance = Vector3.Distance(pos, hitInfo.Point);
            hitDistance = Vector3.Distance(Tool.FlattenVector3(pos), Tool.FlattenVector3(hitInfo.Point));
            return hitInfo.Succeeded;
        }

        public static bool CapsuleCast(Vector3 start, Vector3 end, float radius, Vector3 direction,
            out HitResult hitInfo,
            out float hitDistance,
            float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); hitDistance = float.NaN; return false; }

            var length = end.Y - start.Y; // always end > start
            CapsuleColliderShape shape = new(false, radius, length, ShapeOrientation.UpY);

            var pos = start + Vector3.UnitY * (length * .5f);
            var pos1 = Matrix.Translation(pos);
            var pos2 = Matrix.Translation(Vector3.Add(pos, Vector3.Multiply(direction, maxDistance)));
            hitInfo = Instance.Simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
            //hitDistance = Vector3.Distance(pos, hitInfo.Point);
            hitDistance = Vector3.Distance(Tool.FlattenVector3(pos), Tool.FlattenVector3(hitInfo.Point));
            return hitInfo.Succeeded;
        }

        // OverlapSphere

        public static List<HitResult> OverlapSphere(Vector3 origin, float radius,
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

        public static List<HitResult> OverlapSphere(Vector3 origin, float radius,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            List<HitResult> hitInfo = new();
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); return hitInfo; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.AffineTransformation(1, Quaternion.RotationY(1.570796326794897f), origin); // 90 degrees

            Instance.Simulation.ShapeSweepPenetrating(shape, pos1, pos2, hitInfo, filterGroups, layerMask);
            //if (hitInfo.Count > 0) Tools.LogInfo($"Sphere overlap {pos1.TranslationVector} : Count = {hitInfo.Count}");
            return hitInfo;
        }

        // SphereCast

        public static bool SphereCast(Ray ray, float radius, float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            return simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask).Succeeded;
        }

        public static bool SphereCast(Ray ray, float radius, float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            return Instance.Simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask).Succeeded;
        }

        public static void SphereCast(Ray ray, float radius, out HitResult hitInfo, float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
        }

        public static void SphereCast(Ray ray, float radius, out HitResult hitInfo, float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            hitInfo = Instance.Simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out HitResult hitInfo, float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.Translation(Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
            return hitInfo.Succeeded;
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out HitResult hitInfo, float maxDistance,
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

        // CheckSphere

        public static bool CheckSphere(Vector3 origin, float radius,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.AffineTransformation(1, Quaternion.RotationY(1.570796326794897f), origin); // 90 degrees
            return simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask).Succeeded;
        }

        // Raycast

        public static bool Raycast(Vector3 origin, Vector3 direction, out HitResult hitInfo, float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return false; }

            hitInfo = simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, layerMask);
            return hitInfo.Succeeded;
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out HitResult hitInfo, float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); hitInfo = new(); return false; }

            hitInfo = Instance.Simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, layerMask);
            return hitInfo.Succeeded;
        }

        public static HitResult Raycast(Vector3 origin, Vector3 direction, float maxDistance,
            Simulation simulation,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (simulation == null) { Helper.LogError("Simulation is null!"); return new HitResult(); }

            return simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, layerMask);
        }

        public static HitResult Raycast(Vector3 origin, Vector3 direction, float maxDistance,
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter,
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            if (Instance.Simulation == null) { Helper.LogError("Simulation is null!"); return new HitResult(); }

            return Instance.Simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, layerMask);
        }
    }
}
