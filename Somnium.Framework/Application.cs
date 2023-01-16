using Somnium.Framework.Vulkan;
using Somnium.Framework.Windowing;
using System.Diagnostics;
using System;
using Silk.NET.Core;
using Silk.NET.Vulkan;

namespace Somnium.Framework
{
    public sealed class Application : IDisposable
    {
        public string AppName { get; private set; }

        public Graphics Graphics;

        public Stopwatch updateStopwatch;
        public Stopwatch drawStopwatch;

        public Window Window;

        /// <summary>
        /// The amount that should be allocated at the start for buffers(EG: Vertex buffers, index buffers, etc) in your app.
        /// <br>Done for optimisation reasons. Is not a hard limit to how much memory you can use. Not applicable in previous generation APIs like OpenGL</br>
        /// </summary>
        public static double memoryForBuffersInMiB = 2;
        /// <summary>
        /// The amount that should be allocated at the start for images in your app.
        /// <br>Done for optimisation reasons. Is not a hard limit to how much memory you can use. Not applicable in previous generation APIs like OpenGL</br>
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

        private bool loaded;
        private double internalUpdatePeriod;
        private double internalRenderPeriod;

        public Backends runningBackend { get; private set; }

        public static Application New(string AppName, Point windowSize, string title, Backends preferredBackend)
        {
            Application app = new Application();
            app.AppName = AppName;

            //check for preferredBackend compatibility
            app.runningBackend = preferredBackend;

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
                    Update?.Invoke((float)delta);
                }

                if (delta >= internalRenderPeriod)
                {
                    delta = drawStopwatch.Elapsed.TotalSeconds;
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
                        VkEngine.BeginDraw(Window);
                    }

                    Draw?.Invoke((float)delta);

                    if (runningBackend == Backends.Vulkan)
                    {
                        VkEngine.EndDraw(Window);
                    }
                }
                Window.Update();
            }
            //where applicable, we need to wait for the graphics API to finish rendering before shutting down
            //to avoid disposing things that are being used
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.vk.WaitForFences(VkEngine.vkDevice, 1, in VkEngine.fence, new Bool32(true), uint.MaxValue);
                    break;
            }
            Unload?.Invoke();
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
