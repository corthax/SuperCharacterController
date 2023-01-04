using SCC.SuperCharacter;
using SCC.Tools;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System;
using System.Collections.Generic;

namespace SCC
{
    /// <summary>
    /// Custom character controller, to be used by attaching the component to an object
    /// and writing scripts attached to the same object that recieve the "SuperUpdate" message
    /// </summary>
    public class SuperCharacterController : SyncScript
    {
        [DataMemberIgnore] public float DeltaTime { get; private set; }
        [DataMemberIgnore] public bool ManualUpdateOnly { get; set; } = true;
        [DataMemberIgnore] private bool FixedTimeStep { get; set; } = false;
        //private float fixedDeltaTime;
        //[DataMemberIgnore] private int FixedUpdatesPerSecond { get; set; } = 40;
        [DataMemberIgnore] private bool ClampToMovingGround { get; set; } = true;
        [DataMemberIgnore] private bool GroundClamping { get; set; } = true;
        [DataMemberIgnore] private bool SlopeLimiting { get; set; } = false;

        [DataMemberIgnore]
        public CollisionFilterGroups WalkableGroups { private set; get; } =
            CollisionFilterGroups.DefaultFilter;
        [DataMemberIgnore]
        public CollisionFilterGroupFlags WalkableFlags { private set; get; } =
            CollisionFilterGroupFlags.DefaultFilter |
            CollisionFilterGroupFlags.StaticFilter;

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

            public Ground(HitResult hit, HitResult nearHit, HitResult farHit, HitResult secondaryHit,
                SuperCollisionType superCollisionType, Entity hitTransform)
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
            new CollisionSphere(0.5f, true, false), // radius
            new CollisionSphere(1.0f, false, false),
            new CollisionSphere(1.5f, false, true),
            };

        [DataMemberIgnore] public PhysicsComponent ownCollider;
        [DataMemberIgnore] public float Radius { get; private set; } = 0.5f;
        [DataMemberIgnore] public float HalfRadius => Radius * .5f;

        //[DataMemberIgnore] public float DeltaTime { get; private set; }
        [DataMemberIgnore] public SuperGround CurrentGround { get; private set; }
        [DataMemberIgnore] public CollisionSphere Feet { get; private set; }
        [DataMemberIgnore] public CollisionSphere Head { get; private set; }

        /// <summary>
        /// Total height of the controller from the bottom of the feet to the top of the head
        /// </summary>
        [DataMemberIgnore] public float Height => Length + Radius * 2;
        /// <summary>
        /// Total lenght of the controller from the center of the feet to the center of the head
        /// </summary>
        [DataMemberIgnore] public float Length => Vector3.Distance(SpherePosition(Head), SpherePosition(Feet));
        [DataMemberIgnore] public float HalfLength => Length * .5f;
        //[DataMemberIgnore] public Vector3 Up { get { return Entity.Transform.WorldMatrix.Up; } }
        //[DataMemberIgnore] public Vector3 Down { get { return Entity.Transform.WorldMatrix.Down; } }
        [DataMemberIgnore] public Vector3 Up { get { return Vector3.UnitY; } }
        [DataMemberIgnore] public Vector3 Down { get { return -Vector3.UnitY; } }
        [DataMemberIgnore] public List<SuperCollision> CollisionData { get; private set; } = new();
        [DataMemberIgnore] public Entity CurrentlyClampedTo { get; set; }
        [DataMemberIgnore] public float HeightScale { get; set; } = 1.0f;
        //[DataMemberIgnore] public float RadiusScale { get; set; } = 1.0f;

        //public delegate void UpdateDelegate();
        //public event UpdateDelegate AfterSingleUpdate;

        private Vector3 initialPosition;
        private Vector3 lastGroundPosition;

        private List<PhysicsComponent> ignoredColliders = new();
        private List<IgnoredCollider> ignoredColliderStack = new();

        private const float TOLERANCE = .05f;
        private const float TINY_TOLERANCE = .01f;
        private const byte MAX_PUSHBACK_ITERATIONS = 3;
        private CollisionFilterGroups TemporaryLayerIndex;
        private const float CAST_DISTANCE = 10000f;

