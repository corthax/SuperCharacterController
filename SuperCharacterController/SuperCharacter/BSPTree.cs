using System.Collections.Generic;
using Stride.Physics;
using Stride.Engine;
using Stride.Core.Mathematics;
using Stride.Rendering;
using System;
using Stride.Rendering.Rendering.MeshDataTool;
using SCC.Tools;

namespace SCC.SuperCharacter
{
    /// <summary>
    /// Recursively paritions a mesh's vertices to allow to more quickly
    /// narrow down the search for a nearest point on it's surface with respect to another
    /// point
    /// </summary>

    public class BSPTree : StartupScript
    {
        public class Node
        {
            public Vector3 partitionPoint;
            public Vector3 partitionNormal;

            public Node positiveChild;
            public Node negativeChild;

            public int[] triangles;
        };

        private int triangleCount;
        private int vertexCount;
        private Vector3[] vertices;
        private int[] tris;
        private Vector3[] triangleNormals;

        private Mesh mesh;

        private Node tree;

        private void Awake()
        {
            var _c = Entity.Get<StaticColliderComponent>();
            while (_c == null) { Log.Warning("Can't get 'StaticColliderComponent'!"); _c = Entity.Get<StaticColliderComponent>(); }

            var _s = _c.ColliderShape;
            while (_s == null) { Log.Warning("Can't get 'ColliderShape'!"); _s = _c.ColliderShape; }

            if (_s.GetType() != typeof(StaticMeshColliderShape)) { Log.Warning("Only merged mesh colliders supported!"); }

            var shape = (StaticMeshColliderShape)_s;

            mesh = shape.Model.Meshes[0];

            var dt = new MeshDataToolGPU(mesh);

            tris = dt.getIndicies();
            vertices = dt.getPositions();

            vertexCount = dt.getTotalVerticies();
            triangleCount = dt.getTotalFaces();

            triangleNormals = new Vector3[triangleCount];

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 normal = Vector3.Normalize
                (
                    Vector3.Cross
                    (
                        Vector3.Normalize(vertices[tris[i + 1]] - vertices[tris[i]]),
                        Vector3.Normalize(vertices[tris[i + 2]] - vertices[tris[i]])
                    )
                );

                triangleNormals[i / 3] = normal;
            }
        }

        public override void Start()
        {
            Priority = -1;
            Awake();
            BuildTriangleTree();
        }

        /// <summary>
        /// Returns the closest point on the mesh with respect to Vector3 point to
        /// </summary>
        public Vector3 ClosestPointOn(Vector3 to, float radius)
        {
            //to = transform.InverseTransformPoint(to);
            to = Entity.Transform.WorldToLocal(to);

            List<int> triangles = new();

            FindClosestTriangles(tree, to, radius, triangles);

            var closest = ClosestPointOnTriangle(triangles.ToArray(), to);

            //return transform.TransformPoint(closest);
            return Entity.Transform.LocalToWorld(closest);
        }

        void FindClosestTriangles(Node node, Vector3 to, float radius, List<int> triangles)
        {
            if (node == null) { Log.Warning("Node is null!"); return; }
            if (node.triangles == null)
            {
                if (PointDistanceFromPlane(node.partitionPoint, node.partitionNormal, to) <= radius)
                {
                    FindClosestTriangles(node.positiveChild, to, radius, triangles);
                    FindClosestTriangles(node.negativeChild, to, radius, triangles);
                }
                else if (PointAbovePlane(node.partitionPoint, node.partitionNormal, to))
                {
                    FindClosestTriangles(node.positiveChild, to, radius, triangles);
                }
                else
                {
                    FindClosestTriangles(node.negativeChild, to, radius, triangles);
                }
            }
            else
            {
                triangles.AddRange(node.triangles);
            }
        }

        Vector3 ClosestPointOnTriangle(int[] triangles, Vector3 to)
        {
            float shortestDistance = float.MaxValue;

            var shortestPoint = Vector3.Zero;

            // Iterate through all triangles
            foreach (int triangle in triangles)
            {
                var p1 = vertices[tris[triangle]];
                var p2 = vertices[tris[triangle + 1]];
                var p3 = vertices[tris[triangle + 2]];

                ClosestPointOnTriangleToPoint(ref p1, ref p2, ref p3, ref to, out var nearest);

                float distance = (to - nearest).LengthSquared();

                if (distance <= shortestDistance)
                {
                    shortestDistance = distance;
                    shortestPoint = nearest;
                }
            }

            return shortestPoint;
        }

