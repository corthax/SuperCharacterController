using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System;

namespace GameClient.SuperCharacter
{
    public static class SuperCollider
    {
        public static bool ClosestPointOnSurface(PhysicsComponent collider, Vector3 to, float radius, out Vector3 closestPointOnSurface)
        {
            if (collider.ColliderShape is BoxColliderShape shape)
            {
                closestPointOnSurface = ClosestPointOnSurface(shape, to, collider.Entity);
                return true;
            }
            else if (collider.ColliderShape is SphereColliderShape shape1)
            {
                closestPointOnSurface = ClosestPointOnSurface(shape1, to, collider.Entity);
                return true;
            }
            else if (collider.ColliderShape is CapsuleColliderShape shape2)
            {
                closestPointOnSurface = ClosestPointOnSurface(shape2, to, collider.Entity);
                return true;
            }
            else if (collider.ColliderShape is StaticMeshColliderShape)
            {
                /*RPGMesh rpgMesh = collider.GetComponent<RPGMesh>();

                if (rpgMesh != null)
                {
                    closestPointOnSurface = rpgMesh.ClosestPointOn(to, radius, false, false);
                    return true;
                }*/

                BSPTree bsp = collider.Entity.GetOrCreate<BSPTree>();

                if (bsp != null)
                {
                    closestPointOnSurface = bsp.ClosestPointOn(to, radius);
                    return true;
                }

                /*BruteForceMesh bfm = collider.Get<BruteForceMesh>();

                if (bfm != null)
                {
                    closestPointOnSurface = bfm.ClosestPointOn(to);
                    return true;
                }*/
            }
            /*else if (collider is TerrainCollider)
            {
                closestPointOnSurface = SuperCollider.ClosestPointOnSurface((TerrainCollider)collider, to, radius, false);
                return true;
            }*/

            //Log.Error(string.Format("{0} does not have an implementation for ClosestPointOnSurface; GameObject.Name='{1}'", collider.GetType(), collider.gameObject.name));
            closestPointOnSurface = Vector3.Zero;
            return false;
        }

        public static Vector3 ClosestPointOnSurface(SphereColliderShape collider, Vector3 to, Entity colliderEntity)
        {
            Vector3 p;

            // Cache the collider transform
            var ct = colliderEntity.Transform;

            //Vector3 collider_center = Vector3.Zero; //?

            p = to - (ct.Position + collider.Radius);
            p.Normalize();

            p *= collider.Radius * ct.Scale.X;
            //p += ct.Position + collider_center;
            p += ct.Position;

            return p;
        }

        public static Vector3 ClosestPointOnSurface(BoxColliderShape collider, Vector3 to, Entity colliderEntity)
        {
            // Cache the collider transform
            var ct = colliderEntity.Transform;

            // Firstly, transform the point into the space of the collider
            var local = ct.WorldToLocal(to);
            Vector3 collider_center = Vector3.Zero; //?
            // Now, shift it to be in the center of the box
            local -= collider_center;

            //Pre multiply to save operations.
            var halfSize = collider.BoxSize * 0.5f;

            // Clamp the points to the collider's extents
            var localNorm = new Vector3(
                    Math.Clamp(local.X, -halfSize.X, halfSize.X),
                    Math.Clamp(local.Y, -halfSize.Y, halfSize.Y),
                    Math.Clamp(local.Z, -halfSize.Z, halfSize.Z)
                );

            //Calculate distances from each edge
            var dx = MathF.Min(MathF.Abs(halfSize.X - localNorm.X), MathF.Abs(-halfSize.X - localNorm.X));
            var dy = MathF.Min(MathF.Abs(halfSize.Y - localNorm.Y), MathF.Abs(-halfSize.Y - localNorm.Y));
            var dz = MathF.Min(MathF.Abs(halfSize.Z - localNorm.Z), MathF.Abs(-halfSize.Z - localNorm.Z));

            // Select a face to project on
            if (dx < dy && dx < dz)
            {
                localNorm.X = MathF.Sign(localNorm.X) * halfSize.X;
            }
            else if (dy < dx && dy < dz)
            {
                localNorm.Y = MathF.Sign(localNorm.Y) * halfSize.Y;
            }
            else if (dz < dx && dz < dy)
            {
                localNorm.Z = MathF.Sign(localNorm.Z) * halfSize.Z;
            }

            // Now we undo our transformations
            localNorm += collider_center;

            // Return resulting point
            return ct.LocalToWorld(localNorm);
        }

