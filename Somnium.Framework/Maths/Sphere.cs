using System;
using System.Collections.Generic;
using System.Numerics;

namespace Somnium.Framework
{
    public struct Sphere
    {
        public Vector3 Center;
        public float Radius;

        public Sphere(Vector3 center, float radius)
        {
            this.Center = center;
            this.Radius = radius;
        }
    }
}
