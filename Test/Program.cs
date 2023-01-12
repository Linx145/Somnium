using Somnium.Framework;
using System;
using System.Numerics;

namespace Test
{
    public static class Program
    {
        private static Application application;
        private static float recordTime = 0f;

        private static VertexBuffer vb;

        [STAThread]
        public static void Main(string[] args)
        {
            using (application = Application.New("Test", new Point(1920, 1080), "Window", Backends.Vulkan))
            {
                application.OnLoad = OnLoad;
                application.Update = Update;
                application.Draw = Draw;
                application.Unload = Unload;
                application.Start();
            }
        }
        static VertexPositionColor[] vertices;
        private static void OnLoad()
        {
            vertices = new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(0f, -0.5f, 0f), Color.Red),
                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0f), Color.Green),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0f), Color.Blue)
            };
            vb = new VertexBuffer(application, VertexPositionColor.VertexDeclaration, 3, false);
            vb.SetData(vertices, 0, 3);
        }

        private static void Draw(float deltaTime)
        {
            Graphics.SetVertexBuffer(vb);

            Graphics.DrawPrimitives(3, 1);
        }

        private static void Unload()
        {
            vb.Dispose();
        }

        private static void Update(float deltaTime)
        {
            recordTime += deltaTime;
            if (recordTime >= 0.2f)
            {
                application.Window.Title = string.Concat("Test ", MathF.Round(1f / deltaTime).ToString());
                recordTime -= 0.2f;
            }
            //Here all updates to the program should be done.
        }
    }
}