using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;
using System;

namespace Somnium.Framework.Windowing
{
    public abstract unsafe class Window : IDisposable
    {
        /// <summary>
        /// Whether the window has been initialized.
        /// </summary>
        public bool initialized { get; internal set; } = false;
        /// <summary>
        /// If you are using low-level Graphics API such as Vulkan, Metal or DX12, refers to the index of the current frame within the array as specified by maxSimultaneousFrames
        /// </summary>
        public int frameNumber { get; internal set; }
        /// <summary>
        /// If you are using low-level Graphics API such as Vulkan, Metal or DX12, specifies the amount of frames that can be drawn at the same time. Defaults to 2
        /// </summary>
        public int maxSimultaneousFrames
        {
            get
            {
                return internalMaxSimultaneousFrames;
            }
            set
            {
                if (initialized)
                {
                    throw new InvalidOperationException("Cannot change the max simultaneous frames of a window during runtime!");
                }
                internalMaxSimultaneousFrames = value;
            }
        }
        private int internalMaxSimultaneousFrames;
        public readonly Application application;
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
        /// <summary>
        /// Called every tick
        /// </summary>
        public abstract void Update();
        /// <summary>
        /// Called when the window is resized, with arguments being 1)the resized window, 2)the new window width, 3)the new window height
        /// </summary>
        public event Action<Window, int, int> OnResized;
        public unsafe void OnResizedCallback(WindowHandle* handle, int width, int height)
        {
            OnResized?.Invoke(this, width, height);
        }
        /// <summary>
        /// Whether VSync should be turned on, thus limiting max FPS to your monitor's refresh rate but preventing screen tearing. Only applicable in high-level Graphics API such as OpenGL as DX11
        /// </summary>
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
