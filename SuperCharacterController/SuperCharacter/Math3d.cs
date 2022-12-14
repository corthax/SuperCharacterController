using System;
using Stride.Core.Mathematics;

namespace SCC.Tools
{
    public partial struct Math3d
    {
        /*private static Math3d instance;
        public static Math3d Instance
        {
            get { return instance; }
            private set { instance ??= value; }
        }*/

        //private static Entity tempChild;
        //private static Entity tempParent;

        //[DataMemberIgnore]
        //public Simulation simulation;

        //public override void Start()
        //{
        //Instance = this;
        //simulation = this.GetSimulation();
        //Init();
        //}

        /*public static void Init()
        {
            tempChild = new Entity("Math3d_TempChild");
            tempParent = new Entity("Math3d_TempParent");

            //tempChild.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //DontDestroyOnLoad(tempChild.gameObject);

            //tempParent.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //DontDestroyOnLoad(tempParent.gameObject);

            //set the parent
            tempParent.AddChild(tempChild);
            tempParent.Scene = Instance.Entity.Scene;
        }*/

        /*public static float TimeSeconds => (float)Instance.Game.UpdateTime.Total.TotalSeconds;
        public static float DeltaTime => (float)Instance.Game.UpdateTime.Elapsed.TotalSeconds;
        // debug
        public static void LogError(string msg) => Instance.Log.Error(msg);
        public static void LogInfo(string msg) => Instance.Log.Info(msg);*/

        //increase or decrease the length of vector by size
        /*public static Vector3 AddVectorLength(Vector3 vector, float size)
        {
            //get the vector length
            float magnitude = vector.Length();

            //change the length
            magnitude += size;

            //normalize the vector
            Vector3 vectorNormalized = Vector3.Normalize(vector);

            //scale the vector
            return Vector3.Multiply(vectorNormalized, magnitude);
        }*/

        //create a vector of direction "vector" with length "size"
        public static Vector3 SetVectorLength(Vector3 vector, float size)
        {
            //normalize the vector
            Vector3 vectorNormalized = Vector3.Normalize(vector);

            //scale the vector
            return vectorNormalized *= size;
        }

        //caclulate the rotational difference from A to B
        /*public static Quaternion SubtractRotation(Quaternion B, Quaternion A)
        {
            Quaternion C = Quaternion.Invert(A) * B;
            return C;
        }*/

        //Find the line of intersection between two planes.	The planes are defined by a normal and a point on that plane.
        //The outputs are a point on the line and a vector which indicates it's direction. If the planes are not parallel, 
        //the function outputs true, otherwise false.
        /*public static bool PlanePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Vector3 plane1Normal, Vector3 plane1Position, Vector3 plane2Normal, Vector3 plane2Position)
        {
            linePoint = Vector3.Zero;

            //We can get the direction of the line of intersection of the two planes by calculating the 
            //cross product of the normals of the two planes. Note that this is just a direction and the line
            //is not fixed in space yet. We need a point for that to go with the line vector.
            lineVec = Vector3.Cross(plane1Normal, plane2Normal);

            //Next is to calculate a point on the line to fix it's position in space. This is done by finding a vector from
            //the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
            //errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
            //the cross product of the normal of plane2 and the lineDirection.		
            Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);

            float denominator = Vector3.Dot(plane1Normal, ldir);

            //Prevent divide by Zero and rounding errors by requiring about 5 degrees angle between the planes.
            if (Math.Abs(denominator) > 0.006f)
            {

                Vector3 plane1ToPlane2 = plane1Position - plane2Position;
                float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / denominator;
                linePoint = plane2Position + t * ldir;

                return true;
            }

            //output not valid
            else
            {
                return false;
            }
        }*/

        //Get the intersection between a line and a plane. 
        //If the line and plane are not parallel, the function outputs true, otherwise false.
        /*public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint)
        {
            float length;
            float dotNumerator;
            float dotDenominator;
            Vector3 vector;
            intersection = Vector3.Zero;

            //calculate the distance between the linePoint and the line-plane intersection point
            dotNumerator = Vector3.Dot(planePoint - linePoint, planeNormal);
            dotDenominator = Vector3.Dot(lineVec, planeNormal);

            //line and plane are not parallel
            if (dotDenominator != 0.0f)
            {
                length = dotNumerator / dotDenominator;

                //create a vector from the linePoint to the intersection point
                vector = SetVectorLength(lineVec, length);

                //get the coordinates of the line-plane intersection point
                intersection = linePoint + vector;

                return true;
            }

            //output not valid
            else
            {
                return false;
            }
        }*/

