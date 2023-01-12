using Somnium.Framework.Vulkan;
using Somnium.Framework.Windowing;
using System.Diagnostics;
using System;

namespace Somnium.Framework
{
    public sealed class Application : IDisposable
    {
        public string AppName { get; private set; }

        public Stopwatch updateStopwatch;
        public Stopwatch drawStopwatch;

        public Window Window;

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

            app.Window = WindowGLFW.New(windowSize, title, preferredBackend);

            Graphics.application = app;

            return app;
        }
        public void Start()
        {
            VertexDeclaration.AddDefaultVertexDeclarations(runningBackend);
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.Initialize(Window, AppName);
                    break;
            }

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
                if (!Window.IsMinimized)
                {
                    if (runningBackend == Backends.Vulkan)
                    {
                        VkEngine.BeginDraw(Window);
                        VkEngine.EndDraw(Window);
                    }
                }
                Window.Update();
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
