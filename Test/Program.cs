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
        private static IndexBuffer indexBuffer;

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
        static ushort[] indices;
        private static void OnLoad()
        {
            vertices = new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0f), Color.Red),
                new VertexPositionColor(new Vector3(0.5f, -0.5f, 0f), Color.Green),
                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0f), Color.Blue),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0f), Color.Blue)
            };
            vb = new VertexBuffer(application, VertexPositionColor.VertexDeclaration, 4, false);
            vb.SetData(vertices, 0, 4);

            indices = new ushort[]
            {
                0, 1, 2, 2, 3, 0
            };
            indexBuffer = new IndexBuffer(application, IndexSize.Uint16, 6, false);
            indexBuffer.SetData(indices, 0, 6);
        }

        private static void Draw(float deltaTime)
        {
            Graphics.SetVertexBuffer(vb);
            Graphics.SetIndexBuffer(indexBuffer);

            Graphics.DrawIndexedPrimitives(6, 2);
        }

        private static void Unload()
        {
            vb.Dispose();
            indexBuffer.Dispose();
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