        //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
        //Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
        //same plane, use ClosestPointsOnTwoLines() instead.
        /*public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            intersection = Vector3.Zero;

            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //Lines are not coplanar. Take into account rounding errors.
            if ((planarFactor >= 0.00001f) || (planarFactor <= -0.00001f))
            {
                return false;
            }

            //Note: sqrMagnitude does x*x+y*y+z*z on the input vector.
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.LengthSquared();

            if ((s >= 0.0f) && (s <= 1.0f))
            {
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }

            else
            {
                return false;
            }
        }*/

        //Two non-parallel lines which may or may not touch each other have a point on each line which are closest
        //to each other. This function finds those two points. If the lines are not parallel, the function 
        //outputs true, otherwise false.
        /*public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            closestPointLine1 = Vector3.Zero;
            closestPointLine2 = Vector3.Zero;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            //lines are not parallel
            if (d != 0.0f)
            {
                Vector3 r = linePoint1 - linePoint2;
                float c = Vector3.Dot(lineVec1, r);
                float f = Vector3.Dot(lineVec2, r);

                float s = (b * f - c * e) / d;
                float t = (a * f - c * b) / d;

                closestPointLine1 = linePoint1 + lineVec1 * s;
                closestPointLine2 = linePoint2 + lineVec2 * t;

                return true;
            }

            else
            {
                return false;
            }
        }*/

        //This function returns a point which is a projection from a point to a line.
        //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
        public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
        {
            //get vector from point on line to point in space
            Vector3 linePointToPoint = point - linePoint;

            float t = Vector3.Dot(linePointToPoint, lineVec);

            return linePoint + lineVec * t;
        }

        //This function returns a point which is a projection from a point to a line segment.
        //If the projected point lies outside of the line segment, the projected point will 
        //be clamped to the appropriate line edge.
        //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
        /*public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        {
            Vector3 vector = linePoint2 - linePoint1;

            Vector3 projectedPoint = ProjectPointOnLine(linePoint1, Vector3.Normalize(vector), point);

            int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

            //The projected point is on the line segment
            return side switch
            {
                0 => projectedPoint,
                1 => linePoint1,
                2 => linePoint2,
                _ => Vector3.Zero, //output is invalid
            };
        }*/

        //This function returns a point which is a projection from a point to a plane.
        public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            float distance;
            Vector3 translationVector;

            //First calculate the distance from the point to the plane:
            distance = SignedDistancePlanePoint(planeNormal, planePoint, point);

            //Reverse the sign of the distance
            distance *= -1;

            //Get a translation vector
            translationVector = SetVectorLength(planeNormal, distance);

            //Translate the point to form a projection
            return point + translationVector;
        }

        //Projects a vector onto a plane. The output is not normalized.
        public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
        {
            return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
        }

