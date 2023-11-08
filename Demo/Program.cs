using System.Numerics;
using Somnium.Framework;

namespace Demo
{
    internal class Program
    {
        private static Application application;
        private static Graphics Graphics;

        private static VertexBuffer vb;
        private static Shader shader;
        private static PipelineState pipelineState;

        static void Main(string[] args)
        {
            //execution order is
            //OnLoad()
            //every frame: Update(), Draw()
            //Unload() when program exits (when you press the window's X button)
            application = Application.New(new Application(), "Demo", new Point(1920, 1080), "Demo Window", Backends.Vulkan, 1, false);
            
            application.OnLoadCallback = OnLoad;
            application.UpdateCallback = Update;
            application.DrawCallback = Draw;
            application.UnloadCallback = Unload;
            //application.FramesPerSecond = 60;
            Graphics = application.Graphics;
            application.Start();

            //dispose is needed when you want to dispose memory allocated in another language's dll (Such as C++)
            //By default, you don't need to dispose most objects that are purely c# classes
            application.Dispose();
        }

        public static void OnLoad()
        {
            //this means our triangle has for each vertex (corner), a position of the vertex and it's color
            vb = new VertexBuffer(application, VertexPositionColor.VertexDeclaration, 3, false);
            vb.SetData(new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(-1f, 1f, 0f), Color.Red),
                new VertexPositionColor(new Vector3(0f, -1f, 0f), Color.Green),
                new VertexPositionColor(new Vector3(1f, 1f, 0f), Color.Blue)
            }, 0, 3);
            shader = Shader.FromFile(application, "Content/Shader.shader");
            pipelineState = new PipelineState(application, CullMode.CullNone, PrimitiveType.TriangleList, BlendState.AlphaBlend, shader, true, false, VertexPositionColor.VertexDeclaration);
        }
        public static void Update(float deltaTime)
        {

        }
        public static void Draw(float deltaTime)
        {
            //clear the screen
            Graphics.ClearBuffer(Color.Black);
            //set which shader we are going to use, alongside it's data & state
            Graphics.SetPipeline(pipelineState);
            //set the vertex buffer containing our triangle
            Graphics.SetVertexBuffer(vb);
            //draw the triangle (3 vertices, 1 triangle)
            Graphics.DrawPrimitives(3, 1);
            //end the shader
            Graphics.EndPipeline();
        }
        public static void Unload()
        {
            //because the resources we created interface with the GPU AND C++ backend, we need to dispose
            //of them as they are not accounted for by c#'s managed garbage collector
            pipelineState.Dispose();
            shader.Dispose();
            vb.Dispose();
        }
    }
}