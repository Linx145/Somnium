using Silk.NET.Core.Contexts;
#if VULKAN
using Silk.NET.Vulkan;
#endif
using System;

namespace Somnium.Framework
{
    public abstract unsafe class Window : IDisposable
    {
        /// <summary>
        /// Whether the window has been initialized.
        /// </summary>
        public bool initialized { get; internal set; } = false;
        /// <summary>
        /// If you are using low-level Graphics API such as Vulkan, Metal or DX12, refers to the index of the current frame within the array as specified by Application.Config.maxSimultaneousFrames
        /// </summary>
        public int frameNumber { get; internal set; }

        public Application application;
        /// <summary>
        /// The window title
        /// </summary>
        public abstract string Title { get; set; }
        /// <summary>
        /// The window size
        /// </summary>
        public abstract Point Size { get; set; }
        /// <summary>
        /// The window position on screen
        /// </summary>
        public abstract Point Position { get; set; }
        /// <summary>
        /// Set to true to close the window when it is safe.
        /// </summary>
        public abstract bool ShouldClose { get; set; }
        /// <summary>
        /// Whether the window is minimized
        /// </summary>
        public abstract bool IsMinimized { get; set; }
        /// <summary>
        /// Gets the default collection to record commands to. Applicable for low-level Graphics API, usually corresponds to the command collection of the current frame as per frameNumber
        /// </summary>
        /// <returns></returns>
        public abstract CommandCollection GetDefaultCommandCollection();
        public abstract void Dispose();
        public abstract void UpdateInput();
        public abstract void UpdateWindowControls();
        /// <summary>
        /// Called when the window is resized, with arguments being 1)the resized window, 2)the new window width, 3)the new window height
        /// </summary>
        public event Action<Window, int, int> onResized;
        /// <summary>
        /// Called when the window is moved, with arguments being 1)the moved window, 2)the new X position, 3)the new Y position
        /// </summary>
        public event Action<Window, int, int> onMoved;
        /// <summary>
        /// Called when the window is minimized/restored
        /// </summary>
        public event Action<Window, bool> onMinimizationChanged;
        /// <summary>
        /// Called when a key is pressed.
        /// </summary>
        public Action<Keys, int, KeyState> onKeyPressed;
        /// <summary>
        /// Called when a character is inputted, distinct from onKeyPressed in it's recording function
        /// </summary>
        public Action<char> onTextInput;
        /// <summary>
        /// Whether VSync should be turned on, thus limiting max FPS to your monitor's refresh rate but preventing screen tearing. Only applicable in high-level Graphics API such as OpenGL as DX11
        /// </summary>
        public abstract bool VSync { get; set; }

        public abstract bool UserCanResize { get; set; }
        //public abstract bool Fullscreen { get; set; }

        protected void OnMaximize(Window window, int width, int height)
        {
            onResized?.Invoke(window, width, height);
        }
        protected void OnResized(Window window, int width, int height)
        {
            onResized?.Invoke(window, width, height);
        }
        protected void OnMoved(Window window, int X, int Y)
        {
            onMoved?.Invoke(window, X, Y);
        }
        protected void OnMinimizationChanged(Window window, bool isMinimized)
        {
            onMinimizationChanged?.Invoke(window, isMinimized);
        }

        #region OpenGL
        public abstract IGLContext GetGLContext();
        #endregion

        #region Vulkan
#if VULKAN
        public abstract byte** GetRequiredExtensions(out uint Count);
        public abstract Extent2D GetSwapChainExtents(in SurfaceCapabilitiesKHR capabilities);
        public abstract Point GetFramebufferExtents();
        public abstract SurfaceKHR CreateWindowSurfaceVulkan();
#endif
#endregion
    }
}
