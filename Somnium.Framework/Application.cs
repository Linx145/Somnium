using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;
using Somnium.Framework.Audio;
using Somnium.Framework.GLFW;
using Somnium.Framework.Vulkan;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;

namespace Somnium.Framework
{
    /// <summary>
    /// The entry point for your app. Every app should only have one Application instance, but (in the future) can have multiple windows.
    /// </summary>
    public class Application : IDisposable
    {
        public static class Config
        {
            /// <summary>
            /// The max number of frames that can be processed at once. Must be 1-3 inclusive
            /// </summary>
            public static uint maxSimultaneousFrames = 2;
            /// <summary>
            /// Whether to throw an exception everytime the backend graphics API encounters an error
            /// </summary>
            public static bool throwValidationExceptions = true;
            /// <summary>
            /// Whether to send the debug information via Console.Writeline, System.Diagnostic.Debug.Writeline or not send anything at all.
            /// </summary>
            public static LoggingMode loggingMode = LoggingMode.Console;
            /// <summary>
            /// Whether to log manual memory allocations present in low-level APIs such as Vulkan
            /// </summary>
            public static bool logMemoryAllocations = false;

            public static bool logUniformBufferAllocations = false;
        }

        /// <summary>
        /// Whether rendering + The Draw Callback should be done on a separate thread
        /// </summary>
        public bool runRenderOnSeparateThread { get; private set; }
        private Thread renderThread;
        /// <summary>
        /// The name of your app.
        /// </summary>
        public string AppName { get; private set; }
        /// <summary>
        /// The Graphics context.
        /// </summary>
        public Graphics Graphics;
        /// <summary>
        /// Used to keep track of deltaTime for update calls.
        /// </summary>
        public Stopwatch updateStopwatch;

        //TODO: Allow applications to have multiple windows
        public Window Window;

        /// <summary>
        /// The amount that should be allocated at the start for buffers(EG: Vertex buffers, index buffers, etc) in your app.
        /// <br>Done for optimisation reasons. Is not a hard limit to how much memory you can use as memory reserved will dynamically grow. Not applicable in previous generation APIs like OpenGL</br>
        /// </summary>
        public static double memoryForBuffersInMiB = 2;
        /// <summary>
        /// The amount that should be allocated at the start for images in your app.
        /// <br>Done for optimisation reasons. Is not a hard limit to how much memory you can use as memory reserved will dynamically grow. Not applicable in previous generation APIs like OpenGL</br>
        /// </summary>
        public static double memoryForImagesInMiB = 64;

        public double FramesPerSecond
        {
            get => internalUpdatePeriod <= float.Epsilon ? 0 : 1f / internalUpdatePeriod;
            set => internalUpdatePeriod = value <= float.Epsilon ? 0 : 1f / value;
        }

        public bool loaded { get; private set; }
        private double internalUpdatePeriod;

        public Backends runningBackend { get; private set; }

        /// <summary>
        /// Creates the program's Application instance.
        /// </summary>
        /// <param name="AppName">The internal name of the app. Used as an identifier by external programs/drivers</param>
        /// <param name="windowSize">The initial size of the window to be created</param>
        /// <param name="title">The window title</param>
        /// <param name="preferredBackend">The preferred backend. (In the future) should the backend not be available, Somnium will automatically choose the next best backend for the app.</param>
        /// <param name="maxSimultaneousFrames">The maximum rendering frames to be processed at a time, for use in the Double Buffering technique to boost FPS in Vulkan, DX12 and Metal</param>
        /// <returns></returns>
        public static Application New(Application instance, string AppName, Point windowSize, string title, Backends preferredBackend, uint maxSimultaneousFrames = 2, bool runRenderOnSeparateThread = true)
        {
            if (maxSimultaneousFrames == 0 || maxSimultaneousFrames > 3)
            {
                throw new ArgumentOutOfRangeException("Max simultaneous frames must be between 1-3!");
            }
            Application app = instance ?? new Application();
            app.AppName = AppName;
            app.runRenderOnSeparateThread = runRenderOnSeparateThread;
            Config.maxSimultaneousFrames = maxSimultaneousFrames;

            //TODO: check for preferredBackend compatibility
            app.runningBackend = preferredBackend;

            Input.instance = new InputStateGLFW();
            app.Window = WindowGLFW.New(app, windowSize, title, preferredBackend);

            app.Graphics = new Graphics(app);

            return app;
        }
        public Action InitializeCallback;
        public Action OnLoadCallback;
        public Action<float> UpdateCallback;
        public Action<float> DrawCallback;
        public Action<float> PostEndDrawCallback;
        public Action UnloadCallback;
        public Action SubmitRenderItemsCallback;