        void BuildTriangleTree()
        {
            List<int> rootTriangles = new();

            for (int i = 0; i < tris.Length; i += 3)
            {
                rootTriangles.Add(i);
            }

            tree = new Node();

            RecursivePartition(rootTriangles, 0, tree);
        }

        void RecursivePartition(List<int> triangles, int depth, Node parent)
        {
            var partitionPoint = Vector3.Zero;

            Vector3 maxExtents = new(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            Vector3 minExtents = new(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (int triangle in triangles)
            {
                partitionPoint += vertices[tris[triangle]] + vertices[tris[triangle + 1]] + vertices[tris[triangle + 2]];

                minExtents.X = Mathf.Min(minExtents.X, vertices[tris[triangle]].X, vertices[tris[triangle + 1]].X, vertices[tris[triangle + 2]].X);
                minExtents.Y = Mathf.Min(minExtents.Y, vertices[tris[triangle]].Y, vertices[tris[triangle + 1]].Y, vertices[tris[triangle + 2]].Y);
                minExtents.Z = Mathf.Min(minExtents.Z, vertices[tris[triangle]].Z, vertices[tris[triangle + 1]].Z, vertices[tris[triangle + 2]].Z);

                maxExtents.X = Mathf.Max(maxExtents.X, vertices[tris[triangle]].X, vertices[tris[triangle + 1]].X, vertices[tris[triangle + 2]].X);
                maxExtents.Y = Mathf.Max(maxExtents.Y, vertices[tris[triangle]].Y, vertices[tris[triangle + 1]].Y, vertices[tris[triangle + 2]].Y);
                maxExtents.Z = Mathf.Max(maxExtents.Z, vertices[tris[triangle]].Z, vertices[tris[triangle + 1]].Z, vertices[tris[triangle + 2]].Z);
            }

            // Centroid of all vertices
            partitionPoint /= vertexCount;

            // Better idea? Center of bounding box
            partitionPoint = minExtents + Math3d.SetVectorLength((maxExtents - minExtents), (maxExtents - minExtents).Length() * 0.5f);

            Vector3 extentsMagnitude =
                new
                (
                    MathF.Abs(maxExtents.X - minExtents.X),
                    MathF.Abs(maxExtents.Y - minExtents.Y),
                    MathF.Abs(maxExtents.Z - minExtents.Z)
                );

            Vector3 partitionNormal;

            if (extentsMagnitude.X >= extentsMagnitude.Y && extentsMagnitude.X >= extentsMagnitude.Z)
                partitionNormal = Vector3.UnitX;
            else if (extentsMagnitude.Y >= extentsMagnitude.X && extentsMagnitude.Y >= extentsMagnitude.Z)
                partitionNormal = Vector3.UnitY;
            else
                partitionNormal = Vector3.UnitZ;


            Split(triangles, partitionPoint, partitionNormal, out List<int> positiveTriangles, out List<int> negativeTriangles);

            parent.partitionNormal = partitionNormal;
            parent.partitionPoint = partitionPoint;

            Node posNode = new();
            parent.positiveChild = posNode;

            Node negNode = new();
            parent.negativeChild = negNode;

            if (positiveTriangles.Count < triangles.Count && positiveTriangles.Count > 3)
            {
                RecursivePartition(positiveTriangles, depth + 1, posNode);
            }
            else
            {
                posNode.triangles = positiveTriangles.ToArray();

                /*if (drawMeshTreeOnStart)
                    DrawTriangleSet(posNode.triangles, DebugDraw.RandomColor());*/
            }

            if (negativeTriangles.Count < triangles.Count && negativeTriangles.Count > 3)
            {
                RecursivePartition(negativeTriangles, depth + 1, negNode);
            }
            else
            {
                negNode.triangles = negativeTriangles.ToArray();

                /*if (drawMeshTreeOnStart)
                    DrawTriangleSet(negNode.triangles, DebugDraw.RandomColor());*/
            }

        }

        /// <summary>
        /// Splits a a set of input triangles by a partition plane into positive and negative sets, with triangles
        /// that are intersected by the partition plane being placed in both sets
        /// </summary>
        void Split(List<int> triangles, Vector3 partitionPoint, Vector3 partitionNormal, out List<int> positiveTriangles, out List<int> negativeTriangles)
        {
            positiveTriangles = new List<int>();
            negativeTriangles = new List<int>();

            foreach (int triangle in triangles)
            {
                bool firstPointAbove = PointAbovePlane(partitionPoint, partitionNormal, vertices[tris[triangle]]);
                bool secondPointAbove = PointAbovePlane(partitionPoint, partitionNormal, vertices[tris[triangle + 1]]);
                bool thirdPointAbove = PointAbovePlane(partitionPoint, partitionNormal, vertices[tris[triangle + 2]]);

                if (firstPointAbove && secondPointAbove && thirdPointAbove)
                {
                    positiveTriangles.Add(triangle);
                }
                else if (!firstPointAbove && !secondPointAbove && !thirdPointAbove)
                {
                    negativeTriangles.Add(triangle);
                }
                else
                {
                    positiveTriangles.Add(triangle);
                    negativeTriangles.Add(triangle);
                }
            }
        }

        private static bool PointAbovePlane(Vector3 planeOrigin, Vector3 planeNormal, Vector3 point)
        {
            return Vector3.Dot(point - planeOrigin, planeNormal) >= 0;
        }

        private static float PointDistanceFromPlane(Vector3 planeOrigin, Vector3 planeNormal, Vector3 point)
        {
            return MathF.Abs(Vector3.Dot((point - planeOrigin), planeNormal));
        }

        /// <summary>
        /// Determines the closest point between a point and a triangle.
        /// Borrowed from RPGMesh class of the RPGController package for Unity, by fholm
        /// The code in this method is copyrighted by the SlimDX Group under the MIT license:
        /// 
        /// Copyright (c) 2007-2010 SlimDX Group
        /// 
        /// Permission is hereby granted, free of charge, to any person obtaining a copy
        /// of this software and associated documentation files (the "Software"), to deal
        /// in the Software without restriction, including without limitation the rights
        /// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        /// copies of the Software, and to permit persons to whom the Software is
        /// furnished to do so, subject to the following conditions:
        /// 
        /// The above copyright notice and this permission notice shall be included in
        /// all copies or substantial portions of the Software.
        /// 
        /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        /// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        /// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        /// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        /// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        /// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        /// THE SOFTWARE.

        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="vertex1">The first vertex to test.</param>
        /// <param name="vertex2">The second vertex to test.</param>
        /// <param name="vertex3">The third vertex to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects.</param>
        public static void ClosestPointOnTriangleToPoint(ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, ref Vector3 point, out Vector3 result)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 136

            //Check if P in vertex region outside A
            Vector3 ab = vertex2 - vertex1;
            Vector3 ac = vertex3 - vertex1;
            Vector3 ap = point - vertex1;

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                result = vertex1; //Barycentric coordinates (1,0,0)
                return;
            }

            //Check if P in vertex region outside B
            Vector3 bp = point - vertex2;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                result = vertex2; // barycentric coordinates (0,1,0)
                return;
            }

            //Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                result = vertex1 + v * ab; //Barycentric coordinates (1-v,v,0)
                return;
            }

            //Check if P in vertex region outside C
            Vector3 cp = point - vertex3;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                result = vertex3; //Barycentric coordinates (0,0,1)
                return;
            }

            //Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                result = vertex1 + w * ac; //Barycentric coordinates (1-w,0,w)
                return;
            }

            //Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                result = vertex2 + w * (vertex3 - vertex2); //Barycentric coordinates (0,1-w,w)
                return;
            }

            //P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            float denom = 1.0f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            result = vertex1 + ab * v2 + ac * w2; //= u*vertex1 + v*vertex2 + w*vertex3, u = va * denom = 1.0f - v - w
        }

        /*void DrawTriangleSet(int[] triangles, Color color)
        {
            foreach (int triangle in triangles)
            {
                DebugDraw.DrawTriangle(vertices[tris[triangle]], vertices[tris[triangle + 1]], vertices[tris[triangle + 2]], color, transform);
            }
        }*/
    }
}
