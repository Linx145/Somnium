using Somnium.Framework;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Test
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TexturedInstanceVertexData
    {
        public Vector3 position;

        public int textureID;

        public TexturedInstanceVertexData(Vector3 position, int textureID)
        {
            this.position = position;
            this.textureID = textureID;
        }

        public static VertexDeclaration VertexDeclaration;

        public static void RegisterVertexDeclaration()
        {
            VertexDeclaration = VertexDeclaration.NewVertexDeclaration<TexturedInstanceVertexData>(Backends.Vulkan, VertexElementInputRate.Instance);
            VertexDeclaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            VertexDeclaration.AddElement(new VertexElement(VertexElementFormat.Int, 12));
        }
    }
}
