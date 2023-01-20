using Silk.NET.Vulkan;
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

        private static Vector3[] positions;
        //private static List<Vector3[]> positionsList = new List<Vector3[]>();

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
        private static InstanceBuffer instanceBuffer;
        private static IndexBuffer indexBuffer;

        private static ViewProjection viewProjection;
        //private static WorldViewProjection[] wvps;

        private static ExtendedRandom rand;

        private static void OnLoad()
        {
            rand = new ExtendedRandom();

            float width = 26f / 16f;
            float height = 19f / 16f;
            vertices = new VertexPositionColorTexture[]
            {
                new VertexPositionColorTexture(new Vector3(-width * 0.5f, -height * 0.5f, 0f), Color.White, new Vector2(0, 0)),
                new VertexPositionColorTexture(new Vector3(width * 0.5f, -height * 0.5f, 0f), Color.White, new Vector2(1, 0)),
                new VertexPositionColorTexture(new Vector3(width * 0.5f, height * 0.5f, 0f), Color.White, new Vector2(1, 1)),
                new VertexPositionColorTexture(new Vector3(-width * 0.5f, height * 0.5f, 0f), Color.White, new Vector2(0, 1)),
            };
            vb = new VertexBuffer(application, VertexPositionColorTexture.VertexDeclaration, 4, false);
            vb.SetData(vertices, 0, 4);

            indices = new ushort[]
            {
                0, 1, 2, 2, 3, 0
            };
            indexBuffer = new IndexBuffer(application, IndexSize.Uint16, 6, false);
            indexBuffer.SetData(indices, 0, 6);

            instanceBuffer = InstanceBuffer.New<Vector3>(application, 8100);
            instanceBuffer.instanceDataDeclaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            instanceBuffer.Construct();

            shader = Shader.FromFiles(application, "Content/Vertex.spv", "Content/Fragment.spv");
            shader.shader1Params.AddParameter<ViewProjection>("matrices", 0);
            shader.shader2Params.AddTexture2DParameter("texSampler", 1);
            //shader.shader1Params.Construct();
            //shader.shader2Params.Construct();
            shader.ConstructParams();

            state = new PipelineState(application, new Somnium.Framework.Viewport(0f, 0f, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration, instanceBuffer.instanceDataDeclaration);

            using (FileStream stream = File.OpenRead("Content/tbh.png"))
            {
                texture = Texture2D.FromStream(application, stream, SamplerState.PointClamp);
            }

            float camWidth = 20f;
            float camHeight = camWidth * (9f / 16f);
            viewProjection = new ViewProjection(
                Matrix4x4.CreateTranslation(-20f, -11.25f, 0f),
                Matrix4x4.CreateOrthographicOffCenter(-camWidth, camWidth, -camHeight, camHeight, -1000f, 1000f)
                );


            /*float globalIndex = 0;
            for (int x = 0; x < 2; x++)
            {*/
            positions = new Vector3[instanceBuffer.instanceCount];
            for (int i = 0; i < 90; i++)
            {
                for (int j = 0; j < 90; j++)
                {
                    positions[i * 90 + j] = new Vector3(rand.NextFloat(0f, 40f), rand.NextFloat(0f, 22.5f), (i * 90 + j) * 0.005f);//new Vector3(i * (40f / 64f), j * (22.5f / 64f), 0f);
                }
            }
            instanceBuffer.SetData(positions);
            /*positionsList.Add(positions);
        }*/
        }

        private static void Draw(float deltaTime)
        {
            shader.SetUniform("texSampler", texture);

            shader.SetUniform("matrices", viewProjection);

            state.Begin(Color.CornflowerBlue, renderTarget);

            Graphics.SetVertexBuffer(vb, 0);
            Graphics.SetIndexBuffer(indexBuffer);

            Graphics.SetInstanceBuffer(instanceBuffer, 1);

            //for (int i = 0; i < positionsList.Count; i++)
            //{
            //instanceBuffer.SetData(positionsList[0]);
            Graphics.DrawIndexedPrimitives(6, instanceBuffer.instanceCount);
            //}

            state.End();
        }

        private static void Unload()
        {
            texture.Dispose();
            state.Dispose();
            shader.Dispose();
            vb.Dispose();
            indexBuffer.Dispose();
            instanceBuffer.Dispose();
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