        // DontGoThroughThings
        //private float minimumExtent; // superCharacter.radius
        private Vector3 halfHeight;
        [DataMemberIgnore] public float PartialExtent { private set; get; }
        private float sqrMinimumExtent;
        public Vector3 PreviousPosition { get; private set; }


        private void Awake()
        {
            ownCollider = Entity.Get<PhysicsComponent>();
        }

        public override void Start()
        {
            Awake();
            //Log.Info("SCC: Start!");

            if (ownCollider != null) IgnoreCollider(ownCollider);

            TemporaryLayerIndex = CollisionFilterGroups.CustomFilter10;
            CurrentlyClampedTo = null;
            DeltaTime = Time.FixedDeltaTime;

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

            CurrentGround = new SuperGround(WalkableGroups, WalkableFlags, this);

            // DontGoThroughThings
            PartialExtent = Radius * .95f;
            sqrMinimumExtent = Radius * Radius;
            halfHeight = new(0, Height * .5f, 0); // center of character
            PreviousPosition = Entity.Transform.Position + halfHeight;
        }

        private void DontGoThroughThings()
        {
            // Have we moved more than our minimum extent?
            var movementThisStep = (Entity.Transform.Position + halfHeight) - PreviousPosition;
            var movementSqrLength = movementThisStep.LengthSquared();

            if (movementSqrLength > sqrMinimumExtent)
            {
                var movementLength = MathF.Sqrt(movementSqrLength);
                //Log.Warning($"Moved a lot! {movementLength:N2}");

                // Check for obstructions we might have missed.
                if (Physics.Raycast(
                    PreviousPosition,
                    Vector3.Normalize(movementThisStep),
                    movementLength,
                    out var hitInfo,
                    out var _,
                    WalkableGroups,
                    WalkableFlags))
                {
                    var snapBack = hitInfo.Point - (movementThisStep / movementLength) * PartialExtent - halfHeight;
                    Log.Warning($"Don't go! Position snap from {Entity.Transform.Position:N2} to {snapBack:N2}");
                    //playerMachine.ThisPlayer.Teleport(snapBack);
                    Entity.Transform.Position = snapBack;
                }
            }

            PreviousPosition = Entity.Transform.Position + halfHeight;
        }

        public void SetPreviousPosition()
        {
            PreviousPosition = Entity.Transform.Position + halfHeight;
        }

        // Can run less than Update.
        private float timeRemainder;
        public void FixedUpdate()
        {
            timeRemainder += Time.DeltaTime;
            short fixedUpdates = (short)(timeRemainder / DeltaTime); // DeltaTime = Time.FixedDeltaTime
            timeRemainder -= DeltaTime * fixedUpdates;
            while (fixedUpdates > 0) { SingleUpdate(); fixedUpdates--; }
            //for (ushort i = 0; i < fixedUpdates; i++) SingleUpdate();
        }

        public override void Update()
        {
            if (ManualUpdateOnly) return;

            if (FixedTimeStep)
            {
                // If we are using a fixed timestep, ensure we run the main update loop
                // a sufficient number of times based on the "Time.DeltaTime".
                // Will always execute more or equal to Update, not less! 
                /*float delta = Time.DeltaTime;
                while (delta > Time.FixedDeltaTime)
                {
                    SingleUpdate();
                    delta -= Time.FixedDeltaTime;
                }
                if (delta > 0) SingleUpdate();*/

                FixedUpdate();
            }
            else
            {
                DeltaTime = Time.DeltaTime;
                SingleUpdate();
            }
        }

        public void ManualUpdate(float deltaTime)
        {
            SingleUpdate();
        }

