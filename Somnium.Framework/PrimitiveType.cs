using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework
{
    /// <summary>
    /// Defines how vertex data is ordered.
    /// </summary>
    public enum PrimitiveType
    {
        /// <summary>
        /// Renders the specified vertices as a sequence of isolated triangles. Each group of three vertices defines a separate triangle.
        /// </summary>
        TriangleList,

        /// <summary>
        /// Renders the vertices as a triangle strip.
        /// </summary>
        TriangleStrip,

        /// <summary>
        /// Renders the vertices as a list of isolated straight line segments; the count may be any positive integer.
        /// </summary>
        LineList,

        /// <summary>
        /// Renders the vertices as a single polyline; the count may be any positive integer.
        /// </summary>
        LineStrip,

        /// <summary>
        /// Renders the vertices as individual points; the count may be any positive integer.
        /// </summary>
        PointList,

        /// <summary>
        /// Renders the vertices as a series of triangle primitives connected to a central origin vertex
        /// </summary>
        TriangleFan,

        TriangleListWithAdjacency,

        LineStripWithAdjacency
    }
}
