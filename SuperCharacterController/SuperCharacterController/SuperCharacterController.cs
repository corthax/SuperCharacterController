using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System;
using System.Collections.Generic;

namespace SuperCharacterController
{
    /// <summary>
    /// Custom character controller, to be used by attaching the component to an object
    /// and writing scripts attached to the same object that recieve the "SuperUpdate" message
    /// </summary>
    public class SuperCharacterController : SyncScript
    {
        //private Vector3 debugMove = Vector3.Zero;
        //private bool triggerInteraction = false;

        public bool FixedTimeStep { get; set; } = false;
        public int FixedUpdatesPerSecond { get; set; } = 40;
        public bool ClampToMovingGround { get; set; } = false;
        public bool GroundClamping { get; set; } = false;
        public bool SlopeLimiting { get; set; } = false;
        //public bool debugSpheres { get; set; } = true;

        //[SerializeField]
        //private bool debugGrounding;

        //[SerializeField]
        //private bool debugPushbackMesssages;

        /// <summary>
        /// Describes the Transform of the object we are standing on as well as it's CollisionType, as well
        /// as how far the ground is below us and what angle it is in relation to the controller.
        /// </summary>

        [DataContractIgnore]
        public struct Ground
        {
            public HitResult Hit { get; set; }
            public HitResult NearHit { get; set; }
            public HitResult FarHit { get; set; }
            public HitResult SecondaryHit { get; set; }
            public SuperCollisionType CollisionType { get; set; }
            public Entity MyTransform { get; set; }

            public Ground(HitResult hit, HitResult nearHit, HitResult farHit, HitResult secondaryHit, SuperCollisionType superCollisionType, Entity hitTransform)
            {
                Hit = hit;
                NearHit = nearHit;
                FarHit = farHit;
                SecondaryHit = secondaryHit;
                CollisionType = superCollisionType;
                MyTransform = hitTransform;
            }
        }

        [DataMemberIgnore]
        private readonly CollisionSphere[] spheres =
            new CollisionSphere[3] {
            new CollisionSphere(0.0f, true, false),
            new CollisionSphere(0.5f, false, false),
            new CollisionSphere(1.0f, false, true),
            };

        [DataMemberIgnore] public CollisionFilterGroups Walkable = CollisionFilterGroups.DefaultFilter;

        [DataMemberIgnore] public PhysicsComponent ownCollider;
        [DataMemberIgnore] public float radius = 0.5f;

        [DataMemberIgnore] public float DeltaTime { get; private set; }
        [DataMemberIgnore] public SuperGround CurrentGround { get; private set; }
        [DataMemberIgnore] public CollisionSphere Feet { get; private set; }
        [DataMemberIgnore] public CollisionSphere Head { get; private set; }

        /// <summary>
        /// Total height of the controller from the bottom of the feet to the top of the head
        /// </summary>
        [DataMemberIgnore] public float Height => Vector3.Distance(SpherePosition(Head), SpherePosition(Feet)) + radius * 2;

        [DataMemberIgnore] public Vector3 Up { get { return Entity.Transform.WorldMatrix.Up; } }
        [DataMemberIgnore] public Vector3 Down { get { return Entity.Transform.WorldMatrix.Down; } }
        [DataMemberIgnore] public List<SuperCollision> CollisionData { get; private set; } = new();
        [DataMemberIgnore] public Entity CurrentlyClampedTo { get; set; }
        [DataMemberIgnore] public float HeightScale { get; set; } = 1.0f;
        //[DataMemberIgnore] public float RadiusScale { get; set; } = 1.0f;
        [DataMemberIgnore] public bool ManualUpdateOnly { get; set; } = false;

        public delegate void UpdateDelegate();
        public event UpdateDelegate AfterSingleUpdate;

        private Vector3 initialPosition;
        //private Vector3 groundOffset;
        private Vector3 lastGroundPosition;

        private List<PhysicsComponent> ignoredColliders = new();
        private List<IgnoredCollider> ignoredColliderStack = new();