        private void SingleUpdate()
        {
            // Check if we are clamped to an object implicity or explicity
            bool isClamping = GroundClamping || CurrentlyClampedTo != null;
            Entity clampedTo = CurrentlyClampedTo ?? CurrentGround.Object;

            if (clampedTo != null && ClampToMovingGround && isClamping)
            {
                Entity.Transform.Position += clampedTo.Transform.Position - lastGroundPosition;
            }

            //if (CurrentGround == null) { Helper.LogScreen("No ground.", new Int2(10, 300)); }

            initialPosition = Entity.Transform.Position;

            ProbeGround();

            //gameObject.SendMessage("SuperUpdate", SendMessageOptions.DontRequireReceiver);
            BasicCameraController.Instance.DoSuperUpdate(); // important to call it right here

            CollisionData.Clear();

            RecursivePushback(0, MAX_PUSHBACK_ITERATIONS);

            ProbeGround();

            //if (SlopeLimiting && CurrentGround.IsGrounded(true, TINY_TOLERANCE)) SlopeLimit();
            if (SlopeLimiting) SlopeLimit(); // then manually disable SlopeLimiting if character is not grounded

            ProbeGround();

            if (GroundClamping) ClampToGround();

            isClamping = GroundClamping || CurrentlyClampedTo != null;
            clampedTo = CurrentlyClampedTo ?? CurrentGround.Object;

            if (isClamping && clampedTo != null)
                lastGroundPosition = clampedTo.Transform.Position;

            DontGoThroughThings();
            //AfterSingleUpdate?.Invoke();
        }

        private void ProbeGround()
        {
            PushIgnoredColliders();
            CurrentGround.ProbeGround(SpherePosition(Feet));
            PopIgnoredColliders();
        }

        /// <summary>
        /// Prevents the player from walking up slopes of a larger angle than the object's SlopeLimit.
        /// </summary>
        /// <returns>True if the controller attemped to ascend a too steep slope and had their movement limited</returns>
        private void SlopeLimit()
        {
            if (CurrentGround == null) return;
            Vector3 _n = CurrentGround.PrimaryNormal();
            float _a = Mathf.Angle(_n, Up);

            if (_a > CurrentGround.MySuperCollisionType.SlopeLimit)
            {
                Vector3 absoluteMoveDirection = Math3d.ProjectVectorOnPlane(_n, Entity.Transform.Position - initialPosition);

                // Retrieve a vector pointing down the slope
                Vector3 _r = Vector3.Cross(_n, Down);
                Vector3 _v = Vector3.Cross(_r, _n);

                float angle = Mathf.Angle(absoluteMoveDirection, _v);

                if (angle <= 90.0f) return;

                // Calculate where to place the controller on the slope, or at the bottom, based on the desired movement distance
                Vector3 resolvedPosition = Math3d.ProjectPointOnLine(initialPosition, _r, Entity.Transform.Position);
                Vector3 direction = Math3d.ProjectVectorOnPlane(_n, resolvedPosition - Entity.Transform.Position);

                // Check if our path to our resolved position is blocked by any colliders
                /*if (Physics.CapsuleCast(
                    SpherePosition(Feet),
                    SpherePosition(Head),
                    Radius,
                    Vector3.Normalize(direction),
                    direction.Length(),
                    out var _, // hit info
                    out var hitDistance, // center distance
                    out var _, // point distance
                    WalkableGroups,
                    WalkableFlags))*/
                if (Physics.SphereCast(
                    SpherePosition(Feet),
                    Radius,
                    Vector3.Normalize(direction),
                    direction.Length(),
                    out var _, // hit info
                    out var hitDistanceCenter,
                    WalkableGroups,
                    WalkableFlags))
                {
                    var dist = (Vector3.Normalize(_v) * hitDistanceCenter) + (HalfLength * Up);
                    Helper.LogInfo($"Move to cast: {dist:N2}");
                    Entity.Transform.Position += dist;
                }
                else
                {
                    Helper.LogInfo($"Move: {direction:N2}");
                    Entity.Transform.Position += direction;
                }
            }
        }

