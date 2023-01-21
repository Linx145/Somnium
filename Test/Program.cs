using Silk.NET.Vulkan;
using Somnium.Framework;
using Somnium.Framework.Vulkan;
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

        private static Vector4[] positions;
        private static Vector3[] velocities;

        private static VertexPositionColorTexture[] vertices;
        private static ushort[] indices;
        private static VertexBuffer vb;
        private static IndexBuffer indexBuffer;

        private static InstanceBuffer instanceBuffer;
        private static VertexDeclaration instanceDataDeclaration;

        private static ViewProjection viewProjection;
        //private static WorldViewProjection[] wvps;

        private static ExtendedRandom rand;

        const int instanceCount = 10000;
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
                new VertexPositionColorTexture(new Vector3(-width * 0.5f, height * 0.5f, 0f), Color.White, new Vector2(0, 1))
            };
            vb = new VertexBuffer(application, VertexPositionColorTexture.VertexDeclaration, vertices.Length, false);
            vb.SetData(vertices, 0, vertices.Length);

            indices = new ushort[]
            {
                0, 1, 2, 2, 3, 0
            };
            indexBuffer = new IndexBuffer(application, IndexSize.Uint16, indices.Length, false);
            indexBuffer.SetData(indices, 0, indices.Length);

            /*shader = Shader.FromFiles(application, "Content/Shader.vert.spv", "Content/Shader.frag.spv");
            shader.shader1Params.AddParameter<ViewProjection>("matrices", 0);
            shader.shader1Params.AddParameter<Vector4>("positions", 1, UniformType.uniformBuffer, instanceCount);
            shader.shader2Params.AddTexture2DParameter("texSampler", 2);
            //shader.shader1Params.Construct();
            //shader.shader2Params.Construct();
            shader.ConstructParams();

            state = new PipelineState(application, new Somnium.Framework.Viewport(0f, 0f, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration);
            */
            shader = Shader.FromFiles(application, "Content/ShaderInstanced.vert.spv", "Content/ShaderInstanced.frag.spv");
            shader.shader1Params.AddParameter<ViewProjection>("matrices", 0);
            shader.shader2Params.AddTexture2DParameter("texSampler", 1);
            shader.ConstructParams();

            instanceDataDeclaration = VertexDeclaration.NewVertexDeclaration<Vector4>(Backends.Vulkan, VertexElementInputRate.Instance);
            instanceDataDeclaration.AddElement(new VertexElement(VertexElementFormat.Vector4, 0));
            state = new PipelineState(application, new Somnium.Framework.Viewport(0f, 0f, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration, instanceDataDeclaration);
            instanceBuffer = InstanceBuffer.New<Vector4>(application, instanceCount);

            using (FileStream stream = File.OpenRead("Content/tbh.png"))
            {
                texture = Texture2D.FromStream(application, stream, SamplerState.PointClamp);
            }

            float camWidth = 20f;
            float camHeight = camWidth * (9f / 16f);
            viewProjection = new ViewProjection(
                Matrix4x4.CreateTranslation(0f, 0f, 0f),
                Matrix4x4.CreateOrthographicOffCenter(-camWidth, camWidth, -camHeight, camHeight, -1000f, 1000f)
                );

            positions = new Vector4[instanceCount];
            velocities = new Vector3[instanceCount];

            for (int i = 0; i < positions.Length; i++)
            {
                velocities[i] = new Vector3(rand.NextFloat(-2f, 2f), rand.NextFloat(-2f, 2f), 0f);
                positions[i] = new Vector4(rand.NextFloat(-15f, 15f), rand.NextFloat(-7f, 7f), (i) * 0.005f, 0f);//new Vector3(i * (40f / 64f), j * (22.5f / 64f), 0f);
            }
            //positions[0] = new Vector4(0f, 0f, 0f, 0f);
            
        }
        private static void Draw(float deltaTime)
        {
            instanceBuffer.SetData(positions);

            var commandBuffer = VkEngine.commandBuffer;

            commandBuffer.Reset();
            commandBuffer.Begin();

            shader.SetUniform("texSampler", texture);
            //shader.SetUniforms("positions", positions);
            shader.SetUniform("matrices", viewProjection);
        
            Graphics.SetPipeline(state, Color.CornflowerBlue);
            Graphics.SetVertexBuffer(vb, 0);
            Graphics.SetInstanceBuffer(instanceBuffer, 1);

            Graphics.SetIndexBuffer(indexBuffer);

            Graphics.DrawIndexedPrimitives(6, instanceCount);

            state.End();
        }

        private static void Update(float deltaTime)
        {
            recordTime += deltaTime;
            if (recordTime >= 0.2f)
            {
                application.Window.Title = string.Concat("Test ", MathF.Round(1f / deltaTime).ToString());
                recordTime -= 0.2f;
            }
            for (int i = 0; i < instanceCount; i++)
            {
                positions[i] += new Vector4(velocities[i], 0f) * deltaTime;
            }
            //Here all updates to the program should be done.
        }

        private static void Unload()
        {
            texture.Dispose();
            state.Dispose();
            shader.Dispose();
            vb.Dispose();
            indexBuffer.Dispose();

            instanceBuffer?.Dispose();
        }
    }
}