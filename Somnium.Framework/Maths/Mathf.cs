﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    public static class Mathf
    {
        public const int MatrixSize = sizeof(float) * 16;
        public const int Vector4Size = sizeof(float) * 4;

        public static readonly Vector3 Vector3Forward = new Vector3(0f, 0f, -1f);
        public static readonly Vector3 Vector3Backward = new Vector3(0f, 0f, 1f);

        public const float DegreeRadian = (float)(Math.PI / 180f);
        public const float RadianDegree = (float)(180f / Math.PI);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WithinEpsilon(float a, float b)
        {
            float num = a - b;
            return ((-1.401298E-45f <= num) && (num <= float.Epsilon));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane PlaneFrom(Vector3 pointOnPlane, Vector3 normal)
        {
            float d = -(
                pointOnPlane.X * normal.X +
                pointOnPlane.Y * normal.Y +
                pointOnPlane.Z * normal.Z
            );
            return new Plane(normal, d);
        }
        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <param name="amount">Weighting value.</param>
        /// <returns>Interpolated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothStep(float value1, float value2, float amount)
        {
            float result = Mathf.Clamp(amount, 0f, 1f);
            result = Mathf.Hermite(value1, 0f, value2, 0f, result);

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MulT(float sin, float cos, Vector2 vectorToRotate)
        {
            return new Vector2(cos * vectorToRotate.X + sin * vectorToRotate.Y, -sin * vectorToRotate.X + cos * vectorToRotate.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 tripleProduct(Vector2 A, Vector2 B, Vector2 C)
        {
            float z = A.X * B.Y - A.Y * B.X;
            return new Vector2(-C.Y * z, C.X * z);//[-cy * z, cx * z];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ResizeAndFit(float Width, float Height, float MaxWidth, float MaxHeight, Vector2 originalScale)
        {
            float width = Width * originalScale.X; //Width of item itself
            float height = Height * originalScale.Y; //Width of item itself

            if (width > MaxWidth)
            {
                float multiplier = (float)MaxWidth / (float)width;//(float)width / (float)MaxWidth;
                originalScale.X *= multiplier;
                originalScale.Y *= multiplier;
                width = Width * originalScale.X;
                height = Height * originalScale.Y;
            }
            if (height > MaxHeight)
            {
                float multiplier = (float)MaxHeight / (float)height;//(float)width / (float)MaxWidth;
                originalScale.X *= multiplier;
                originalScale.Y *= multiplier;
            }

            return originalScale;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ProjectUntoAxis(Vector2 thisPoint, float axisRightAngle)
        {
            Vector2 axis = Mathf.lengthdir(1f, axisRightAngle);
            float x = Vector2.Dot(thisPoint, axis);
            axis = Mathf.lengthdir(1f, axisRightAngle + (90f * Mathf.DegreeRadian));
            float y = Vector2.Dot(thisPoint, axis);

            return new Vector2(x, y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateAbout(Vector2 thisPoint, Vector2 pivot, float s, float c)
        {
            if (thisPoint == pivot)
                return thisPoint;
            // translate point back to origin:
            thisPoint -= pivot;

            // rotate point
            float xnew = thisPoint.X * c - thisPoint.Y * s;
            float ynew = thisPoint.X * s + thisPoint.Y * c;

            // translate point back:
            thisPoint = new Vector2(xnew + pivot.X, ynew + pivot.Y);
            //thisPoint += new Vector2(xnew + pivot.X, ynew + pivot.Y);
            return thisPoint;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateAbout(Vector2 thisPoint, Vector2 pivot, float angle)
        {
            if (thisPoint == pivot)
                return thisPoint;

            float s = Sin(angle);
            float c = Cos(angle);
            return RotateAbout(thisPoint, pivot, s, c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Abs(Vector2 vec)
        {
            return new Vector2(Math.Abs(vec.X), Math.Abs(vec.Y));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b)
        {
            return a < b ? a : b;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// Returns the remainder of a / b, wrapped around b if it is negative
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ModWrapped(int a, int b)
        {
            return a - b * Floor(a / (float)b);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ModWrapped(float a, float b)
        {
            return a - Floor(a / b) * b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float val) => (float)Math.Abs(val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float radians) => (float)Math.Sin(radians);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float radians) => (float)Math.Cos(radians);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float radians) => (float)Math.Tan(radians);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Floor(float value) => (int)Math.Floor(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Round(float value) => (int)Math.Round(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Floor(Vector2 vector)
        {
            return new Vector2(Floor(vector.X), Floor(vector.Y));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Round(Vector2 vector)
        {
            return new Vector2(Round(vector.X), Round(vector.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ceiling(float value) => (int)Math.Ceiling((float)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Ceiling(Vector2 vector)
        {
            return new Vector2(Ceiling(vector.X), Ceiling(vector.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthdirX(float length, float dirInRadians)
        {
            return (float)(Math.Cos(dirInRadians) * length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthdirY(float length, float dirInRadians)
        {
            return (float)(Math.Sin(dirInRadians) * length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 lengthdir(float length, float dirInRadians)
        {
            return new Vector2(lengthdirX(length, dirInRadians), lengthdirY(length, dirInRadians));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float getSpeed(float velocityX, float velocityY)
        {
            return (float)Math.Sqrt((velocityX * velocityX) + (velocityY * velocityY));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float getSpeed(Vector2 velocity)
        {
            return (float)Math.Sqrt((velocity.X * velocity.X) + (velocity.Y * velocity.Y));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PointDirection(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Atan2(y2 - y1, x2 - x1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PointDirection(Vector2 pos1, Vector2 pos2)
        {
            return PointDirection(pos1.X, pos1.Y, pos2.X, pos2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuadraticStep(float valueA, float valueB, float amount)
        {
            float equation = Sign(valueB - valueA) * amount * amount + (valueB < valueA ? 1f : 0f);
            return valueA + (valueB - valueA) * equation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float val) => (float)Math.Sqrt(val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(float val) => Math.Sign(val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 getGlidingVelocityTowards(Vector2 currentPosition, Vector2 currentVelocity, Vector2 targetPosition, float maxSpeed, float turnSpeed = 20f)
        {
            
            Vector2 totalDistanceToCover = targetPosition - currentPosition;
            if (getSpeed(totalDistanceToCover) > maxSpeed)
            {
                totalDistanceToCover *= maxSpeed / getSpeed(totalDistanceToCover);
            }
            return (turnSpeed * currentVelocity + totalDistanceToCover) / (turnSpeed + 1f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Point pos1, Point pos2)
        {
            return Distance(pos1.X, pos1.Y, pos2.X, pos2.Y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float moveTowards(float currentPosition, float targetPosition, float currentVelocity, float maxVelocity, float acceleration, float decceleration = 0f)
        {
            if (currentPosition < targetPosition)
            {
                if (currentVelocity < maxVelocity)
                {
                    currentVelocity += acceleration;
                }
                else if (currentVelocity > maxVelocity)
                {
                    currentVelocity -= decceleration;
                }
            }
            else if (currentPosition > targetPosition)
            {
                if (currentVelocity > -maxVelocity)
                {
                    currentVelocity -= acceleration;
                }
                else if (currentVelocity < maxVelocity)
                {
                    currentVelocity += acceleration;
                }
            }
            return currentVelocity;
        }
        /// <summary>
        /// Returns the Cartesian coordinate for one axis of a point that is defined by a given triangle and two normalized barycentric (areal) coordinates.
        /// </summary>
        /// <param name="value1">The coordinate on one axis of vertex 1 of the defining triangle.</param>
        /// <param name="value2">The coordinate on the same axis of vertex 2 of the defining triangle.</param>
        /// <param name="value3">The coordinate on the same axis of vertex 3 of the defining triangle.</param>
        /// <param name="amount1">The normalized barycentric (areal) coordinate b2, equal to the weighting factor for vertex 2, the coordinate of which is specified in value2.</param>
        /// <param name="amount2">The normalized barycentric (areal) coordinate b3, equal to the weighting factor for vertex 3, the coordinate of which is specified in value3.</param>
        /// <returns>Cartesian coordinate of the specified point with respect to the axis being used.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
        {
            return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
        }
        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>A position that is the result of the Catmull-Rom interpolation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
        {
            // Using formula from http://www.mvps.org/directx/articles/catmull/
            // Internally using doubles not to lose precission
            double amountSquared = amount * amount;
            double amountCubed = amountSquared * amount;
            return (float)(0.5 * (2.0 * value2 +
                (value3 - value1) * amount +
                (2.0 * value1 - 5.0 * value2 + 4.0 * value3 - value4) * amountSquared +
                (3.0 * value2 - value1 - 3.0 * value3 + value4) * amountCubed));
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value. If <c>value</c> is less than <c>min</c>, <c>min</c> will be returned.</param>
        /// <param name="max">The maximum value. If <c>value</c> is greater than <c>max</c>, <c>max</c> will be returned.</param>
        /// <returns>The clamped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            // First we check to see if we're greater than the max
            value = (value > max) ? max : value;

            // Then we check to see if we're less than the min.
            value = (value < min) ? min : value;

            // There's no check to see if min > max.
            return value;
        }
        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value. If <c>value</c> is less than <c>min</c>, <c>min</c> will be returned.</param>
        /// <param name="max">The maximum value. If <c>value</c> is greater than <c>max</c>, <c>max</c> will be returned.</param>
        /// <returns>The clamped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        /// <summary>
        /// Calculates the absolute value of the difference of two values.
        /// </summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <returns>Distance between the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(float value1, float value2)
        {
            return Math.Abs(value1 - value2);
        }
        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position.</param>
        /// <param name="tangent1">Source tangent.</param>
        /// <param name="value2">Source position.</param>
        /// <param name="tangent2">Source tangent.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The result of the Hermite spline interpolation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
        {
            // All transformed to double not to lose precission
            // Otherwise, for high numbers of param:amount the result is NaN instead of Infinity
            double v1 = value1, v2 = value2, t1 = tangent1, t2 = tangent2, s = amount, result;
            double sCubed = s * s * s;
            double sSquared = s * s;

            if (amount == 0f)
                result = value1;
            else if (amount == 1f)
                result = value2;
            else
                result = (2 * v1 - 2 * v2 + t2 + t1) * sCubed +
                    (3 * v2 - 3 * v1 - 2 * t1 - t2) * sSquared +
                    t1 * s +
                    v1;
            return (float)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Lerp(int a, int b, float amount)
        {
            return (int)(a + (b - a) * amount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float amount)
        {
            return a + (b - a) * amount;
        }
        /// <summary>
        /// Reduces a given angle to a value between pi and -pi.
        /// </summary>
        /// <param name="angle">The angle to reduce, in radians.</param>
        /// <returns>The new angle, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float WrapAngle(float angle)
        {
            if ((angle > -MathF.PI) && (angle <= MathF.PI))
                return angle;
            angle %= MathF.PI * 2f;
            if (angle <= -MathF.PI)
                return angle + MathF.PI * 2f;
            if (angle > MathF.PI)
                return angle - MathF.PI * 2f;
            return angle;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpAngle(float current, float designated, float amt)
        {
            if (designated < current)
            {
                if (designated + (180 * DegreeRadian) < current)
                {
                    float distance = (2 * (float)Math.PI) - Math.Abs(designated - current);
                    return WrapAngle(current + (amt * distance));
                }
                else
                {
                    float distance = current - designated;
                    return WrapAngle(current - (distance * amt));
                }
            }
            else if (current < designated)
            {
                if (designated - (180 * DegreeRadian) > current)
                {
                    float distance = (2 * (float)Math.PI) - Math.Abs(designated - current);
                    return WrapAngle(current - (amt * distance));
                }
                else
                {
                    float distance = designated - current;
                    return WrapAngle(current + (distance * amt));
                }
            }
            else return current;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleDifference(float sourceAngle, float targetAngle)
        {
            return ModWrapped(targetAngle - sourceAngle + 180f * Mathf.DegreeRadian, 360f * Mathf.DegreeRadian) - 180f * Mathf.DegreeRadian;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Approach(float value, float toApproach, float amount)
        {
            float result = value;
            result += (toApproach - value) * amount;
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Approach(Vector2 value, Vector2 toApproach, float amount)
        {
            return new Vector2(Approach(value.X, toApproach.X, amount), Approach(value.Y, toApproach.Y, amount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ApproachAngle(this float curAngle, float targetAngle, float maxChange)
        {
            curAngle = WrapAngle(curAngle);
            targetAngle = WrapAngle(targetAngle);
            if (curAngle < targetAngle)
            {
                if (targetAngle - curAngle > (float)Math.PI)
                {
                    curAngle += MathF.PI * 2f;
                }
            }
            else if (curAngle - targetAngle > (float)Math.PI)
            {
                curAngle -= MathF.PI * 2f;
            }
            curAngle += Clamp(targetAngle - curAngle, -maxChange, maxChange);
            return WrapAngle(curAngle);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float transformX(float positionX, float positionY, Matrix4x4 matrix)
        {
            return (positionX * matrix.M11) + (positionY * matrix.M21) + matrix.M41;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float transformY(float positionX, float positionY, Matrix4x4 matrix)
        {
            return (positionX * matrix.M12) + (positionY * matrix.M22) + matrix.M42;
        }
    }
}