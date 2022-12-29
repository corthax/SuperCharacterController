using Stride.Core.Mathematics;
using System;

namespace SCC.GameTools
{
    public partial struct Mathf
    {
        public static float Min(float a, float b) { return a < b ? a : b; }
        /// <summary>
        /// Returns the smallest of two or more values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Min(params float[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            float m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] < m)
                    m = values[i];
            }
            return m;
        }

        public static int Min(int a, int b) { return a < b ? a : b; }
        /// <summary>
        /// Returns the smallest of two or more values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static int Min(params int[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            int m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] < m)
                    m = values[i];
            }
            return m;
        }

        public static float Max(float a, float b) { return a > b ? a : b; }
        /// <summary>
        /// Returns largest of two or more values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Max(params float[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            float m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] > m)
                    m = values[i];
            }
            return m;
        }

        public static int Max(int a, int b) { return a > b ? a : b; }
        /// <summary>
        /// Returns the largest of two or more values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static int Max(params int[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            int m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] > m)
                    m = values[i];
            }
            return m;
        }

        /// <summary>
        /// Returns the smallest integer greater to or equal to /f/.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }

        /// <summary>
        /// Returns the largest integer smaller to or equal to /f/.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }

        /// <summary>
        /// Returns /f/ rounded to the nearest integer.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static int RoundToInt(float f) { return (int)Math.Round(f); }

        /// <summary>
        /// Returns the sign of /f/.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Sign(float f) { return f >= 0F ? 1F : -1F; }

        /// <summary>
        /// Degrees-to-radians conversion constant (RO).
        /// </summary>
        public const float Deg2Rad = MathF.PI * 2F / 360F;

        /// <summary>
        /// Radians-to-degrees conversion constant (RO).
        /// </summary>
        public const float Rad2Deg = 1F / Deg2Rad;

        // We cannot round to more decimals than 15 according to docs for System.Math.Round.
        internal const int kMaxDecimals = 15;

        // Clamps a value between a minimum float and maximum float value.
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        // Clamps value between min and max and returns value.
        // Set the position of the transform to be that of the time
        // but never less than 1 or more than 3
        //
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        // Clamps value between 0 and 1 and returns value
        public static float Clamp01(float value)
        {
            if (value < 0F)
                return 0F;
            else if (value > 1F)
                return 1F;
            else
                return value;
        }

        /// <summary>
        /// Interpolates between /a/ and /b/ by /t/. /t/ is clamped between 0 and 1.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpClamped(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        /// <summary>
        /// Interpolates between /a/ and /b/ by /t/ without clamping the interpolant.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpUnclamped(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Same as ::ref::Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat((b - a), 360);
            if (delta > 180)
                delta -= 360;
            return a + delta * Clamp01(t);
        }

        /// <summary>
        /// Moves a value /current/ towards /target/.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="maxDelta"></param>
        /// <returns></returns>
        static public float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;
            return current + Sign(target - current) * maxDelta;
        }

        /// <summary>
        /// Same as ::ref::MoveTowards but makes sure the values interpolate correctly when they wrap around 360 degrees.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="maxDelta"></param>
        /// <returns></returns>
        static public float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float deltaAngle = DeltaAngle(current, target);
            if (-maxDelta < deltaAngle && deltaAngle < maxDelta)
                return target;
            target = current + deltaAngle;
            return MoveTowards(current, target, maxDelta);
        }

        /// <summary>
        /// Interpolates between /min/ and /max/ with smoothing at the limits.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2.0F * t * t * t + 3.0F * t * t;
            return to * t + from * (1F - t);
        }

        /// <summary>
        /// *undocumented
        /// </summary>
        /// <param name="value"></param>
        /// <param name="absmax"></param>
        /// <param name="gamma"></param>
        /// <returns></returns>
        public static float Gamma(float value, float absmax, float gamma)
        {
            bool negative = value < 0F;
            float absval = MathF.Abs(value);
            if (absval > absmax)
                return negative ? -absval : absval;

            float result = MathF.Pow(absval / absmax, gamma) * absmax;
            return negative ? -result : result;
        }

        /// <summary>
        /// Compares two floating point values if they are similar.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Approximately(float a, float b)
        {
            // If a or b is zero, compare that the other is less or equal to epsilon.
            // If neither a or b are 0, then find an epsilon that is good for
            // comparing numbers at the maximum magnitude of a and b.
            // Floating points have about 7 significant digits, so
            // 1.000001f can be represented while 1.0000001f is rounded to zero,
            // thus we could use an epsilon of 0.000001f for comparing values close to 1.
            // We multiply this epsilon by the biggest magnitude of a and b.
            return MathF.Abs(b - a) < Max(0.000001f * Max(MathF.Abs(a), MathF.Abs(b)), float.Epsilon * 8);
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed)
        {
            float deltaTime = Time.DeltaTime;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime)
        {
            float deltaTime = Time.DeltaTime;
            float maxSpeed = float.PositiveInfinity;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Gradually changes a value towards a desired goal over time.
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
            float change = current - target;
            float originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;

            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            // Prevent overshooting
            if (originalTo - current > 0.0F == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }


        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed)
        {
            float deltaTime = Time.DeltaTime;
            return SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
        {
            float deltaTime = Time.DeltaTime;
            float maxSpeed = float.PositiveInfinity;
            return SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Gradually changes an angle given in degrees towards a desired goal angle over time.
        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Loops the value t, so that it is never larger than length and never smaller than 0.
        public static float Repeat(float t, float length)
        {
            return Clamp(t - MathF.Floor(t / length) * length, 0.0f, length);
        }

        // PingPongs the value t, so that it is never larger than length and never smaller than 0.
        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2F);
            return length - MathF.Abs(t - length);
        }

        // Calculates the ::ref::Lerp parameter between of two values.
        public static float InverseLerp(float a, float b, float value)
        {
            if (a != b)
                return Clamp01((value - a) / (b - a));
            else
                return 0.0f;
        }

        // Calculates the shortest difference between two given angles.
        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat((target - current), 360.0F);
            if (delta > 180.0F)
                delta -= 360.0F;
            return delta;
        }

        // Infinite Line Intersection (line1 is p1-p2 and line2 is p3-p4)
        internal static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
        {
            float bx = p2.X - p1.X;
            float by = p2.Y - p1.Y;
            float dx = p4.X - p3.X;
            float dy = p4.Y - p3.Y;
            float bDotDPerp = bx * dy - by * dx;
            if (bDotDPerp == 0)
            {
                return false;
            }
            float cx = p3.X - p1.X;
            float cy = p3.Y - p1.Y;
            float t = (cx * dy - cy * dx) / bDotDPerp;

            result.X = p1.X + t * bx;
            result.Y = p1.Y + t * by;
            return true;
        }

        // Line Segment Intersection (line1 is p1-p2 and line2 is p3-p4)
        internal static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
        {
            float bx = p2.X - p1.X;
            float by = p2.Y - p1.Y;
            float dx = p4.X - p3.X;
            float dy = p4.Y - p3.Y;
            float bDotDPerp = bx * dy - by * dx;
            if (bDotDPerp == 0)
            {
                return false;
            }
            float cx = p3.X - p1.X;
            float cy = p3.Y - p1.Y;
            float t = (cx * dy - cy * dx) / bDotDPerp;
            if (t < 0 || t > 1)
            {
                return false;
            }
            float u = (cx * by - cy * bx) / bDotDPerp;
            if (u < 0 || u > 1)
            {
                return false;
            }

            result.X = p1.X + t * bx;
            result.Y = p1.Y + t * by;
            return true;
        }

        static internal long RandomToLong(Random r)
        {
            var buffer = new byte[8];
            r.NextBytes(buffer);
            return (long)(BitConverter.ToUInt64(buffer, 0) & long.MaxValue);
        }

        internal static float ClampToFloat(double value)
        {
            if (double.IsPositiveInfinity(value))
                return float.PositiveInfinity;

            if (double.IsNegativeInfinity(value))
                return float.NegativeInfinity;

            if (value < float.MinValue)
                return float.MinValue;

            if (value > float.MaxValue)
                return float.MaxValue;

            return (float)value;
        }

        internal static int ClampToInt(long value)
        {
            if (value < int.MinValue)
                return int.MinValue;

            if (value > int.MaxValue)
                return int.MaxValue;

            return (int)value;
        }

        internal static float RoundToMultipleOf(float value, float roundingValue)
        {
            if (roundingValue == 0)
                return value;
            return MathF.Round(value / roundingValue) * roundingValue;
        }

        internal static float GetClosestPowerOfTen(float positiveNumber)
        {
            if (positiveNumber <= 0)
                return 1;
            return MathF.Pow(10, RoundToInt(MathF.Log10(positiveNumber)));
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
        {
            return Clamp(-FloorToInt(MathF.Log10(MathF.Abs(minDifference))), 0, kMaxDecimals);
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(double minDifference)
        {
            return (int)Math.Max(0.0, -Math.Floor(Math.Log10(Math.Abs(minDifference))));
        }

        internal static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
        {
            if (minDifference == 0)
                return DiscardLeastSignificantDecimal(valueToRound);
            return (float)Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference),
                MidpointRounding.AwayFromZero);
        }

        internal static double RoundBasedOnMinimumDifference(double valueToRound, double minDifference)
        {
            if (minDifference == 0)
                return DiscardLeastSignificantDecimal(valueToRound);
            return Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference),
                MidpointRounding.AwayFromZero);
        }

        internal static float DiscardLeastSignificantDecimal(float v)
        {
            int decimals = Clamp((int)(5 - MathF.Log10(MathF.Abs(v))), 0, kMaxDecimals);
            return (float)Math.Round(v, decimals, MidpointRounding.AwayFromZero);
        }

        internal static double DiscardLeastSignificantDecimal(double v)
        {
            int decimals = Math.Max(0, (int)(5 - Math.Log10(Math.Abs(v))));
            try
            {
                return Math.Round(v, decimals);
            }
            catch (ArgumentOutOfRangeException)
            {
                // This can happen for very small numbers.
                return 0;
            }
        }

        //

        /// <summary>
        /// Calculate the dot product as an angle.
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
        /// Return an angle between vec1 and vec2 in degrees.
        /// </summary>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <returns></returns>
        public static float Vector3_Angle(Vector3 vec1, Vector3 vec2)
        {
            return DotProductAngle(vec1, vec2) * Mathf.Rad2Deg;
        }

        private static bool IsEqualUsingDot(float dot)
        {
            // Returns false in the presence of NaN values.
            return dot > 1.0f - 0.000001F;
        }

        public static float Angle(Quaternion a, Quaternion b)
        {
            float dot = Min(MathF.Abs(Quaternion.Dot(a, b)), 1.0F);
            return IsEqualUsingDot(dot) ? 0.0f : MathF.Acos(dot) * 2.0F * Rad2Deg;
        }

        /// <summary>
        /// Return angle in degrees. 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float Angle(Vector3 from, Vector3 to)
        {
            return Angle(from, to, true) * Rad2Deg;
        }

        /// <summary>
        /// Return angle in radians.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static float Angle(Vector3 from, Vector3 to, bool radians)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = MathF.Sqrt(from.LengthSquared() * to.LengthSquared());
            if (denominator < 1e-15F)
                return 0F;

            float dot = Clamp(Vector3.Dot(from, to) / denominator, -1F, 1F);
            return MathF.Acos(dot);
        }

        /// <summary>
        /// Quaternion from angle and axis,
        /// </summary>
        /// <param name="aAngle"></param>
        /// <param name="aAxis"></param>
        /// <returns></returns>
        public static Quaternion AngleAxis(float aAngle, Vector3 aAxis)
        {
            aAxis = Vector3.Normalize(aAxis);
            float rad = aAngle * Deg2Rad * 0.5f;
            aAxis *= MathF.Sin(rad);
            return new Quaternion(aAxis.X, aAxis.Y, aAxis.Z, MathF.Cos(rad));
        }

        public static Quaternion FromToRotation(Vector3 aFrom, Vector3 aTo)
        {
            Vector3 axis = Vector3.Cross(aFrom, aTo);
            float angle = Angle(aFrom, aTo);
            return AngleAxis(angle, Vector3.Normalize(axis));
        }
    }
}
