using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;

namespace Somnium.Framework.Windowing
{
    public abstract unsafe class Window : IDisposable
    {
        public abstract string Title { get; set; }
        public abstract Point Size { get; set; }
        public abstract Point Position { get; set; }
        public abstract bool ShouldClose { get; set; }
        public abstract bool IsMinimized { get; set; }

        public abstract void Dispose();
        public abstract void Update();
        public abstract SurfaceKHR CreateWindowSurfaceVulkan();

        public abstract bool VSync { get; set; }

        #region OpenGL
        public abstract IGLContext GetGLContext();

        #endregion

        #region Vulkan
        public abstract byte** GetRequiredExtensions(out uint Count);
        public abstract Extent2D GetSwapChainExtents(in SurfaceCapabilitiesKHR capabilities);
        #endregion
    }
}