        double delta;

        public void Start()
        {
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.Initialize(Window, AppName);
                    break;
            }
            AudioEngine.Initialize();
            SamplerState.AddDefaultSamplerStates(this);
            VertexDeclaration.AddDefaultVertexDeclarations(runningBackend);
            InitializeCallback?.Invoke();

            updateStopwatch = new Stopwatch();
            updateStopwatch.Start();

            while (!Window.ShouldClose)
            {
                if (!loaded)
                {
                    OnLoadCallback?.Invoke();
                    loaded = true;
                }
                delta = updateStopwatch.Elapsed.TotalSeconds;
                if (delta >= internalUpdatePeriod)
                {
                    Window.UpdateInput();
                    Window.UpdateWindowControls();
                    while (Window.Size.X == 0 || Window.Size.Y == 0)
                    {
                        Thread.Sleep(0);
                        Window.UpdateWindowControls();
                    }

                    updateStopwatch.Restart();
                    UpdateCallback?.Invoke((float)delta);

                    if (!runRenderOnSeparateThread)
                    {
                        SubmitRenderItemsCallback?.Invoke();

                        DoRender();
                    }
                    else
                    {
                        if (allowSubmitRenderItems == null)
                        {
                            allowSubmitRenderItems = new ManualResetEventSlim(true);
                        }
                        //pause the main thread until the (previous frame's) render has been completed
                        allowSubmitRenderItems.Wait();
                        //only then can we update the renderitem list via calling SubmitRenderItemsCallback
                        SubmitRenderItemsCallback?.Invoke();

                        if (renderThread == null)
                        {
                            allowDraw = new ManualResetEventSlim();
                            renderThread = new Thread(new ThreadStart(RenderThreadLoop));
                            renderThread.IsBackground = true;
                            renderThread.Start();
                        }
                        else allowDraw.Set();
                    }

                    /*if (VSyncChanged)
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
                    }*/
                }
                AudioEngine.Update();
            }
            if (runRenderOnSeparateThread)
            {
                //allow it to die
                allowDraw.Set();
            }
            //where applicable, we need to wait for the graphics API to finish rendering before shutting down
            //to avoid disposing things that are being used
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        Fence* fences = stackalloc Fence[(int)Config.maxSimultaneousFrames];
                        for (int i = 0; i < Config.maxSimultaneousFrames; i++)
                        {
                            fences[i] = VkEngine.frames[i].fence;
                        }
                        VkEngine.vk.WaitForFences(VkEngine.vkDevice, (uint)Config.maxSimultaneousFrames, fences, new Bool32(true), uint.MaxValue);
                    }
                    break;
            }
            UnloadCallback?.Invoke();
            AudioEngine.Shutdown();
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.Shutdown();
                    break;
            }
        }

        public void Dispose()
        {
            Window.Dispose();
        }

        private static ManualResetEventSlim allowDraw;
        private static ManualResetEventSlim allowSubmitRenderItems;

        public void DoRender()
        {
            if (runningBackend == Backends.OpenGL)
            {
                var glContext = Window.GetGLContext();
                if (glContext != null)
                {
                    glContext.MakeCurrent();
                }
            }
            else if (runningBackend == Backends.Vulkan)
            {
                VkEngine.BeginDraw();
            }

            DrawCallback?.Invoke((float)delta);

            if (Graphics.currentRenderbuffer != null)
            {
                throw new ExecutionException("Renderbuffer target must be set to null(default) by the end of the draw loop!");
            }
            if (runningBackend == Backends.Vulkan)
            {
                VkEngine.EndDraw(this);
            }
            PostEndDrawCallback?.Invoke((float)delta);
            Window.frameNumber++;
            if (Window.frameNumber >= Config.maxSimultaneousFrames)
            {
                Window.frameNumber = 0;
            }
        }
        public void RenderThreadLoop()
        {
            while (!Window.ShouldClose)
            {
                //prevent the render items from being modified
                allowSubmitRenderItems.Reset();
                DoRender();
                //when we are finished rendering, allow items to be modified again
                allowSubmitRenderItems.Set();

                allowDraw.Wait();
                allowDraw.Reset();
            }
        }
    }
}
