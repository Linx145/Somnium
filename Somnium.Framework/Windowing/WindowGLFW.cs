using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System.Diagnostics;

namespace Somnium.Framework.Windowing
{
    public unsafe class WindowGLFW : Window
    {

        public static int activeWindows { get; private set; }
        public static Glfw Glfw
        {
            get
            {
                return SomniumGLFW.API;
            }
        }

        public WindowHandle* handle;

        #region properties
        public override Point Size
        {
            get
            {
                return internalSize;
            }
            set
            {
                if (handle != null) Glfw.SetWindowSize(handle, Size.X, Size.Y);
                internalSize = value;
            }
        }
        public override Point Position
        {
            get
            {
                return internalPosition;
            }
            set
            {
                if (handle != null) Glfw.SetWindowPos(handle, value.X, value.Y);
                internalPosition = value;
            }
        }
        public override string Title
        {
            get
            {
                return internalTitle;
            }
            set
            {
                if (handle != null) Glfw.SetWindowTitle(handle, value);
                internalTitle = value;
            }
        }
        public override bool ShouldClose
        {
            get
            {
                return Glfw.WindowShouldClose(handle);
            }
            set
            {
                Glfw.SetWindowShouldClose(handle, value);
            }
        }
        public override bool IsMinimized
        {
            get
            {
                return internalIsMinimized;// Glfw.GetWindowAttrib(handle, WindowAttributeGetter.Iconified);
            }
            set
            {
                if (internalIsMinimized != value)
                {
                    if (value)
                    {
                        Glfw.IconifyWindow(handle);
                    }
                    else
                    {
                        Glfw.MaximizeWindow(handle);
                    }
                    internalIsMinimized = value;
                }
            }
        }
        private bool internalIsMinimized = false;
        public override bool VSync
        {
            get
            {
                return internalVSync;
            }
            set
            {
                VSyncChanged = true;
                internalVSync = value;
            }
        }
        public override byte** GetRequiredExtensions(out uint count)
        {
            return Glfw.GetRequiredInstanceExtensions(out count);
        }
        #endregion

        private Point internalSize;
        private Point internalPosition;
        private string internalTitle;
        private bool internalVSync = true;

        private bool VSyncChanged = false;

        public GlfwContext GLContext { get; private set; }

        public static WindowGLFW New(Point windowSize, string title, Backends backend)
        {
            if (!SomniumGLFW.initialized)
            {
                bool result = SomniumGLFW.Initialize();
                if (!result)
                {
                    throw new InitializationException("GLFW failed to initialize!");
                }
            }
            WindowGLFW window = new WindowGLFW();
            window.internalSize = windowSize;
            window.internalTitle = title;

            switch (backend)
            {
                case Backends.OpenGL:
                    Glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3); // Targeted major version
                    Glfw.WindowHint(WindowHintInt.ContextVersionMinor, 2); // Targeted minor version
                    Glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
                    Glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
                    break;
                case Backends.Vulkan:
                    Glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
                    break;
                default:
                    throw new NotImplementedException();
            }

            window.handle = Glfw.CreateWindow(windowSize.X, windowSize.Y, title, null, null);
            if (window.handle == null)
            {
                throw new InitializationException("Unable to create window!");
            }

            if (backend == Backends.OpenGL)
            {
                window.GLContext = new GlfwContext(Glfw, window.handle);
            }

            activeWindows++;
            return window;
        }

        public void Close()
        {
            Glfw.SetWindowShouldClose(handle, true);
        }
        public override void Update()
        {
            if (!Glfw.GetWindowAttrib(handle, WindowAttributeGetter.Iconified))
            {
                // Window
                Glfw.GetWindowSize(handle, out internalSize.X, out internalSize.Y);
                Glfw.GetWindowPos(handle, out internalPosition.X, out internalPosition.Y);
            }

            if (GLContext != null)
            {
                if (VSyncChanged)
                {
                    if (handle != null)
                    {
                        Glfw.SwapInterval(0);
                    }
                    VSyncChanged = false;
                }

                if (VSync)
                {
                    Glfw.SwapBuffers(handle);
                }
            }
            Glfw.PollEvents();
        }
        public override IGLContext GetGLContext() => GLContext;
        public override SurfaceKHR CreateWindowSurfaceVulkan()
        {
            VkNonDispatchableHandle surfaceHandle;
            Result result = (Result)Glfw.CreateWindowSurface(VkEngine.vkInstance.ToHandle(), handle, null, &surfaceHandle);
            if (result != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan surface for window!");
            }
            return surfaceHandle.ToSurface();
        }
        public override Extent2D GetSwapChainExtents(in SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != int.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                SomniumGLFW.API.GetFramebufferSize(handle, out int width, out int height);
                Extent2D extents = new Extent2D((uint)width, (uint)height);

                extents.Width = Math.Clamp(extents.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
                extents.Height = Math.Clamp(extents.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

                return extents;
            }
        }
        public override void Dispose()
        {
            if (handle != null)
            {
                Glfw.DestroyWindow(handle);
                activeWindows--;
                if (activeWindows == 0)
                {
                    SomniumGLFW.Shutdown();
                }
            }
        }
    }
}