        // Courtesy of Moodie
        public static Vector3 ClosestPointOnSurface(CapsuleColliderShape collider, Vector3 to, Entity colliderEntity)
        {
            var ct = colliderEntity.Transform; // Transform of the collider

            float lineLength = collider.Length - collider.Radius * 2; // The length of the line connecting the center of both sphere
            Vector3 dir = Vector3.UnitY;

            var collider_center = collider.Radius * 0.5f;

            Vector3 upperSphere = dir * lineLength * 0.5f + collider_center; // The position of the radius of the upper sphere in local coordinates
            Vector3 lowerSphere = -dir * lineLength * 0.5f + collider_center; // The position of the radius of the lower sphere in local coordinates

            Vector3 local = ct.WorldToLocal(to); // The position of the controller in local coordinates

            Vector3 p; // Contact point
            Vector3 pt = Vector3.Zero; // The point we need to use to get a direction vector with the controller to calculate contact point

            if (local.Y < lineLength * 0.5f && local.Y > -lineLength * 0.5f) // Controller is contacting with cylinder, not spheres
                pt = dir * local.Y + collider_center;
            else if (local.Y > lineLength * 0.5f) // Controller is contacting with the upper sphere 
                pt = upperSphere;
            else if (local.Y < -lineLength * 0.5f) // Controller is contacting with lower sphere
                pt = lowerSphere;

            //Calculate contact point in local coordinates and return it in world coordinates
            p = local - pt;
            p.Normalize();
            p = p * collider.Radius + pt;
            return ct.LocalToWorld(p);
        }

