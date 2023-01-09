using Somnium.Framework.Vulkan;
using Somnium.Framework.Windowing;
using System.Diagnostics;
using System.Reflection;

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

            return app;
        }
        public void Start()
        {
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VulkanEngine.Initialize(Window, AppName);
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
                    {
                        var glContext = Window.GetGLContext();
                        if (glContext != null)
                        {
                            glContext.MakeCurrent();
                        }
                    }
                    delta = drawStopwatch.Elapsed.TotalSeconds;
                    drawStopwatch.Restart();
                    Draw?.Invoke((float)delta);
                }

                Window.Update();
            }

            switch (runningBackend)
            {
                case Backends.Vulkan:
                    VulkanEngine.Shutdown();
                    break;
            }
        }
        public Action OnLoad;
        public Action<float> Update;
        public Action<float> Draw;

        public void Dispose()
        {
            Window.Dispose();
        }
    }
}
