using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using System;
namespace Somnium.Framework.Windowing
{
    public abstract unsafe class Window : IDisposable
    {
        public abstract string Title { get; set; }
        public abstract Point Size { get; set; }
        public abstract Point Position { get; set; }
        public abstract bool ShouldClose { get; set; }

        public abstract void Dispose();
        public abstract void Update();

        public abstract bool VSync { get; set; }

        #region OpenGL
        public abstract IGLContext GetGLContext();

        #endregion

        #region Vulkan
        public abstract byte** GetRequiredExtensions(out uint Count);
        #endregion
    }
}