        private void ClampToGround()
        {
            if (CurrentGround == null) return;
            Entity.Transform.Position -= Up * CurrentGround.Distance();
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
        private void RecursivePushback(int depth, byte maxDepth)
        {
            PushIgnoredColliders();

            bool contact = false;

            foreach (var sphere in spheres) // feet, body, head
            {
                foreach (var hr in Physics.OverlapSphere(
                    SpherePosition(sphere),
                    Radius,
                    WalkableGroups,
                    WalkableFlags))
                {
                    var spherePosition = SpherePosition(sphere);
                    var physicsComponent = hr.Collider;
                    bool contactPointSuccess = SuperCollider.ClosestPointOnSurface(physicsComponent, spherePosition, Radius, out var contactPoint);

                    if (!contactPointSuccess)
                    {
                        return;
                    }

                    var _v = contactPoint - spherePosition; // direction to contact point
                    if (_v != Vector3.Zero)
                    {
                        // Cache the collider's layer so that we can cast against it
                        var layer = physicsComponent.CollisionGroup;

                        physicsComponent.CollisionGroup = TemporaryLayerIndex;

                        // Check which side of the normal we are on
                        bool facingNormal = Physics.SphereCast(
                            new Ray(spherePosition, Vector3.Normalize(_v)),
                            TINY_TOLERANCE,
                            _v.Length() + TINY_TOLERANCE,
                            (CollisionFilterGroups)(1 << (int)TemporaryLayerIndex),
                            WalkableFlags);

                        physicsComponent.CollisionGroup = layer;

                        // Orient and scale our vector based on which side of the normal we are situated
                        if (facingNormal)
                        {
                            if (Vector3.Distance(spherePosition, contactPoint) < Radius)
                            {
                                _v = Vector3.Normalize(_v) * (Radius - _v.Length()) * -1;
                            }
                            else
                            {
                                // A previously resolved collision has had a side effect that moved us outside this collider
                                continue;
                            }
                        }
                        else
                        {
                            _v = Vector3.Normalize(_v) * (Radius + _v.Length());
                        }

                        contact = true;

                        //Helper.LogScreen($"Pushback: {_v:N2}", new Int2(10, 150));
                        Entity.Transform.Position += _v; // move

                        physicsComponent.CollisionGroup = TemporaryLayerIndex;

                        // Retrieve the surface normal of the collided point
                        var pv = spherePosition + _v;
                        Physics.SphereCast(
                            new Ray(pv, contactPoint - pv),
                            TINY_TOLERANCE,
                            out var normalHit,
                            CAST_DISTANCE,
                            (CollisionFilterGroups)(1 << (int)TemporaryLayerIndex),
                            WalkableFlags);

                        physicsComponent.CollisionGroup = layer;

                        SuperCollisionType superColType = physicsComponent.Entity.GetOrCreate<SuperCollisionType>();

                        // Our collision affected the collider; add it to the collision data
                        var collision = new SuperCollision()
                        {
                            collisionSphere = sphere,
                            superCollisionType = superColType,
                            entity = physicsComponent.Entity,
                            point = contactPoint,
                            normal = normalHit.Normal,
                        };

                        CollisionData.Add(collision); // unused
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
            return Mathf.Angle(point - SpherePosition(Head), Up) > 89.0f;
        }

        public bool PointAboveFeet(Vector3 point)
        {
            return Mathf.Angle(point - SpherePosition(Feet), Down) > 89.0f;
        }

        public void IgnoreCollider(PhysicsComponent col)
        {
            ignoredColliders.Add(col);
        }

        public void RemoveIgnoredCollider(PhysicsComponent col)
        {
            ignoredColliders.Remove(col);
        }

        public void ClearIgnoredColliders()
        {
            ignoredColliders.Clear();
        }

        public class SuperGround
        {
            public SuperGround(
                CollisionFilterGroups walkable,
                CollisionFilterGroupFlags flags,
                SuperCharacterController controller
                //, bool triggerInteraction
                )
            {
                this.walkableGroups = walkable;
                this.controller = controller;
                this.walkableFlags = flags;
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

            private CollisionFilterGroups walkableGroups;
            private CollisionFilterGroupFlags walkableFlags;
            private SuperCharacterController controller;
            //private bool triggerInteraction;

            private GroundHit primaryGround;
            private GroundHit nearGround;
            private GroundHit farGround;
            private GroundHit stepGround;
            private GroundHit flushGround;

            [DataMemberIgnore] public SuperCollisionType MySuperCollisionType { get; private set; }
            [DataMemberIgnore] public Entity Object { get; private set; }

            private const float groundingUpperBoundAngle = 60.0f;
            private const float groundingMaxPercentFromCenter = 0.85f;
            private const float groundingMinPercentFromcenter = 0.50f;

            /// <summary>
            /// Scan the surface below us for ground. Follow up the initial scan with subsequent scans
            /// designed to test what kind of surface we are standing above and handle different edge cases
            /// </summary>
            /// <param name="origin">Center of the sphere for the initial SphereCast</param>
            // <param name="iter">Debug tool to print out which ProbeGround iteration is being run (3 are run each frame for the controller)</param

            public void ProbeGround(Vector3 origin)
            {
                ResetGrounds();

                Vector3 _origin = origin + (controller.Up * TOLERANCE);

                // Reduce our radius by Tolerance squared to avoid failing the SphereCast due to clipping with walls
                float smallerRadius = controller.Radius - (TOLERANCE * TOLERANCE);

                HitResult hit; // reused

                if (Physics.SphereCast(
                    _origin,
                    smallerRadius,
                    controller.Down,
                    out hit,
                    CAST_DISTANCE,
                    walkableGroups,
                    walkableFlags))
                {
                    Object = hit.Collider.Entity;
                    var superColType = Object.GetOrCreate<SuperCollisionType>();
                    MySuperCollisionType = superColType;

                    //Helper.LogScreen($"ProbeGround: SphereCast Normal: {hit.Normal:N2}", new Int2(10, 10));

                    // By reducing the initial SphereCast's radius by Tolerance, our casted sphere no longer fits with
                    // our controller's shape. Reconstruct the sphere cast with the proper radius
                    SimulateSphereCast(hit.Normal, out hit, out float hitDistance);

                    //Helper.LogScreen($"ProbeGround: SimulateSphereCast Normal: {hit.Normal:N2} Distance: {hitDistance}:N2", new Int2(10, 30));

                    primaryGround = new GroundHit(hit.Point, hit.Normal, hitDistance);

                    // If we are standing on a perfectly flat surface, we cannot be either on an edge,
                    // on a slope or stepping off a ledge.
                    var _projection = Math3d.ProjectPointOnPlane(controller.Up, controller.Entity.Transform.Position, hit.Point);
                    if (Vector3.Distance(_projection, controller.Entity.Transform.Position) < TINY_TOLERANCE)
                    {
                        //Helper.LogScreen($"ProbeGround: Below is flat: {_projection:N2}", new Int2(10, 50));
                        return;
                    }

                    // As we are standing on an edge, we need to retrieve the normals of the two
                    // faces on either side of the edge and store them in nearHit and farHit
                    var toCenter = Math3d.ProjectVectorOnPlane(controller.Up,
                        Vector3.Normalize(controller.Entity.Transform.Position - hit.Point) * TINY_TOLERANCE);

                    //Vector3 awayFromCenter = Quaternion.RotationAxis(Vector3.Cross(toCenter, up), -80.0f * Mathf.Deg2Rad) * -toCenter; // angle in radians
                    Vector3 awayFromCenter = Mathf.AngleAxis(-80.0f, Vector3.Cross(toCenter, controller.Up)) * -toCenter;

                    Vector3 nearPoint = hit.Point + toCenter + (controller.Up * TINY_TOLERANCE);
                    Vector3 farPoint = hit.Point + (awayFromCenter * 3);

                    Physics.Raycast(
                        nearPoint,
                        controller.Down,
                        CAST_DISTANCE,
                        out HitResult nearHit,
                        out var nearHitDistance,
                        walkableGroups,
                        walkableFlags);
                    Physics.Raycast(
                        farPoint,
                        controller.Down,
                        CAST_DISTANCE,
                        out HitResult farHit,
                        out var farHitDistance,
                        walkableGroups,
                        walkableFlags);

                    nearGround = new GroundHit(nearHit.Point, nearHit.Normal, nearHitDistance);
                    farGround = new GroundHit(farHit.Point, farHit.Normal, farHitDistance);

                    // If we are currently standing on ground that should be counted as a wall,
                    // we are likely flush against it on the ground. Retrieve what we are standing on
                    if (Mathf.Angle(hit.Normal, controller.Up) > superColType.StandAngle)
                    {
                        //Helper.LogScreen($"ProbeGround: Angle: {Mathf.Angle(hit.Normal, controller.Up):N2}", new Int2(10, 50));
                        // Retrieve a vector pointing down the slope
                        var _r = Vector3.Cross(hit.Normal, controller.Down);
                        var downTheSlope = Vector3.Cross(_r, hit.Normal);

                        var flushOrigin = hit.Point + (hit.Normal * TINY_TOLERANCE); // add offset to raycast down along the slope
                        if (Physics.Raycast(
                            flushOrigin,
                            downTheSlope,
                            CAST_DISTANCE,
                            out var flushHit,
                            out var _,
                            walkableGroups,
                            walkableFlags))
                        {
                            //Helper.LogScreen($"ProbeGround: Slope end: {flushHit.Point:N2}", new Int2(10, 70));
                            if (SimulateSphereCast(
                                flushHit.Normal,
                                out var sphereCastHit,
                                out var sphereCastHitDistance))
                            {
                                flushGround = new GroundHit(sphereCastHit.Point, sphereCastHit.Normal, sphereCastHitDistance);
                            }
                            else
                            {
                                // Uh? 
                            }
                        }
                    }

                    // If we are currently standing on a ledge then the face nearest the center of the
                    // controller should be steep enough to be counted as a wall. Retrieve the ground
                    // it is connected to at it's base, if there exists any
                    if (Mathf.Angle(nearHit.Normal, controller.Up) > superColType.StandAngle || nearHitDistance > TOLERANCE)
                    {
                        //Helper.LogScreen($"ProbeGround: Wall to ground.", new Int2(10, 90));
                        var col = nearHit.Collider.Entity.GetOrCreate<SuperCollisionType>();

                        //col ??= defaultCollisionType;

                        // We contacted the wall of the ledge, rather than the landing. Raycast down
                        // the wall to retrieve the proper landing
                        if (Mathf.Angle(nearHit.Normal, controller.Up) > col.StandAngle)
                        {
                            //Helper.LogScreen($"ProbeGround: Sliding slope.", new Int2(10, 110));
                            // Retrieve a vector pointing down the slope
                            Vector3 _r = Vector3.Cross(nearHit.Normal, controller.Down);
                            Vector3 downTheSlope = Vector3.Cross(_r, nearHit.Normal);

                            if (Physics.Raycast(
                                nearPoint,
                                downTheSlope,
                                CAST_DISTANCE,
                                out var stepHit,
                                out var stepHitDistance,
                                walkableGroups,
                                walkableFlags))
                            {
                                stepGround = new GroundHit(stepHit.Point, stepHit.Normal, stepHitDistance);
                            }
                        }
                        else
                        {
                            //Helper.LogScreen($"ProbeGround: Standing slope.", new Int2(10, 110));
                            stepGround = new GroundHit(nearHit.Point, nearHit.Normal, nearHitDistance);
                        }
                    }
                }
                // If the initial SphereCast fails, likely due to the controller clipping a wall,
                // fallback to a raycast simulated to SphereCast data
                else if (Physics.Raycast(
                    _origin,
                    controller.Down,
                    CAST_DISTANCE,
                    out hit,
                    out var hitDistance,
                    walkableGroups,
                    walkableFlags))
                {
                    Object = hit.Collider.Entity;
                    var superColType = Object.GetOrCreate<SuperCollisionType>();
                    MySuperCollisionType = superColType;

                    //Helper.LogScreen($"ProbeGround: Clipping a wall.", new Int2(10, 90));

                    if (SimulateSphereCast(
                        hit.Normal,
                        out HitResult sphereCastHit,
                        out float sphereCastHitDistance))
                    {
                        primaryGround = new GroundHit(sphereCastHit.Point, sphereCastHit.Normal, sphereCastHitDistance);
                    }
                    else
                    {
                        primaryGround = new GroundHit(hit.Point, hit.Normal, hitDistance);
                    }
                }
                else
                {
                    Helper.LogError("SCC: No ground was found below the player!");
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

            // not a running logic
            public bool IsGrounded(bool currentlyGrounded, float distance)
            {
                return IsGrounded(currentlyGrounded, distance, out Vector3 groundNormal);
            }

            public bool IsGrounded(bool currentlyGrounded, float distance, out Vector3 groundNormal)
            {
                groundNormal = Vector3.Zero;

                if (primaryGround == null || primaryGround.Distance > distance)
                {
                    return false;
                }

                // Check if we are flush against a wall
                if (farGround != null && Mathf.Angle(farGround.Normal, controller.Up) > MySuperCollisionType.StandAngle)
                {
                    if (flushGround != null &&
                        (Mathf.Angle(flushGround.Normal, controller.Up) < MySuperCollisionType.StandAngle) &&
                        (flushGround.Distance < distance))
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
                    if (nearGround != null &&
                        (nearGround.Distance < distance) &&
                        (Mathf.Angle(nearGround.Normal, controller.Up) < MySuperCollisionType.StandAngle) &&
                        !OnSteadyGround(nearGround.Normal, nearGround.Point))
                    {
                        groundNormal = nearGround.Normal;
                        return true;
                    }

                    // Check if we are on a step or stair
                    if (stepGround != null &&
                        (stepGround.Distance < distance) &&
                        (Mathf.Angle(stepGround.Normal, controller.Up) < MySuperCollisionType.StandAngle))
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

            // IsGrounded
            private bool OnSteadyGround(Vector3 normal, Vector3 point)
            {
                float angle = Mathf.Angle(normal, controller.Up);
                float angleRatio = angle / groundingUpperBoundAngle;
                float distanceRatio = Mathf.LerpClamped(groundingMinPercentFromcenter, groundingMaxPercentFromCenter, angleRatio);
                Vector3 _p = Math3d.ProjectPointOnPlane(controller.Up, controller.Entity.Transform.Position, point);
                float distanceFromCenter = Vector3.Distance(_p, controller.Entity.Transform.Position);

                return distanceFromCenter <= distanceRatio * controller.Radius;
            }

            // SlopeLimit
            public Vector3 PrimaryNormal()
            {
                if (primaryGround == null) return Vector3.UnitY;
                else return primaryGround.Normal;
            }

            // ClapmToGround
            public float Distance()
            {
                if (primaryGround == null) return 0F;
                else return primaryGround.Distance;
            }

            /// <summary>
            /// Provides raycast data based on where a SphereCast would contact the specified normal
            /// Raycasting downwards from a point along the controller's bottom sphere, based on the provided
            /// normal
            /// </summary>
            /// <param name="groundNormal">Normal of a triangle assumed to be directly below the controller</param>
            /// <param name="hitResult">Simulated SphereCast data</param>
            /// <returns>success, hitResult, hitDistance</returns>
            private bool SimulateSphereCast(Vector3 groundNormal, out HitResult hitResult, out float hitDistance)
            {
                float groundAngle = Mathf.Angle(groundNormal, controller.Up, true);

                Vector3 secondaryOrigin = controller.Entity.Transform.Position + controller.Up * TOLERANCE;

                //if (!MathUtil.NearEqual(groundAngle, 0))
                if (!Mathf.Approximately(groundAngle, 0))
                {
                    float horizontal = MathF.Sin(groundAngle) * controller.Radius;
                    float vertical = (1.0f - MathF.Cos(groundAngle)) * controller.Radius;

                    // Retrieve a vector pointing up the slope
                    Vector3 r2 = Vector3.Cross(groundNormal, controller.Down);
                    Vector3 v2 = -Vector3.Cross(r2, groundNormal);

                    secondaryOrigin +=
                        Vector3.Normalize(Math3d.ProjectVectorOnPlane(controller.Up, v2)) * horizontal +
                        controller.Up * vertical;
                }

                if (Physics.Raycast(
                    secondaryOrigin,
                    controller.Down,
                    CAST_DISTANCE,
                    out hitResult,
                    out hitDistance,
                    walkableGroups,
                    walkableFlags))
                {
                    // Remove the tolerance from the distance travelled
                    hitDistance -= (TOLERANCE + TINY_TOLERANCE);

                    return true;
                }
                else
                {
                    hitDistance = float.NaN;
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
        public Entity entity;
        public Vector3 point;
        public Vector3 normal;
    }
}