        private const float Tolerance = 0.05f;
        private const float TinyTolerance = 0.01f;
        //private const string TemporaryLayer = "TempCast";
        private const int MaxPushbackIterations = 2;
        private CollisionFilterGroups TemporaryLayerIndex;
        private float fixedDeltaTime;

        private static SuperCollisionType defaultCollisionType;
        private static Vector3 sweepEpsilon = new(0, -.001f, 0);
        private const float CAST_DISTANCE = 1000f;

        /*void OnDrawGizmos()
        {
            if (debugSpheres)
            {
                if (spheres != null)
                {
                    if (HeightScale == 0) HeightScale = 1;

                    foreach (var sphere in spheres)
                    {
                        var color = sphere.isFeet ? Color.Green : (sphere.isHead ? Color.Yellow : Color.Cyan);
                        var _c = new Vector3(color.R, color.G, color.B);
                        var _m = Matrix.Translation((BulletSharp.Math.Vector3)SpherePosition(sphere));
                        DrawSphere(radius, ref _m, ref _c);
                    }
                }
            }
        }*/

        public override void Start()
        {
            Log.Info("SCC start");
            ownCollider = Entity.Get<PhysicsComponent>();
            if (ownCollider != null) IgnoreCollider(ownCollider);

            TemporaryLayerIndex = CollisionFilterGroups.CustomFilter10;
            CurrentlyClampedTo = null;
            fixedDeltaTime = 1.0f / FixedUpdatesPerSecond;

            foreach (var sphere in spheres)
            {
                if (sphere.isFeet)
                    Feet = sphere;

                if (sphere.isHead)
                    Head = sphere;
            }

            if (Feet == null)
                Log.Error("Feet not found on controller!");

            if (Head == null)
                Log.Error("Head not found on controller!");

            defaultCollisionType ??= new Entity("DefaultSuperCollisionType").GetOrCreate<SuperCollisionType>();
            defaultCollisionType.Entity.Scene= Entity.Scene;

            CurrentGround = new SuperGround(Walkable, this/*, triggerInteraction*/);

            //gameObject.SendMessage("SuperStart", SendMessageOptions.DontRequireReceiver);
        }

        public override void Update()
        {
            // If we are using a fixed timestep, ensure we run the main update loop
            // a sufficient number of times based on the Time.deltaTime
            if (ManualUpdateOnly) return;

            if (!FixedTimeStep)
            {
                //Log.Info("SCC update");
                DeltaTime = Math3d.DeltaTime;
                SingleUpdate();
            }
            else
            {
                //Log.Info("SCC fixed update");
                float delta = Math3d.DeltaTime;

                while (delta > fixedDeltaTime)
                {
                    DeltaTime = fixedDeltaTime;

                    SingleUpdate();

                    delta -= fixedDeltaTime;
                }

                if (delta > 0f)
                {
                    DeltaTime = delta;

                    SingleUpdate();
                }
            }
        }

        public void ManualUpdate(float deltaTime)
        {
            DeltaTime = deltaTime;

            SingleUpdate();
        }

        void SingleUpdate()
        {
            // Check if we are clamped to an object implicity or explicity
            bool isClamping = GroundClamping || CurrentlyClampedTo != null;
            Entity clampedTo = CurrentlyClampedTo ?? CurrentGround.Transform;

            if (ClampToMovingGround && isClamping && clampedTo != null && clampedTo.Transform.Position - lastGroundPosition != Vector3.Zero)
                Entity.Transform.Position += clampedTo.Transform.Position - lastGroundPosition;

            initialPosition = Entity.Transform.Position;

            ProbeGround(1);

            //Entity.Transform.Position += debugMove * DeltaTime;

            //gameObject.SendMessage("SuperUpdate", SendMessageOptions.DontRequireReceiver);

            CollisionData.Clear();

            RecursivePushback(0, MaxPushbackIterations);

            ProbeGround(2);

            if (SlopeLimiting) SlopeLimit();

            ProbeGround(3);

            if (GroundClamping) ClampToGround();

            isClamping = GroundClamping || CurrentlyClampedTo != null;
            clampedTo = CurrentlyClampedTo ?? CurrentGround.Transform;

            if (isClamping)
                lastGroundPosition = clampedTo.Transform.Position;

            /*if (debugGrounding)
                CurrentGround.DebugGround(true, true, true, true, true);*/

            AfterSingleUpdate?.Invoke();
        }