        /*public static Vector3 ClosestPointOnSurface(TerrainCollider collider, Vector3 to, float radius, bool debug = false)
        {
            var terrainData = collider.terrainData;

            var local = collider.Entity.Transform.WorldToLocal(to);

            // Calculate the size of each tile on the terrain horizontally and vertically
            float pixelSizeX = terrainData.size.X / (terrainData.heightmapResolution - 1);
            float pixelSizeZ = terrainData.size.Z / (terrainData.heightmapResolution - 1);

            var percentZ = Math.Clamp(local.Z / terrainData.size.Z, 0, 1);
            var percentX = Math.Clamp(local.X / terrainData.size.X, 0, 1);

            float positionX = percentX * (terrainData.heightmapResolution - 1);
            float positionZ = percentZ * (terrainData.heightmapResolution - 1);

            // Calculate our position, in tiles, on the terrain
            int pixelX = (int)Math.Floor(positionX);
            int pixelZ = (int)Math.Floor(positionZ);

            // Calculate the distance from our point to the edge of the tile we are in
            float distanceX = (positionX - pixelX) * pixelSizeX;
            float distanceZ = (positionZ - pixelZ) * pixelSizeZ;

            // Find out how many tiles we are overlapping on the X plane
            float radiusExtentsLeftX = radius - distanceX;
            float radiusExtentsRightX = radius - (pixelSizeX - distanceX);

            int overlappedTilesXLeft = radiusExtentsLeftX > 0 ? (int)Math.Floor(radiusExtentsLeftX / pixelSizeX) + 1 : 0;
            int overlappedTilesXRight = radiusExtentsRightX > 0 ? (int)Math.Floor(radiusExtentsRightX / pixelSizeX) + 1 : 0;

            // Find out how many tiles we are overlapping on the Z plane
            float radiusExtentsLeftZ = radius - distanceZ;
            float radiusExtentsRightZ = radius - (pixelSizeZ - distanceZ);

            int overlappedTilesZLeft = radiusExtentsLeftZ > 0 ? (int)Math.Floor(radiusExtentsLeftZ / pixelSizeZ) + 1 : 0;
            int overlappedTilesZRight = radiusExtentsRightZ > 0 ? (int)Math.Floor(radiusExtentsRightZ / pixelSizeZ) + 1 : 0;

            // Retrieve the heights of the pixels we are testing against
            int startPositionX = pixelX - overlappedTilesXLeft;
            int startPositionZ = pixelZ - overlappedTilesZLeft;

            int numberOfXPixels = overlappedTilesXRight + overlappedTilesXLeft + 1;
            int numberOfZPixels = overlappedTilesZRight + overlappedTilesZLeft + 1;

            // Account for if we are off the terrain
            if (startPositionX < 0)
            {
                numberOfXPixels -= Math.Abs(startPositionX);
                startPositionX = 0;
            }

            if (startPositionZ < 0)
            {
                numberOfZPixels -= Math.Abs(startPositionZ);
                startPositionZ = 0;
            }

            if (startPositionX + numberOfXPixels + 1 > terrainData.heightmapResolution)
            {
                numberOfXPixels = terrainData.heightmapResolution - startPositionX - 1;
            }

            if (startPositionZ + numberOfZPixels + 1 > terrainData.heightmapResolution)
            {
                numberOfZPixels = terrainData.heightmapResolution - startPositionZ - 1;
            }

            // Retrieve the heights of the tile we are in and all overlapped tiles
            var heights = terrainData.GetHeights(startPositionX, startPositionZ, numberOfXPixels + 1, numberOfZPixels + 1);

            // Pre-scale the heights data to be world-scale instead of 0...1
            for (int i = 0; i < numberOfXPixels + 1; i++)
            {
                for (int j = 0; j < numberOfZPixels + 1; j++)
                {
                    heights[j, i] *= terrainData.size.Y;
                }
            }

            // Find the shortest distance to any triangle in the set gathered
            float shortestDistance = float.MaxValue;

            Vector3 shortestPoint = Vector3.Zero;

            for (int x = 0; x < numberOfXPixels; x++)
            {
                for (int z = 0; z < numberOfZPixels; z++)
                {
                    // Build the set of points that creates the two triangles that form this tile
                    Vector3 a = new Vector3((startPositionX + x) * pixelSizeX, heights[z, x], (startPositionZ + z) * pixelSizeZ);
                    Vector3 b = new Vector3((startPositionX + x + 1) * pixelSizeX, heights[z, x + 1], (startPositionZ + z) * pixelSizeZ);
                    Vector3 c = new Vector3((startPositionX + x) * pixelSizeX, heights[z + 1, x], (startPositionZ + z + 1) * pixelSizeZ);
                    Vector3 d = new Vector3((startPositionX + x + 1) * pixelSizeX, heights[z + 1, x + 1], (startPositionZ + z + 1) * pixelSizeZ);

                    Vector3 nearest;

                    BSPTree.ClosestPointOnTriangleToPoint(ref a, ref d, ref c, ref local, out nearest);

                    float distance = (local - nearest).LengthSquared();

                    if (distance <= shortestDistance)
                    {
                        shortestDistance = distance;
                        shortestPoint = nearest;
                    }

                    BSPTree.ClosestPointOnTriangleToPoint(ref a, ref b, ref d, ref local, out nearest);

                    distance = (local - nearest).LengthSquared();

                    if (distance <= shortestDistance)
                    {
                        shortestDistance = distance;
                        shortestPoint = nearest;
                    }

                    if (debug)
                    {
                        //DebugDraw.DrawTriangle(a, d, c, Color.cyan);
                        //DebugDraw.DrawTriangle(a, b, d, Color.red);
                    }
                }
            }
            return collider.Entity.Transform.LocalToWorld(shortestPoint);
        }*/
    }
}
