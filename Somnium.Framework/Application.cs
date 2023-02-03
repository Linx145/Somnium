using Somnium.Framework.Vulkan;
using Somnium.Framework.GLFW;
using System.Diagnostics;
using System;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Somnium.Framework.Audio;

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
        /// <summary>
        /// Used to keep track of deltaTime for draw calls.
        /// </summary>
        public Stopwatch drawStopwatch;

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
            get => internalRenderPeriod <= double.Epsilon ? 0 : 1 / internalRenderPeriod;
            set => internalRenderPeriod = value <= double.Epsilon ? 0 : 1 / value;
        }

        public double UpdatesPerSecond
        {
            get => internalUpdatePeriod <= float.Epsilon ? 0 : 1f / internalUpdatePeriod;
            set => internalUpdatePeriod = value <= float.Epsilon ? 0 : 1f / value;
        }

        public bool loaded { get; private set; }
        private double internalUpdatePeriod;
        private double internalRenderPeriod;

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
        public static Application New(string AppName, Point windowSize, string title, Backends preferredBackend, uint maxSimultaneousFrames = 2)
        {
            if (maxSimultaneousFrames == 0 || maxSimultaneousFrames > 3)
            {
                throw new ArgumentOutOfRangeException("Max simultaneous frames must be between 1-3!");
            }
            Application app = new Application();
            app.AppName = AppName;
            Config.maxSimultaneousFrames = maxSimultaneousFrames;

            //TODO: check for preferredBackend compatibility
            app.runningBackend = preferredBackend;

            Input.instance = new InputStateGLFW();
            app.Window = WindowGLFW.New(app, windowSize, title, preferredBackend);

            app.Graphics = new Graphics(app);

            return app;
        }
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

            updateStopwatch = new Stopwatch();
            drawStopwatch = new Stopwatch();
            updateStopwatch.Start();
            drawStopwatch.Start();
            while (!Window.ShouldClose)
            {
                if (!loaded)
                {
                    OnLoad?.Invoke();
                    loaded = true;
                }
                double delta;

                delta = updateStopwatch.Elapsed.TotalSeconds;
                if (delta >= internalUpdatePeriod)
                {
                    updateStopwatch.Restart();
                    AudioEngine.Update();
                    Update?.Invoke((float)delta);
                }

                delta = drawStopwatch.Elapsed.TotalSeconds;
                if (delta >= internalRenderPeriod)
                {
                    drawStopwatch.Restart();
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

                    Draw?.Invoke((float)delta);

                    if (Graphics.currentRenderbuffer != null)
                    {
                        throw new ExecutionException("Renderbuffer target must be set to null(default) by the end of the draw loop!");
                    }
                    if (runningBackend == Backends.Vulkan)
                    {
                        VkEngine.EndDraw(this);
                    }
                    Window.frameNumber++;
                    if (Window.frameNumber >= Config.maxSimultaneousFrames)
                    {
                        Window.frameNumber = 0;
                    }
                }
                Window.Update();
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
            Unload?.Invoke();
            AudioEngine.Shutdown();
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.Shutdown();
                    break;
            }
        }
        public Action OnLoad;
        public Action<float> Update;
        public Action<float> Draw;
        public Action Unload;

        public void Dispose()
        {
            Window.Dispose();
        }
    }
}