        void ProbeGround(int iter)
        {
            PushIgnoredColliders();
            CurrentGround.ProbeGround(SpherePosition(Feet), iter);
            PopIgnoredColliders();
        }

        /// <summary>
        /// Prevents the player from walking up slopes of a larger angle than the object's SlopeLimit.
        /// </summary>
        /// <returns>True if the controller attemped to ascend a too steep slope and had their movement limited</returns>
        bool SlopeLimit()
        {
            Vector3 n = CurrentGround.PrimaryNormal();
            float a = Math3d.Vector3_Angle(n, Up);

            if (a > CurrentGround.MySuperCollisionType.SlopeLimit)
            {
                Vector3 absoluteMoveDirection = Math3d.ProjectVectorOnPlane(n, Entity.Transform.Position - initialPosition);

                // Retrieve a vector pointing down the slope
                Vector3 r = Vector3.Cross(n, Down);
                Vector3 v = Vector3.Cross(r, n);

                float angle = Math3d.Vector3_Angle(absoluteMoveDirection, v);

                if (angle <= 90.0f)
                    return false;

                // Calculate where to place the controller on the slope, or at the bottom, based on the desired movement distance
                Vector3 resolvedPosition = Math3d.ProjectPointOnLine(initialPosition, r, Entity.Transform.Position);
                Vector3 direction = Math3d.ProjectVectorOnPlane(n, resolvedPosition - Entity.Transform.Position);


                // Check if our path to our resolved position is blocked by any colliders
                if (Physics_CapsuleCast(SpherePosition(Feet), SpherePosition(Head), radius, Vector3.Normalize(direction), out HitResult hit, direction.Length(), Walkable))
                {
                    Entity.Transform.Position += Vector3.Normalize(v) * Vector3.Distance(hit.Point, Entity.Transform.Position);
                }
                else
                {
                    Entity.Transform.Position += direction;
                }

                return true;
            }

            return false;
        }

        // unity physics adaptation

        private static bool Physics_CapsuleCast(Vector3 start, Vector3 end, float radius, Vector3 direction, out HitResult hitInfo, float maxDistance, CollisionFilterGroups layerMask)
        {
            CapsuleColliderShape shape = new(false, radius, Vector3.Distance(start, end), ShapeOrientation.UpY);
            var pos = (start + end) * .5f; // capsule center
            var pos1 = Matrix.Translation(pos);
            var pos2 = Matrix.Translation(Vector3.Add(pos, Vector3.Multiply(direction, maxDistance)));
            hitInfo = Math3d.Instance.simulation.ShapeSweep(shape, pos1, pos2, layerMask);
            return hitInfo.Succeeded;
        }

        private static List<HitResult> Physics_OverlapSphere(Vector3 position, float radius, CollisionFilterGroups layerMask)
        {
            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(position);
            var pos2 = Matrix.Translation(Vector3.Add(position, sweepEpsilon));
            List<HitResult> hitInfo = new();
            Math3d.Instance.simulation.ShapeSweepPenetrating(shape, pos1, pos2, hitInfo, layerMask);
            //Math3d.LogInfo($"Sphere overlap {pos1.TranslationVector} : hit '{hitInfo.Count > 0}'");
            return hitInfo;
        }

        private static bool Physics_SphereCast(Ray ray, float radius, float maxDistance, CollisionFilterGroups layerMask)
        {
            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            return Math3d.Instance.simulation.ShapeSweep(shape, pos1, pos2, layerMask).Succeeded;
        }

