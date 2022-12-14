using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System.Collections.Generic;

namespace GameClient.Tools
{
    public class Physics : StartupScript
    {
        private static Physics instance;
        public static Physics Instance
        {
            get { return instance; }
            private set { instance ??= value; }
        }

        private static readonly Vector3 SWEEP_EPSILON = new(0, -.001f, 0);

        [DataMemberIgnore] public Simulation simulation;

        public override void Start()
        {
            Instance = this;
            simulation = Instance.GetSimulation();
        }

        public static bool CapsuleCast(Vector3 start, Vector3 end, float radius, Vector3 direction, out HitResult hitInfo, float maxDistance, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); hitInfo = new(); return false; }

            CapsuleColliderShape shape = new(false, radius, Vector3.Distance(start, end), ShapeOrientation.UpY);
            var pos = (start + end) * .5f; // capsule center
            var pos1 = Matrix.Translation(pos);
            var pos2 = Matrix.Translation(Vector3.Add(pos, Vector3.Multiply(direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
            return hitInfo.Succeeded;
        }

        public static List<HitResult> OverlapSphere(Vector3 position, float radius, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); return new List<HitResult>(); }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(position);
            var pos2 = Matrix.Translation(Vector3.Add(position, SWEEP_EPSILON));
            List<HitResult> hitInfo = new();
            simulation.ShapeSweepPenetrating(shape, pos1, pos2, hitInfo, filterGroups, layerMask);
            //Tools.LogInfo($"Sphere overlap {pos1.TranslationVector} : hit '{hitInfo.Count > 0}'");
            return hitInfo;
        }

        public static bool SphereCast(Ray ray, float radius, float maxDistance, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            return simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask).Succeeded;
        }

        public static void SphereCast(Ray ray, float radius, out HitResult hitInfo, float maxDistance, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); hitInfo = new(); return; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out HitResult hitInfo, float maxDistance, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); hitInfo = new(); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.Translation(Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)));
            hitInfo = simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask);
            //Tools.LogInfo($"Sphere sweep from {pos1.TranslationVector} to {pos2.TranslationVector} : hit '{hitInfo.Succeeded}'");
            return hitInfo.Succeeded;
        }

        public static bool CheckSphere(Vector3 origin, float radius, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); return false; }

            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.Translation(Vector3.Add(origin, SWEEP_EPSILON));
            return simulation.ShapeSweep(shape, pos1, pos2, filterGroups, layerMask).Succeeded;
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out HitResult hitInfo, float maxDistance, 
            CollisionFilterGroups filterGroups = CollisionFilterGroups.DefaultFilter, 
            CollisionFilterGroupFlags layerMask = CollisionFilterGroupFlags.DefaultFilter)
        {
            var simulation = Instance.GetSimulation();
            if (simulation == null) { Tools.LogError("Simulation is null!"); hitInfo = new(); return false; }

            hitInfo = simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), filterGroups, layerMask);
            return hitInfo.Succeeded;
        }
    }
}
