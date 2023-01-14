using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework
{
    public struct Viewport
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float minDepth;
        public float maxDepth;

        public Viewport(float X, float Y, float Width, float Height, float minDepth, float maxDepth)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
        }

        public Silk.NET.Vulkan.Viewport ToVulkanViewport()
        {
            return new Silk.NET.Vulkan.Viewport(X, Y, Width, Height, minDepth, maxDepth);
        }
    }
}