        private static void Physics_SphereCast(Ray ray, float radius, out HitResult hitInfo, float maxDistance, CollisionFilterGroups layerMask)
        {
            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(ray.Position);
            var pos2 = Matrix.Translation(Vector3.Add(ray.Position, Vector3.Multiply(ray.Direction, maxDistance)));
            hitInfo = Math3d.Instance.simulation.ShapeSweep(shape, pos1, pos2, layerMask);
        }

        private static bool Physics_SphereCast(Vector3 origin, float radius, Vector3 direction, out HitResult hitInfo, float maxDistance, CollisionFilterGroups layerMask)
        {
            SphereColliderShape shape = new(false, radius);
            var pos1 = Matrix.Translation(origin);
            var pos2 = Matrix.Translation(Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)));
            hitInfo = Math3d.Instance.simulation.ShapeSweep(shape, pos1, pos2, layerMask);
            //Math3d.LogInfo($"Sphere sweep from {pos1.TranslationVector} to {pos2.TranslationVector} : hit '{hitInfo.Succeeded}'");
            return hitInfo.Succeeded;
        }

        private static bool Physics_Raycast(Vector3 origin, Vector3 direction, out HitResult hitInfo, float maxDistance, CollisionFilterGroups layerMask)
        {
            hitInfo = Math3d.Instance.simulation.Raycast(origin, Vector3.Add(origin, Vector3.Multiply(direction, maxDistance)), layerMask);
            return hitInfo.Succeeded;
        }
        // ---

        void ClampToGround()
        {
            float d = CurrentGround.Distance();
            Entity.Transform.Position -= Vector3.Multiply(Up, d);
        }

        public void EnableClamping()
        {
            GroundClamping = true;
        }

        public void DisableClamping()
        {
            GroundClamping = false;
        }

        public void EnableSlopeLimit()
        {
            SlopeLimiting = true;
        }

        public void DisableSlopeLimit()
        {
            SlopeLimiting = false;
        }

        public bool IsClamping()
        {
            return GroundClamping;
        }

        /// <summary>
        /// Check if any of the CollisionSpheres are colliding with any walkable objects in the world.
        /// If they are, apply a proper pushback and retrieve the collision data
        /// </summary>
        void RecursivePushback(int depth, int maxDepth)
        {
            PushIgnoredColliders();

            bool contact = false;

            foreach (var sphere in spheres)
            {
                foreach (var hr in Physics_OverlapSphere(SpherePosition(sphere), radius, Walkable))
                {
                    var physicsComponent = hr.Collider;
                    Vector3 position = SpherePosition(sphere);
                    bool contactPointSuccess = SuperCollider.ClosestPointOnSurface(physicsComponent, position, radius, out Vector3 contactPoint);

                    if (!contactPointSuccess)
                    {
                        return;
                    }

                    //Math3d.LogInfo("Pushback!");
                    /*if (debugPushbackMesssages)
                        DebugDraw.DrawMarker(contactPoint, 2.0f, Color.cyan, 0.0f, false);*/

                    var _v = Vector3.Subtract(contactPoint, position);
                    if (_v != Vector3.Zero)
                    {
                        // Cache the collider's layer so that we can cast against it
                        var layer = physicsComponent.CollisionGroup;

                        physicsComponent.CollisionGroup = TemporaryLayerIndex;

                        // Check which side of the normal we are on
                        bool facingNormal = Physics_SphereCast(new Ray(position, Vector3.Normalize(_v)), TinyTolerance, _v.Length() + TinyTolerance, (CollisionFilterGroups)(1 << (int)TemporaryLayerIndex));

                        physicsComponent.CollisionGroup = layer;

                        // Orient and scale our vector based on which side of the normal we are situated
                        if (facingNormal)
                        {
                            if (Vector3.Distance(position, contactPoint) < radius)
                            {
                                _v = Vector3.Multiply(Vector3.Normalize(_v), (radius - _v.Length()) * -1);
                            }
                            else
                            {
                                // A previously resolved collision has had a side effect that moved us outside this collider
                                continue;
                            }
                        }
                        else
                        {
                            _v = Vector3.Multiply(Vector3.Normalize(_v), radius + _v.Length());
                        }

                        contact = true;

                        Entity.Transform.Position += _v;

                        physicsComponent.CollisionGroup = TemporaryLayerIndex;

                        // Retrieve the surface normal of the collided point

                        Physics_SphereCast(new Ray(position + _v, contactPoint - (position + _v)), TinyTolerance, out HitResult normalHit, CAST_DISTANCE, (CollisionFilterGroups)(1 << (int)TemporaryLayerIndex));

                        physicsComponent.CollisionGroup = (CollisionFilterGroups)layer;

                        SuperCollisionType superColType = physicsComponent.Entity.GetOrCreate<SuperCollisionType>();

                        superColType ??= defaultCollisionType;

                        // Our collision affected the collider; add it to the collision data
                        var collision = new SuperCollision()
                        {
                            collisionSphere = sphere,
                            superCollisionType = superColType,
                            gameObject = physicsComponent.Entity,
                            point = contactPoint,
                            normal = normalHit.Normal,
                        };

                        CollisionData.Add(collision);
                    }
                }
            }

            PopIgnoredColliders();

            if (depth < maxDepth && contact)
            {
                RecursivePushback(depth + 1, maxDepth);
            }
        }

        protected struct IgnoredCollider
        {
            public PhysicsComponent collider;
            public CollisionFilterGroups layer;

            public IgnoredCollider(PhysicsComponent collider, CollisionFilterGroups layer)
            {
                this.collider = collider;
                this.layer = layer;
            }
        }

        private void PushIgnoredColliders()
        {
            ignoredColliderStack.Clear();

            for (int i = 0; i < ignoredColliders.Count; i++)
            {
                PhysicsComponent col = ignoredColliders[i];
                ignoredColliderStack.Add(new IgnoredCollider(col, col.CollisionGroup));
                col.CollisionGroup = TemporaryLayerIndex;
            }
        }

        private void PopIgnoredColliders()
        {
            for (int i = 0; i < ignoredColliderStack.Count; i++)
            {
                IgnoredCollider ic = ignoredColliderStack[i];
                ic.collider.CollisionGroup = ic.layer;
            }

            ignoredColliderStack.Clear();
        }

        public Vector3 SpherePosition(CollisionSphere sphere)
        {
            if (sphere.isFeet)
                return Entity.Transform.Position + sphere.offset * Up;
            else
                return Entity.Transform.Position + sphere.offset * Up * HeightScale;
        }

        public bool PointBelowHead(Vector3 point)
        {
            return Math3d.Vector3_Angle(point - SpherePosition(Head), Up) > 89.0f;
        }

        public bool PointAboveFeet(Vector3 point)
        {
            return Math3d.Vector3_Angle(point - SpherePosition(Feet), Down) > 89.0f;
        }

        public void IgnoreCollider(PhysicsComponent col)
        {
            ignoredColliders.Add(col);
        }

        public void RemoveIgnoredCollider(StaticColliderComponent col)
        {
            ignoredColliders.Remove(col);
        }

        public void ClearIgnoredColliders()
        {
            ignoredColliders.Clear();
        }


        public class SuperGround
        {
            public SuperGround(CollisionFilterGroups walkable, SuperCharacterController controller/*, bool triggerInteraction*/)
            {
                this.walkable = walkable;
                this.controller = controller;
                //this.triggerInteraction = triggerInteraction;
            }

            [DataContractIgnore]
            private class GroundHit
            {
                public Vector3 Point { get; private set; }
                public Vector3 Normal { get; private set; }
                public float Distance { get; private set; }

                public GroundHit(Vector3 point, Vector3 normal, float distance)
                {
                    Point = point;
                    Normal = normal;
                    Distance = distance;
                }
            }

            private CollisionFilterGroups walkable;
            private SuperCharacterController controller;
            //private bool triggerInteraction;

            private GroundHit primaryGround;
            private GroundHit nearGround;
            private GroundHit farGround;
            private GroundHit stepGround;
            private GroundHit flushGround;

            [DataMemberIgnore]
            public SuperCollisionType MySuperCollisionType { get; private set; }
            [DataMemberIgnore]
            public Entity Transform { get; private set; }

            private const float groundingUpperBoundAngle = 60.0f;
            private const float groundingMaxPercentFromCenter = 0.85f;
            private const float groundingMinPercentFromcenter = 0.50f;

            /// <summary>
            /// Scan the surface below us for ground. Follow up the initial scan with subsequent scans
            /// designed to test what kind of surface we are standing above and handle different edge cases
            /// </summary>
            /// <param name="origin">Center of the sphere for the initial SphereCast</param>
            ///// <param name="iter">Debug tool to print out which ProbeGround iteration is being run (3 are run each frame for the controller)</param

            public void ProbeGround(Vector3 origin, int _)
            {
                //Math3d.LogInfo("Probe ground...");
                ResetGrounds();

                Vector3 up = controller.Up;
                Vector3 down = controller.Down;

                Vector3 _o = origin + (up * Tolerance);

                // Reduce our radius by Tolerance squared to avoid failing the SphereCast due to clipping with walls
                float smallerRadius = controller.radius - (Tolerance * Tolerance);

                if (Physics_SphereCast(_o, smallerRadius, down, out HitResult hit, CAST_DISTANCE, walkable))
                {
                    //Math3d.LogInfo("Probe ground. Sphere.");
                    var superColType = hit.Collider.Entity.GetOrCreate<SuperCollisionType>();

                    superColType ??= defaultCollisionType;

                    MySuperCollisionType = superColType;
                    Transform = hit.Collider.Entity;

                    // By reducing the initial SphereCast's radius by Tolerance, our casted sphere no longer fits with
                    // our controller's shape. Reconstruct the sphere cast with the proper radius
                    SimulateSphereCast(hit.Normal, out hit, out float distance);

                    primaryGround = new GroundHit(hit.Point, hit.Normal, distance);

                    // If we are standing on a perfectly flat surface, we cannot be either on an edge,
                    // On a slope or stepping off a ledge
                    if (Vector3.Distance(Math3d.ProjectPointOnPlane(controller.Up, controller.Entity.Transform.Position, hit.Point), controller.Entity.Transform.Position) < TinyTolerance)
                    {
                        return;
                    }

                    // As we are standing on an edge, we need to retrieve the normals of the two
                    // faces on either side of the edge and store them in nearHit and farHit

                    Vector3 toCenter = Math3d.ProjectVectorOnPlane(up, Vector3.Normalize(controller.Entity.Transform.Position - hit.Point) * TinyTolerance);

                    Vector3 awayFromCenter = Quaternion.RotationAxis(Vector3.Cross(toCenter, up), -80.0f) * -toCenter;

                    Vector3 nearPoint = hit.Point + toCenter + (up * TinyTolerance);
                    Vector3 farPoint = hit.Point + (awayFromCenter * 3);

                    Physics_Raycast(nearPoint, down, out HitResult nearHit, CAST_DISTANCE, walkable);
                    Physics_Raycast(farPoint, down, out HitResult farHit, CAST_DISTANCE, walkable);

                    nearGround = new GroundHit(nearHit.Point, nearHit.Normal, Vector3.Distance(nearPoint, nearHit.Point));
                    farGround = new GroundHit(farHit.Point, farHit.Normal, Vector3.Distance(farPoint, farHit.Point));

                    // If we are currently standing on ground that should be counted as a wall,
                    // we are likely flush against it on the ground. Retrieve what we are standing on
                    if (Math3d.Vector3_Angle(hit.Normal, up) > superColType.StandAngle)
                    {
                        // Retrieve a vector pointing down the slope
                        Vector3 r = Vector3.Cross(hit.Normal, down);
                        Vector3 v = Vector3.Cross(r, hit.Normal);

                        Vector3 flushOrigin = hit.Point + hit.Normal * TinyTolerance;

                        if (Physics_Raycast(flushOrigin, v, out HitResult flushHit, CAST_DISTANCE, walkable))
                        {

                            if (SimulateSphereCast(flushHit.Normal, out HitResult sphereCastHit, out float distance2))
                            {
                                flushGround = new GroundHit(sphereCastHit.Point, sphereCastHit.Normal, distance2);
                            }
                            else
                            {
                                // Uh oh
                            }
                        }
                    }

                    // If we are currently standing on a ledge then the face nearest the center of the
                    // controller should be steep enough to be counted as a wall. Retrieve the ground
                    // it is connected to at it's base, if there exists any
                    if (Math3d.Vector3_Angle(nearHit.Normal, up) > superColType.StandAngle || Vector3.Distance(nearPoint, nearHit.Point) > Tolerance)
                    {
                        var col = nearHit.Collider.Entity.GetOrCreate<SuperCollisionType>();

                        col ??= defaultCollisionType;

                        // We contacted the wall of the ledge, rather than the landing. Raycast down
                        // the wall to retrieve the proper landing
                        if (Math3d.Vector3_Angle(nearHit.Normal, up) > col.StandAngle)
                        {
                            // Retrieve a vector pointing down the slope
                            Vector3 r = Vector3.Cross(nearHit.Normal, down);
                            Vector3 v = Vector3.Cross(r, nearHit.Normal);

                            if (Physics_Raycast(nearPoint, v, out HitResult stepHit, CAST_DISTANCE, walkable))
                            {
                                stepGround = new GroundHit(stepHit.Point, stepHit.Normal, Vector3.Distance(nearPoint, stepHit.Point));
                            }
                        }
                        else
                        {
                            stepGround = new GroundHit(nearHit.Point, nearHit.Normal, Vector3.Distance(nearPoint, nearHit.Point));
                        }
                    }
                }
                // If the initial SphereCast fails, likely due to the controller clipping a wall,
                // fallback to a raycast simulated to SphereCast data
                else if (Physics_Raycast(_o, down, out hit, CAST_DISTANCE, walkable))
                {
                    //Math3d.LogInfo("Probe ground. Ray.");
                    var superColType = hit.Collider.Entity.GetOrCreate<SuperCollisionType>();

                    superColType ??= defaultCollisionType;

                    MySuperCollisionType = superColType;
                    Transform = hit.Collider.Entity;

                    if (SimulateSphereCast(hit.Normal, out HitResult sphereCastHit, out float distance))
                    {
                        primaryGround = new GroundHit(sphereCastHit.Point, sphereCastHit.Normal, distance);
                    }
                    else
                    {
                        primaryGround = new GroundHit(hit.Point, hit.Normal, distance);
                    }
                }
                else
                {
                    Math3d.LogError("No ground was found below the player!");
                }
            }

            private void ResetGrounds()
            {
                primaryGround = null;
                nearGround = null;
                farGround = null;
                flushGround = null;
                stepGround = null;
            }

            public bool IsGrounded(bool currentlyGrounded, float distance)
            {
                return IsGrounded(currentlyGrounded, distance, out Vector3 _);
            }

            public bool IsGrounded(bool _, float distance, out Vector3 groundNormal)
            {
                groundNormal = Vector3.Zero;

                if (primaryGround == null || primaryGround.Distance > distance)
                {
                    return false;
                }

                // Check if we are flush against a wall
                if (farGround != null && Math3d.Vector3_Angle(farGround.Normal, controller.Up) > MySuperCollisionType.StandAngle)
                {
                    if (flushGround != null && Math3d.Vector3_Angle(flushGround.Normal, controller.Up) < MySuperCollisionType.StandAngle && flushGround.Distance < distance)
                    {
                        groundNormal = flushGround.Normal;
                        return true;
                    }

                    return false;
                }

                // Check if we are at the edge of a ledge, or on a high angle slope
                if (farGround != null && !OnSteadyGround(farGround.Normal, primaryGround.Point))
                {
                    // Check if we are walking onto steadier ground
                    if (nearGround != null && nearGround.Distance < distance && Math3d.Vector3_Angle(nearGround.Normal, controller.Up) < MySuperCollisionType.StandAngle && !OnSteadyGround(nearGround.Normal, nearGround.Point))
                    {
                        groundNormal = nearGround.Normal;
                        return true;
                    }

                    // Check if we are on a step or stair
                    if (stepGround != null && stepGround.Distance < distance && Math3d.Vector3_Angle(stepGround.Normal, controller.Up) < MySuperCollisionType.StandAngle)
                    {
                        groundNormal = stepGround.Normal;
                        return true;
                    }

                    return false;
                }


                if (farGround != null)
                {
                    groundNormal = farGround.Normal;
                }
                else
                {
                    groundNormal = primaryGround.Normal;
                }

                return true;
            }

            private bool OnSteadyGround(Vector3 normal, Vector3 point)
            {
                float angle = Math3d.Vector3_Angle(normal, controller.Up);
                float angleRatio = angle / groundingUpperBoundAngle;
                float distanceRatio = MathUtil.Lerp(groundingMinPercentFromcenter, groundingMaxPercentFromCenter, angleRatio);
                Vector3 p = Math3d.ProjectPointOnPlane(controller.Up, controller.Entity.Transform.Position, point);
                float distanceFromCenter = Vector3.Distance(p, controller.Entity.Transform.Position);

                return distanceFromCenter <= distanceRatio * controller.radius;
            }

            public Vector3 PrimaryNormal()
            {
                return primaryGround.Normal;
            }

            public float Distance()
            {
                return primaryGround.Distance;
            }

            /// <summary>
            /// Provides raycast data based on where a SphereCast would contact the specified normal
            /// Raycasting downwards from a point along the controller's bottom sphere, based on the provided
            /// normal
            /// </summary>
            /// <param name="groundNormal">Normal of a triangle assumed to be directly below the controller</param>
            /// <param name="hit">Simulated SphereCast data</param>
            /// <returns>True if the raycast is successful</returns>
            private bool SimulateSphereCast(Vector3 groundNormal, out HitResult hit, out float distance)
            {
                float groundAngle = Math3d.Vector3_Angle(groundNormal, controller.Up) * (MathF.PI * 2 / 360);
                Vector3 secondaryOrigin = controller.Entity.Transform.Position + controller.Up * Tolerance;

                if (!MathUtil.NearEqual(groundAngle, 0))
                {
                    float horizontal = MathF.Sin(groundAngle) * controller.radius;
                    float vertical = (1.0f - MathF.Cos(groundAngle)) * controller.radius;

                    // Retrieve a vector pointing up the slope
                    Vector3 r2 = Vector3.Cross(groundNormal, controller.Down);
                    Vector3 v2 = -Vector3.Cross(r2, groundNormal);

                    secondaryOrigin += Vector3.Normalize(Math3d.ProjectVectorOnPlane(controller.Up, v2)) * horizontal + controller.Up * vertical;
                }

                if (Physics_Raycast(secondaryOrigin, controller.Down, out hit, CAST_DISTANCE, walkable))
                {
                    // Remove the tolerance from the distance travelled
                    distance = Vector3.Distance(secondaryOrigin, hit.Point);
                    distance -= Tolerance + TinyTolerance;

                    return true;
                }
                else
                {
                    distance = 0;
                    return false;
                }
            }
        }
    }

    public class CollisionSphere
    {
        public float offset;
        public bool isFeet;
        public bool isHead;

        public CollisionSphere(float offset, bool isFeet, bool isHead)
        {
            this.offset = offset;
            this.isFeet = isFeet;
            this.isHead = isHead;
        }
    }

    public struct SuperCollision
    {
        public CollisionSphere collisionSphere;
        public SuperCollisionType superCollisionType;
        public Entity gameObject;
        public Vector3 point;
        public Vector3 normal;
    }
}
