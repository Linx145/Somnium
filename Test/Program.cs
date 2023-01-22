using Somnium.Framework;
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

        private static VertexPositionColorTexture[] vertices;
        private static ushort[] indices;
        private static VertexBuffer vb;
        private static IndexBuffer indexBuffer;

#if INSTANCING
        private static Vector4[] positions;
        private static Vector3[] velocities;

        private static ExtendedRandom rand;

        private static InstanceBuffer instanceBuffer;
        private static VertexDeclaration instanceDataDeclaration;

        const int instanceCount = 1000;
#endif

#if RENDERBUFFERS
        private static RenderBuffer renderBuffer;
#endif

        private static ViewProjection viewProjection;

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

            #region test: render buffers
#if RENDERBUFFERS
            shader = Shader.FromFiles(application, "Content/Shader.vert.spv", "Content/Shader.frag.spv");
            shader.shader1Params.AddParameter<ViewProjection>("matrices", 0);
            shader.shader2Params.AddTexture2DParameter("texSampler", 1);
            shader.ConstructParams();

            renderBuffer = new RenderBuffer(application, 16, 16, ImageFormat.R8G8B8A8Unorm, DepthFormat.Depth32);

            state = new PipelineState(application, new Viewport(0, 0, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration);
#endif
            #endregion

            #region test: regular drawing
#if DRAWING
            shader = Shader.FromFiles(application, "Content/Shader.vert.spv", "Content/Shader.frag.spv");
            shader.shader1Params.AddParameter<ViewProjection>("matrices", 0);
            shader.shader2Params.AddTexture2DParameter("texSampler", 1);
            shader.ConstructParams();

            state = new PipelineState(application, new Viewport(0f, 0f, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration);
#endif
            #endregion

            #region test: instancing
#if INSTANCING
            rand = new ExtendedRandom();
            shader = Shader.FromFiles(application, "Content/ShaderInstanced.vert.spv", "Content/ShaderInstanced.frag.spv");
            shader.shader1Params.AddParameter<ViewProjection>("matrices", 0);
            shader.shader2Params.AddTexture2DParameter("texSampler", 1);
            shader.ConstructParams();

            instanceDataDeclaration = VertexDeclaration.NewVertexDeclaration<Vector4>(Backends.Vulkan, VertexElementInputRate.Instance);
            instanceDataDeclaration.AddElement(new VertexElement(VertexElementFormat.Vector4, 0));
            
            state = new PipelineState(application, new Viewport(0f, 0f, 1920, 1080, 0, 1), CullMode.CullCounterClockwise, PrimitiveType.TriangleList, BlendState.NonPremultiplied, shader, VertexPositionColorTexture.VertexDeclaration, instanceDataDeclaration);
            instanceBuffer = InstanceBuffer.New<Vector4>(application, instanceCount);

            positions = new Vector4[instanceCount];
            velocities = new Vector3[instanceCount];

            for (int i = 0; i < positions.Length; i++)
            {
                velocities[i] = new Vector3(rand.NextFloat(-2f, 2f), rand.NextFloat(-2f, 2f), 0f);
                positions[i] = new Vector4(rand.NextFloat(-20f, 20f), rand.NextFloat(-11.25f, 11.25f), (i) * 0.005f, 0f);//new Vector3(i * (40f / 64f), j * (22.5f / 64f), 0f);
            }
#endif
            #endregion

            #region uniforms
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
            #endregion
        }
        private static void Draw(float deltaTime)
        {
#if INSTANCING
            instanceBuffer.SetData(positions);
#endif

            var commandBuffer = application.Window.GetDefaultCommandCollection();

            commandBuffer.Begin();

            shader.SetUniform("texSampler", texture);
            shader.SetUniform("matrices", viewProjection);

#if RENDERBUFFERS
            Graphics.SetPipeline(state, Color.Transparent, renderBuffer);
            Graphics.SetVertexBuffer(vb, 0);
            Graphics.SetIndexBuffer(indexBuffer);
            Graphics.DrawIndexedPrimitives(6, 1);
            state.End();

            Graphics.SetPipeline(state, Color.CornflowerBlue, null);
            
            Graphics.SetVertexBuffer(vb, 0);
            Graphics.SetIndexBuffer(indexBuffer);
            Graphics.DrawIndexedPrimitives(6, 1);
            //Graphics.SetPipeline(state, Color.CornflowerBlue, null);
#endif
#if DRAWING
            Graphics.SetPipeline(state, Color.CornflowerBlue);
            Graphics.SetVertexBuffer(vb, 0);
            Graphics.SetIndexBuffer(indexBuffer);
            Graphics.DrawIndexedPrimitives(6, 1);
#endif
#if INSTANCING
            Graphics.SetPipeline(state, Color.CornflowerBlue);
            Graphics.SetVertexBuffer(vb, 0);
            Graphics.SetIndexBuffer(indexBuffer);
            Graphics.SetInstanceBuffer(instanceBuffer, 1);

            Graphics.DrawIndexedPrimitives(6, instanceCount);
#endif

            state.End();
            commandBuffer.End();
        }

        private static void Update(float deltaTime)
        {
            recordTime += deltaTime;
            if (recordTime >= 0.2f)
            {
                application.Window.Title = string.Concat("Test ", MathF.Round(1f / deltaTime).ToString());
                recordTime -= 0.2f;
            }
#if INSTANCING
            Parallel.For(0, instanceCount, (int i) => { positions[i] += new Vector4(velocities[i], 0f) * deltaTime; });
#endif
        }

        private static void Unload()
        {
            texture.Dispose();
            state.Dispose();
            shader.Dispose();
            vb.Dispose();
            indexBuffer.Dispose();

#if INSTANCING
            instanceBuffer?.Dispose();
#endif
#if RENDERBUFFERS
            renderBuffer?.Dispose();
#endif
        }
    }
}