        //Get the shortest distance between a point and a plane. The output is signed so it holds information
        //as to which side of the plane normal the point is.
        public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            return Vector3.Dot(planeNormal, point - planePoint);
        }

        //This function calculates a signed (+ or - sign instead of being ambiguous) dot product. It is basically used
        //to figure out whether a vector is positioned to the left or right of another vector. The way this is done is
        //by calculating a vector perpendicular to one of the vectors and using that as a reference. This is because
        //the result of a dot product only has signed information when an angle is transitioning between more or less
        //then 90 degrees.
        /*public static float SignedDotProduct(Vector3 vectorA, Vector3 vectorB, Vector3 normal)
        {
            Vector3 perpVector;
            float dot;

            //Use the geometry object normal and one of the input vectors to calculate the perpendicular vector
            perpVector = Vector3.Cross(normal, vectorA);

            //Now calculate the dot product between the perpendicular vector (perpVector) and the other input vector
            dot = Vector3.Dot(perpVector, vectorB);

            return dot;
        }*/

        /*public static float Vector3_Angle(Vector3 from, Vector3 to)
        {
            return MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(from), Vector3.Normalize(to)), -1f, 1f)) * 57.29578f;
        }*/

        /*public static float SignedVectorAngle(Vector3 referenceVector, Vector3 otherVector, Vector3 normal)
        {
            Vector3 perpVector;
            float angle;

            //Use the geometry object normal and one of the input vectors to calculate the perpendicular vector
            perpVector = Vector3.Cross(normal, referenceVector);

            //Now calculate the dot product between the perpendicular vector (perpVector) and the other input vector
            angle = DotProductAngle(referenceVector, otherVector);
            angle *= MathF.Sign(Vector3.Dot(perpVector, otherVector));

            return angle;
        }*/

        //Calculate the angle between a vector and a plane. The plane is made by a normal vector.
        //Output is in radians.
        /*public static float AngleVectorPlane(Vector3 vector, Vector3 normal)
        {
            float dot;
            float angle;

            //calculate the the dot product between the two input vectors. This gives the cosine between the two vectors
            dot = Vector3.Dot(vector, normal);

            //this is in radians
            angle = MathF.Acos(dot);

            return 1.570796326794897f - angle; //90 degrees - angle
        }*/

        /// <summary>
        /// Calculate the dot product as an angle in radians.
        /// </summary>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <returns></returns>
        public static float DotProductAngle(Vector3 vec1, Vector3 vec2)
        {
            float dot;
            float angle;

            //get the dot product
            dot = Vector3.Dot(vec1, vec2);

            //Clamp to prevent NaN error. Shouldn't need this in the first place, but there could be a rounding error issue.
            dot = Math.Clamp(dot, -1.0f, 1.0f);

            //Calculate the angle. The output is in radians
            //This step can be skipped for optimization...
            angle = MathF.Acos(dot);

            return angle;
        }

        /// <summary>
        /// Calculate and convert the dot product as an angle to degrees.
        /// </summary>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <returns></returns>
        public static float Angle(Vector3 vec1, Vector3 vec2)
        {
            return DotProductAngle(vec1, vec2) * Mathf.Rad2Deg;
        }

        //Convert a plane defined by 3 points to a plane defined by a vector and a point. 
        //The plane point is the middle of the triangle defined by the 3 points.
        /*public static void PlaneFrom3Points(out Vector3 planeNormal, out Vector3 planePoint, Vector3 pointA, Vector3 pointB, Vector3 pointC)
        {
            //Make two vectors from the 3 input points, originating from point A
            Vector3 AB = pointB - pointA;
            Vector3 AC = pointC - pointA;

            //Calculate the normal
            planeNormal = Vector3.Normalize(Vector3.Cross(AB, AC));

            //Get the points in the middle AB and AC
            Vector3 middleAB = pointA + (AB / 2.0f);
            Vector3 middleAC = pointA + (AC / 2.0f);

            //Get vectors from the middle of AB and AC to the point which is not on that line.
            Vector3 middleABtoC = pointC - middleAB;
            Vector3 middleACtoB = pointB - middleAC;

            //Calculate the intersection between the two lines. This will be the center 
            //of the triangle defined by the 3 points.
            //We could use LineLineIntersection instead of ClosestPointsOnTwoLines but due to rounding errors 
            //this sometimes doesn't work.
            ClosestPointsOnTwoLines(out planePoint, out Vector3 _, middleAB, middleABtoC, middleAC, middleACtoB);
        }*/

        //Returns the forward vector of a quaternion
        /*public static Vector3 GetForwardVector(Quaternion q)
        {
            return q * Vector3.UnitZ;
        }*/

        //Returns the up vector of a quaternion
        /*public static Vector3 GetUpVector(Quaternion q)
        {
            return q * Vector3.UnitY;
        }*/

        //Returns the right vector of a quaternion
        /*public static Vector3 GetRightVector(Quaternion q)
        {
            return q * Vector3.UnitX;
        }*/

        /*public static Vector4 Matrix4x4_GetColumn(Matrix m, int index)
        {
            return index switch
            {
                0 => new Vector4(m.M11, m.M21, m.M31, m.M41),
                1 => new Vector4(m.M12, m.M22, m.M32, m.M42),
                2 => new Vector4(m.M13, m.M23, m.M33, m.M43),
                3 => new Vector4(m.M14, m.M24, m.M34, m.M44),
                _ => throw new IndexOutOfRangeException($"Invalid column index! {index}"),
            };
        }*/

        //Gets a quaternion from a matrix
        /*public static Quaternion QuaternionFromMatrix(Matrix m)
        {
            var vF = (Vector3)Matrix4x4_GetColumn(m, 2);
            var vU = (Vector3)Matrix4x4_GetColumn(m, 1);
            return Quaternion.LookRotation(vF, vU);
        }*/

        //Gets a position from a matrix
        /*public static Vector3 PositionFromMatrix(Matrix m)
        {

            Vector4 vector4Position = Matrix4x4_GetColumn(m, 3);
            return new Vector3(vector4Position.X, vector4Position.Y, vector4Position.Z);
        }*/

        //This is an alternative for Quaternion.LookRotation. Instead of aligning the forward and up vector of the game 
        //object with the input vectors, a custom direction can be used instead of the fixed forward and up vectors.
        //alignWithVector and alignWithNormal are in world space.
        //customForward and customUp are in object space.
        //Usage: use alignWithVector and alignWithNormal as if you are using the default LookRotation function.
        //Set customForward and customUp to the vectors you wish to use instead of the default forward and up vectors.
        /*public static void LookRotationExtended(ref Entity gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal, Vector3 customForward, Vector3 customUp)
        {
            //Set the rotation of the destination
            Quaternion rotationA = Quaternion.LookRotation(alignWithVector, alignWithNormal);

            //Set the rotation of the custom normal and up vectors. 
            //When using the default LookRotation function, this would be hard coded to the forward and up vector.
            Quaternion rotationB = Quaternion.LookRotation(customForward, customUp);

            //Calculate the rotation
            gameObjectInOut.Transform.Rotation = rotationA * Quaternion.Invert(rotationB);
        }*/

        //This function transforms one object as if it was parented to the other.
        //Before using this function, the Init() function must be called
        //Input: parentRotation and parentPosition: the current parent transform.
        //Input: startParentRotation and startParentPosition: the transform of the parent object at the time the objects are parented.
        //Input: startChildRotation and startChildPosition: the transform of the child object at the time the objects are parented.
        //Output: childRotation and childPosition.
        //All transforms are in world space.
        /*private static void TransformWithParent(out Quaternion childRotation, out Vector3 childPosition, Quaternion parentRotation, Vector3 parentPosition, Quaternion startParentRotation, Vector3 startParentPosition, Quaternion startChildRotation, Vector3 startChildPosition)
        {
            //set the parent start transform
            tempParent.Transform.Rotation = startParentRotation;
            tempParent.Transform.Position = startParentPosition;
            tempParent.Transform.Scale = Vector3.One; //to prevent scale wandering

            //set the child start transform
            tempChild.Transform.Rotation = startChildRotation;
            tempChild.Transform.Position = startChildPosition;
            tempChild.Transform.Scale = Vector3.One; //to prevent scale wandering

            //translate and rotate the child by moving the parent
            tempParent.Transform.Rotation = parentRotation;
            tempParent.Transform.Position = parentPosition;

            //get the child transform
            childRotation = tempChild.Transform.Rotation;
            childPosition = tempChild.Transform.Position;
        }*/

        //With this function you can align a triangle of an object with any transform.
        //Usage: gameObjectInOut is the game object you want to transform.
        //alignWithVector, alignWithNormal, and alignWithPosition is the transform with which the triangle of the object should be aligned with.
        //triangleForward, triangleNormal, and trianglePosition is the transform of the triangle from the object.
        //alignWithVector, alignWithNormal, and alignWithPosition are in world space.
        //triangleForward, triangleNormal, and trianglePosition are in object space.
        //trianglePosition is the mesh position of the triangle. The effect of the scale of the object is handled automatically.
        //trianglePosition can be set at any position, it does not have to be at a vertex or in the middle of the triangle.
        /*public static void PreciseAlign(ref Entity gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal, Vector3 alignWithPosition, Vector3 triangleForward, Vector3 triangleNormal, Vector3 trianglePosition)
        {
            //Set the rotation.
            LookRotationExtended(ref gameObjectInOut, alignWithVector, alignWithNormal, triangleForward, triangleNormal);

            //Get the world space position of trianglePosition
            Vector3 trianglePositionWorld = gameObjectInOut.Transform.LocalToWorld(trianglePosition);

            //Get a vector from trianglePosition to alignWithPosition
            Vector3 translateVector = alignWithPosition - trianglePositionWorld;

            //Now transform the object so the triangle lines up correctly.
            //gameObjectInOut.Transform.Translate(translateVector, Space.World);
            gameObjectInOut.Transform.Position += translateVector;
        }*/


        //Convert a position, direction, and normal vector to a transform
        /*void VectorsToTransform(ref Entity gameObjectInOut, Vector3 positionVector, Vector3 directionVector, Vector3 normalVector)
        {
            gameObjectInOut.Transform.Position = positionVector;
            gameObjectInOut.Transform.Rotation = Quaternion.LookRotation(directionVector, normalVector);
        }*/

        //This function finds out on which side of a line segment the point is located.
        //The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
        //the line segment, project it on the line using ProjectPointOnLine() first.
        //Returns 0 if point is on the line segment.
        //Returns 1 if point is outside of the line segment and located on the side of linePoint1.
        //Returns 2 if point is outside of the line segment and located on the side of linePoint2.
        /*public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        {
            Vector3 lineVec = linePoint2 - linePoint1;
            Vector3 pointVec = point - linePoint1;

            float dot = Vector3.Dot(pointVec, lineVec);

            //point is on side of linePoint2, compared to linePoint1
            if (dot > 0)
            {
                //point is on the line segment
                if (pointVec.Length() <= lineVec.Length())
                {
                    return 0;
                }

                //point is not on the line segment and it is on the side of linePoint2
                else
                {
                    return 2;
                }
            }

            //Point is not on side of linePoint2, compared to linePoint1.
            //Point is not on the line segment and it is on the side of linePoint1.
            else
            {
                return 1;
            }
        }*/

        //Returns the pixel distance from the mouse pointer to a line.
        //Alternative for HandleUtility.DistanceToLine(). Works both in Editor mode and Play mode.
        //Do not call this function from OnGUI() as the mouse position will be wrong.
        /*public static float MouseDistanceToLine(Vector3 linePoint1, Vector3 linePoint2)
        {

            CameraComponent currentCamera;
            Vector3 mousePosition;


            Vector3 screenPos1 = currentCamera.WorldToScreenPoint(linePoint1);
            Vector3 screenPos2 = currentCamera.WorldToScreenPoint(linePoint2);
            Vector3 projectedPoint = ProjectPointOnLineSegment(screenPos1, screenPos2, mousePosition);

            //set z to Zero
            projectedPoint = new Vector3(projectedPoint.X, projectedPoint.Y, 0f);

            Vector3 vector = projectedPoint - mousePosition;
            return vector.Magnitude;
        }*/


        //Returns the pixel distance from the mouse pointer to a camera facing circle.
        //Alternative for HandleUtility.DistanceToCircle(). Works both in Editor mode and Play mode.
        //Do not call this function from OnGUI() as the mouse position will be wrong.
        //If you want the distance to a point instead of a circle, set the radius to 0.
        /*public static float MouseDistanceToCircle(Vector3 point, float radius)
        {

            Camera currentCamera;
            Vector3 mousePosition;

            Vector3 screenPos = currentCamera.WorldToScreenPoint(point);

            //set z to Zero
            screenPos = new Vector3(screenPos.X, screenPos.Y, 0f);

            Vector3 vector = screenPos - mousePosition;
            float fullDistance = vector.Magnitude;
            float circleDistance = fullDistance - radius;

            return circleDistance;
        }*/

        //Returns true if a line segment (made up of linePoint1 and linePoint2) is fully or partially in a rectangle
        //made up of RectA to RectD. The line segment is assumed to be on the same plane as the rectangle. If the line is 
        //not on the plane, use ProjectPointOnPlane() on linePoint1 and linePoint2 first.
        /*public static bool IsLineInRectangle(Vector3 linePoint1, Vector3 linePoint2, Vector3 rectA, Vector3 rectB, Vector3 rectC, Vector3 rectD)
        {
            bool pointAInside;
            bool pointBInside = false;

            pointAInside = IsPointInRectangle(linePoint1, rectA, rectC, rectB, rectD);

            if (!pointAInside)
            {
                pointBInside = IsPointInRectangle(linePoint2, rectA, rectC, rectB, rectD);
            }

            //none of the points are inside, so check if a line is crossing
            if (!pointAInside && !pointBInside)
            {
                bool lineACrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectA, rectB);
                bool lineBCrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectB, rectC);
                bool lineCCrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectC, rectD);
                bool lineDCrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectD, rectA);

                if (lineACrossing || lineBCrossing || lineCCrossing || lineDCrossing)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }*/

        //Returns true if "point" is in a rectangle mad up of RectA to RectD. The line point is assumed to be on the same 
        //plane as the rectangle. If the point is not on the plane, use ProjectPointOnPlane() first.
        /*public static bool IsPointInRectangle(Vector3 point, Vector3 rectA, Vector3 rectC, Vector3 rectB, Vector3 rectD)
        {
            Vector3 vector;
            Vector3 linePoint;

            //get the center of the rectangle
            vector = rectC - rectA;
            float size = -(vector.Length() / 2f);
            vector = AddVectorLength(vector, size);
            Vector3 middle = rectA + vector;

            Vector3 xVector = rectB - rectA;
            float width = xVector.Length() / 2f;

            Vector3 yVector = rectD - rectA;
            float height = yVector.Length() / 2f;

            linePoint = ProjectPointOnLine(middle, Vector3.Normalize(xVector), point);
            vector = linePoint - point;
            float yDistance = vector.Length();

            linePoint = ProjectPointOnLine(middle, Vector3.Normalize(yVector), point);
            vector = linePoint - point;
            float xDistance = vector.Length();

            return (xDistance <= width) && (yDistance <= height);
        }*/

        //Returns true if line segment made up of pointA1 and pointA2 is crossing line segment made up of
        //pointB1 and pointB2. The two lines are assumed to be in the same plane.
        /*public static bool AreLineSegmentsCrossing(Vector3 pointA1, Vector3 pointA2, Vector3 pointB1, Vector3 pointB2)
        {
            int sideA;
            int sideB;

            Vector3 lineVecA = pointA2 - pointA1;
            Vector3 lineVecB = pointB2 - pointB1;

            bool valid = ClosestPointsOnTwoLines(out Vector3 closestPointA, out Vector3 closestPointB, pointA1, Vector3.Normalize(lineVecA), pointB1, Vector3.Normalize(lineVecB));

            //lines are not parallel
            if (valid)
            {
                sideA = PointOnWhichSideOfLineSegment(pointA1, pointA2, closestPointA);
                sideB = PointOnWhichSideOfLineSegment(pointB1, pointB2, closestPointB);

                return (sideA == 0) && (sideB == 0);
            }
            //lines are parallel
            else
            {
                return false;
            }
        }*/

        // matrix math

        /// <summary>
        /// Creates a rotation matrix. Assumes unit quaternion.
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        /*public static Matrix Matrix_Rotation(Quaternion rotation)
        {
            // Precalculate coordinate products
            float x = rotation.X * 2F;
            float y = rotation.Y * 2F;
            float z = rotation.Z * 2F;
            float xx = rotation.X * x;
            float yy = rotation.Y * y;
            float zz = rotation.Z * z;
            float xy = rotation.X * y;
            float xz = rotation.X * z;
            float yz = rotation.Y * z;
            float wx = rotation.W * x;
            float wy = rotation.W * y;
            float wz = rotation.W * z;

            // Calculate 3x3 matrix from orthonormal basis
            Matrix m = new();
            m.M11 = 1F - (yy + zz);     m.M21 = xy + wz;            m.M31 = xz - wy;            m.M41 = 0F;
            m.M12 = xy - wz;            m.M22 = 1F - (xx + zz);     m.M32 = yz + wx;            m.M42 = 0F;
            m.M13 = xz + wy;            m.M23 = yz - wx;            m.M33 = 1F - (xx + yy);     m.M43 = 0F;
            m.M14 = 0F;                 m.M24 = 0F;                 m.M34 = 0F;                 m.M44 = 1F;
            return m;
        }*/

        /// <summary>
        /// Creates a translation matrix.
        /// </summary>
        /// <param name="translation"></param>
        /// <returns></returns>
        /*public static Matrix Matrix_Translation(Vector3 translation)
        {
            Matrix m = new();
            m.M11 = 1F; m.M12 = 0F; m.M13 = 0F; m.M14 = translation.X;
            m.M21 = 0F; m.M22 = 1F; m.M23 = 0F; m.M24 = translation.Y;
            m.M31 = 0F; m.M32 = 0F; m.M33 = 1F; m.M34 = translation.Z;
            m.M41 = 0F; m.M42 = 0F; m.M43 = 0F; m.M44 = 1F;
            return m;
        }*/

        /// <summary>
        /// Creates a scaling matrix.
        /// </summary>
        /// <param name="scaling"></param>
        /// <returns></returns>
        /*public static Matrix Matrix_Scale(Vector3 scaling)
        {
            Matrix m = new();
            m.M11 = scaling.X;  m.M12 = 0F;         m.M13 = 0F;         m.M14 = 0F;
            m.M21 = 0F;         m.M22 = scaling.Y;  m.M23 = 0F;         m.M24 = 0F;
            m.M31 = 0F;         m.M32 = 0F;         m.M33 = scaling.Z;  m.M34 = 0F;
            m.M41 = 0F;         m.M42 = 0F;         m.M43 = 0F;         m.M44 = 1F;
            return m;
        }*/

        /// <summary>
        /// Transforms a direction by matrix.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        /*public static Vector3 Matrix_MultiplyVector(Matrix matrix, Vector3 vector)
        {
            Vector3 res;
            res.X = matrix.M11 * vector.X + matrix.M12 * vector.Y + matrix.M13 * vector.Z;
            res.Y = matrix.M21 * vector.X + matrix.M22 * vector.Y + matrix.M23 * vector.Z;
            res.Z = matrix.M31 * vector.X + matrix.M32 * vector.Y + matrix.M33 * vector.Z;
            return res;
        }*/

        /// <summary>
        /// Transforms a position by matrix, without a perspective divide. (fast)
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        /*public static Vector3 Matrix_MultiplyPoint3x4(Matrix matrix, Vector3 point)
        {
            Vector3 res;
            res.X = matrix.M11 * point.X + matrix.M12 * point.Y + matrix.M13 * point.Z + matrix.M14;
            res.Y = matrix.M21 * point.X + matrix.M22 * point.Y + matrix.M23 * point.Z + matrix.M24;
            res.Z = matrix.M31 * point.X + matrix.M32 * point.Y + matrix.M33 * point.Z + matrix.M34;
            return res;
        }*/

        /// <summary>
        /// Transforms a position by matrix, with a perspective divide. (generic)
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        /*public static Vector3 Matrix_MultiplyPoint(Matrix matrix, Vector3 point)
        {
            Vector3 res;
            float w;
            res.X = matrix.M11 * point.X + matrix.M12 * point.Y + matrix.M13 * point.Z + matrix.M14;
            res.Y = matrix.M21 * point.X + matrix.M22 * point.Y + matrix.M23 * point.Z + matrix.M24;
            res.Z = matrix.M31 * point.X + matrix.M32 * point.Y + matrix.M33 * point.Z + matrix.M34;
                w = matrix.M41 * point.X + matrix.M42 * point.Y + matrix.M43 * point.Z + matrix.M44;

            w = 1F / w;
            res.X *= w;
            res.Y *= w;
            res.Z *= w;
            return res;
        }*/

        /// <summary>
        /// Returns position from matrix.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        /*public static Vector3 Matrix_GetPosition(Matrix matrix)
        {
            return new Vector3(matrix.M14, matrix.M24, matrix.M34);
        }*/

        /// <summary>
        /// Transforms a plane by matrix.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        /*public static Plane Matrix_TransformPlane(Matrix matrix, Plane plane)
        {
            var ittrans = Matrix.Invert(matrix);

            float x = plane.Normal.X, y = plane.Normal.Y, z = plane.Normal.Z, w = plane.D;
            // note: a transpose is part of this transformation
            var a = ittrans.M11 * x + ittrans.M21 * y + ittrans.M31 * z + ittrans.M41 * w;
            var b = ittrans.M12 * x + ittrans.M22 * y + ittrans.M32 * z + ittrans.M42 * w;
            var c = ittrans.M13 * x + ittrans.M23 * y + ittrans.M33 * z + ittrans.M43 * w;
            var d = ittrans.M14 * x + ittrans.M24 * y + ittrans.M34 * z + ittrans.M44 * w;

            return new Plane(new Vector3(a, b, c), d);
        }*/

        /// <summary>
        /// Returns entity's transform TRS matrix.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /*public static Matrix Matrix_TRS(Entity entity)
        {
            var t = Matrix_Translation(entity.Transform.Position);
            var r = Matrix_Rotation(entity.Transform.Rotation);
            var s = Matrix_Scale(entity.Transform.Scale);
            return t * r * s;
        }*/

        /// <summary>
        /// Returns entity's transform inverse TRS matrix.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /*public static Matrix Matrix_TRS_Inverse(Entity entity)
        {
            return Matrix.Invert(Matrix_TRS(entity));
        }*/
    }
}