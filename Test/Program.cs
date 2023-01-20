using Somnium.Framework;
using Somnium.Framework.Windowing;
using System;
using System.Numerics;

namespace Test
{
    public static class Program
    {
        private static Application application;
        private static float recordTime = 0f;

        private static Graphics Graphics;

        private static Shader shader;
        private static PipelineState state;
        private static Texture2D texture;
        private static RenderTarget2D renderTarget;

        [STAThread]
        public static void Main(string[] args)
        {
            using (application = Application.New("Test", new Point(1920, 1080), "Window", Backends.Vulkan, 2))
            {
                application.OnLoad = OnLoad;
                application.Update = Update;
                application.Draw = Draw;
                application.Unload = Unload;
                Graphics = application.Graphics;
                application.Start();
            }
        }
        private static VertexPositionColorTexture[] vertices;
        private static ushort[] indices;
        private static VertexBuffer vb;
        private static IndexBuffer indexBuffer;

        private static void OnLoad()
        {
            vertices = new VertexPositionColorTexture[]
            {
                new VertexPositionColorTexture(new Vector3(-0.5f, -0.5f, 0f), Color.White, new Vector2(0, 0)),
                new VertexPositionColorTexture(new Vector3(0.5f, -0.5f, 0f), Color.White, new Vector2(1, 0)),
                new VertexPositionColorTexture(new Vector3(0.5f, 0.5f, 0f), Color.White, new Vector2(1, 1)),
                new VertexPositionColorTexture(new Vector3(-0.5f, 0.5f, 0f), Color.White, new Vector2(0, 1)),
                
                new VertexPositionColorTexture(new Vector3(-1f, -0.5f, -0.1f), Color.White, new Vector2(0, 0)),
                new VertexPositionColorTexture(new Vector3(0f, -0.5f, -0.1f), Color.White, new Vector2(1, 0)),
                new VertexPositionColorTexture(new Vector3(0f, 0.5f, -0.1f), Color.White, new Vector2(1, 1)),
                new VertexPositionColorTexture(new Vector3(-1f, 0.5f, -0.1f), Color.White, new Vector2(0, 1))
            };
            vb = new VertexBuffer(application, VertexPositionColorTexture.VertexDeclaration, 8, false);
            vb.SetData(vertices, 0, 8);

            indices = new ushort[]
            {
                0, 1, 2, 2, 3, 0,
                4, 5, 6, 6, 7, 4
            };
            indexBuffer = new IndexBuffer(application, IndexSize.Uint16, 12, false);
            indexBuffer.SetData(indices, 0, 12);

            shader = Shader.FromFiles(application, "Content/Vertex.spv", "Content/Fragment.spv");
            shader.shader1Params.AddParameter<WorldViewProjection>("ubo", 0);
            shader.shader2Params.AddTexture2DParameter("texSampler", 1);
            //shader.shader1Params.Construct();
            //shader.shader2Params.Construct();
            shader.ConstructParams();

            state = new PipelineState(application, new Viewport(0f, 0f, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration);

            using (FileStream stream = File.OpenRead("Content/tbh.png"))
            {
                texture = Texture2D.FromStream(application, stream, SamplerState.PointClamp);
            }
        }

        private static void Draw(float deltaTime)
        {
            shader.SetUniform("texSampler", texture);
            float width = 2f;
            float height = 2f * (9f / 16f);
            shader.SetUniform("ubo", new WorldViewProjection(
                Matrix4x4.Identity,
                Matrix4x4.Identity,
                Matrix4x4.CreateOrthographicOffCenter(-width, width, -height, height, -1f, 1f)
                ));

            state.Begin(Color.CornflowerBlue, renderTarget);

            Graphics.SetVertexBuffer(vb);
            Graphics.SetIndexBuffer(indexBuffer);

            Graphics.DrawIndexedPrimitives(12, 4);

            state.End();
        }

        private static void Unload()
        {
            texture.Dispose();
            state.Dispose();
            shader.Dispose();
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