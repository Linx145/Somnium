using System;
using System.Collections.Generic;

namespace Somnium.Framework
{
    public struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;

        public BoundingBox(Vector3 min, Vector3 max)
        {
            this.Min = min;
            this.Max = max;
        }

        public bool Contains(BoundingBox other)
        {
            if (other.Max.X < Min.X
                || other.Min.X > Max.X
                || other.Max.Y < Min.Y
                || other.Min.Y > Max.Y
                || other.Max.Z < Min.Z
                || other.Min.Z > Max.Z)
                return false;

            return true;
        }
    }
}
