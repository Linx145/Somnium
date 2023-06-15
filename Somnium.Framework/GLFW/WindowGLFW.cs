using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;
#if VULKAN
using Somnium.Framework.Vulkan;
#endif
using System;

namespace Somnium.Framework.GLFW
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

        public WindowGLFW(Application application)
        {
            this.application = application;
        }

        #region properties
        public override INativeWindow Native => new GlfwNativeWindow(SomniumGLFW.API, handle);
        public override Point Size
        {
            get
            {
                return internalSize;
            }
            set
            {
                if (handle != null) Glfw.SetWindowSize(handle, value.X, value.Y);
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
        public override void Maximize()
        {
            Glfw.MaximizeWindow(handle);
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
        public override bool UserCanResize
        {
            get
            {
                return Glfw.GetWindowAttrib(handle, WindowAttributeGetter.Resizable);
            }
            set
            {
                Glfw.SetWindowAttrib(handle, WindowAttributeSetter.Resizable, value);
            }
        }
        /*public override bool Fullscreen
        {
            set
            {
                unsafe
                {
                    Monitor* monitor = Glfw.GetPrimaryMonitor();
                    if (value)
                    {
                        Glfw.GetMonitorWorkarea(monitor, out int x, out int y, out int w, out int h);
                        Glfw.SetWindowMonitor(handle, monitor, x, y, w, h, Glfw.DontCare);
                    }
                    else
                    {
                        Glfw.SetWindowMonitor(handle, null, 0, 0, );
                    }
                }
            }
        }*/
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
#if VULKAN
        public override byte** GetRequiredExtensions(out uint count)
        {
            return Glfw.GetRequiredInstanceExtensions(out count);
        }
#endif
#endregion

        private Point internalSize;
        private Point internalPosition;
        private string internalTitle;
        private bool internalVSync = true;

        private bool VSyncChanged = false;

        public GlfwContext GLContext { get; private set; }

        public static WindowGLFW New(Application application, Point windowSize, string title, Backends backend)
        {
            if (!SomniumGLFW.initialized)
            {
                bool result = SomniumGLFW.Initialize();
                if (!result)
                {
                    throw new InitializationException("GLFW failed to initialize!");
                }
            }
            WindowGLFW window = new WindowGLFW(application);
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
                default:
                    Glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
                    break;
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

            Glfw.SetFramebufferSizeCallback(window.handle, new GlfwCallbacks.FramebufferSizeCallback(window.OnResizedGLFW));
            Glfw.SetWindowPosCallback(window.handle, new GlfwCallbacks.WindowPosCallback(window.OnMovedGLFW));
            Glfw.SetWindowIconifyCallback(window.handle, new GlfwCallbacks.WindowIconifyCallback(window.OnMinimizationChangedGLFW));
            Glfw.SetWindowMaximizeCallback(window.handle, new GlfwCallbacks.WindowMaximizeCallback(window.OnMaximizeGLFW));

            activeWindows++;
            //set the key press event for this window
            Glfw.SetCharCallback(window.handle, new GlfwCallbacks.CharCallback(window.OnTextInput));
            Glfw.SetKeyCallback(window.handle, new GlfwCallbacks.KeyCallback(window.OnKeyPressed));
            Glfw.SetMouseButtonCallback(window.handle, new GlfwCallbacks.MouseButtonCallback(window.OnMousePressed));
            Glfw.SetCursorPosCallback(window.handle, new GlfwCallbacks.CursorPosCallback(window.MousePositionCallback));
            Glfw.SetScrollCallback(window.handle, new GlfwCallbacks.ScrollCallback(window.MouseScrollCallback));

            window.initialized = true;
            return window;
        }
        #region window control callbacks
        public unsafe void OnMaximizeGLFW(WindowHandle* handle, bool maximized)
        {
            if (maximized)
            {
                Glfw.GetWindowSize(handle, out int width, out int height);
                //OnResized(this, width, height);
                internalSize.X = width;
                internalSize.Y = height;

                OnMaximize(this, width, height);
            }
        }
        public unsafe void OnMinimizationChangedGLFW(WindowHandle* handle, bool status)
        {
            if (status) //if is minimized, wipe the input
            {
                InputStateGLFW.ClearAllInputStates();
            }
            base.OnMinimizationChanged(this, status);
        }
        public unsafe void OnResizedGLFW(WindowHandle* handle, int width, int height)
        {
            internalSize.X = width;
            internalSize.Y = height;

            Debugger.Log("Window resized. New size: " + internalSize);

            base.OnResized(this, width, height);
        }
        public unsafe void OnMovedGLFW(WindowHandle* handle, int X, int Y)
        {
            internalPosition.X = X;
            internalPosition.Y = Y;

            base.OnMoved(this, X, Y);
        }
        public void Close()
        {
            Glfw.SetWindowShouldClose(handle, true);
        }
        #endregion

        #region input callbacks
        public unsafe void OnKeyPressed(WindowHandle* handle, Silk.NET.GLFW.Keys key, int scanCode, InputAction inputAction, KeyModifiers modifiers)
        {
            if (key == Silk.NET.GLFW.Keys.Unknown)
            {
                return;
            }
            if (inputAction == InputAction.Press)
            {
                InputStateGLFW.keysDown.Insert((uint)key, true);
                InputStateGLFW.perFrameKeyStates.Insert((uint)key, KeyState.Pressed);
            }
            else if (inputAction == InputAction.Release)
            {
                InputStateGLFW.keysDown.Insert((uint)key, false);
                InputStateGLFW.perFrameKeyStates.Insert((uint)key, KeyState.Released);
            }

            onKeyPressed?.Invoke((Keys)(int)key, scanCode, (KeyState)(int)inputAction);
        }
        public unsafe void OnTextInput(WindowHandle* handle, uint codePoint)
        {
            onTextInput?.Invoke((char)codePoint);
        }
        public unsafe void OnMousePressed(WindowHandle* handle, MouseButton button, InputAction inputAction, KeyModifiers keyModifiers)
        {
            if (inputAction == InputAction.Press)
            {
                InputStateGLFW.mouseButtonsDown.Insert((uint)button, true);
                InputStateGLFW.perFrameMouseStates.Insert((uint)button, KeyState.Pressed);
            }
            else if (inputAction == InputAction.Release)
            {
                InputStateGLFW.mouseButtonsDown.Insert((uint)button, false);
                InputStateGLFW.perFrameMouseStates.Insert((uint)button, KeyState.Released);
            }
        }
        public unsafe void MousePositionCallback(WindowHandle* handle, double x, double y)
        {
            InputStateGLFW.internalMousePosition.X = (float)x;
            InputStateGLFW.internalMousePosition.Y = (float)y;
        }
        public unsafe void MouseScrollCallback(WindowHandle* handle, double offsetX, double offsetY)
        {
            InputStateGLFW.scroll.X = (float)offsetX;
            InputStateGLFW.scroll.Y = (float)offsetY;
        }
        #endregion
        public override void UpdateInput()
        {
            InputStateGLFW.ResetPerFrameInputStates();
        }
        public override void UpdateWindowControls()
        {
            //finally, reset per state key frames
            //and poll events such as clicking window close/minimize buttons, etc
            Glfw.PollEvents();
        }
        public override unsafe void SetIcon(Texture2D texture)
        {
            Silk.NET.GLFW.Image image = new Silk.NET.GLFW.Image();
            image.Width = (int)texture.Width;
            image.Height = (int)texture.Height;
            Span<byte> bytes = texture.GetData<byte>();
            fixed (byte* ptr = &bytes[0])
            {
                image.Pixels = ptr;
            }
            Glfw.SetWindowIcon(handle, 1, &image);
        }
        public override IGLContext GetGLContext() => GLContext;
        public override CommandCollection GetDefaultCommandCollection()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    return VkEngine.commandBuffer;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
#if VULKAN
        public override bool CreateWindowSurfaceVulkan(out SurfaceKHR surface)
        {
            VkNonDispatchableHandle surfaceHandle;
            Result result = (Result)Glfw.CreateWindowSurface(VkEngine.vkInstance.ToHandle(), handle, null, &surfaceHandle);
            if (result != Result.Success)
            {
                surface = default;
                return false;
                //throw new InitializationException("Failed to create Vulkan surface for window!");
            }
            surface = surfaceHandle.ToSurface();
            return true;
        }
#endif
        public override Point GetFramebufferExtents()
        {
            int width;
            int height;
            Glfw.GetFramebufferSize(handle, out  width, out height);
            Glfw.WaitEvents();

            return new Point(width, height);
        }
#if VULKAN
        public override Extent2D GetSwapChainExtents(in SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != int.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                //SomniumGLFW.API.GetFramebufferSize(handle, out int width, out int height);
                int width = Size.X;
                int height = Size.Y;
                Extent2D extents = new Extent2D((uint)width, (uint)height);

                extents.Width = Math.Clamp(extents.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
                extents.Height = Math.Clamp(extents.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

                return extents;
            }
        }
#endif
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
