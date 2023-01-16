using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;
using System;

namespace Somnium.Framework.Windowing
{
    public abstract unsafe class Window : IDisposable
    {
        public Application application;
        public Color clearColor = Color.CornflowerBlue;//new Color(50, 50, 50);
        public abstract string Title { get; set; }
        public abstract Point Size { get; set; }
        public abstract Point Position { get; set; }
        public abstract bool ShouldClose { get; set; }
        public abstract bool IsMinimized { get; set; }

        public abstract void Dispose();
        public abstract void Update();
        /// <summary>
        /// Called when the window is resized, with arguments being 1)the resized window, 2)the new window width, 3)the new window height
        /// </summary>
        public event Action<Window, int, int> OnResized;
        public unsafe void OnResizedCallback(WindowHandle* handle, int width, int height)
        {
            OnResized?.Invoke(this, width, height);
        }

        public abstract bool VSync { get; set; }

        #region OpenGL
        public abstract IGLContext GetGLContext();

        #endregion

        #region Vulkan
        public abstract byte** GetRequiredExtensions(out uint Count);
        public abstract Extent2D GetSwapChainExtents(in SurfaceCapabilitiesKHR capabilities);
        public abstract Point GetFramebufferExtents();
        public abstract SurfaceKHR CreateWindowSurfaceVulkan();
        #endregion
    }
}
