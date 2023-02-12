
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionColorTexture
    {
        public Vector3 Position;

        public Vector4 Color;

        public Vector2 UV;

        public static VertexDeclaration VertexDeclaration
        {
            get
            {
                return internalVertexDeclaration;
            }
        }
        internal static VertexDeclaration internalVertexDeclaration;

        public VertexPositionColorTexture(Vector3 position, Color color, Vector2 UV)
        {
            this.Position = position;
            Color = color.ToVector4();
            this.UV = UV;
        }

        public override string ToString()
        {
            return "{Position:" + this.Position + ", Color:" + this.Color + ", UV: " + UV + "}";
        }

        public static bool operator ==(VertexPositionColorTexture left, VertexPositionColorTexture right)
        {
            return ((left.Color == right.Color) && (left.Position == right.Position) && (left.UV == right.UV));
        }

        public static bool operator !=(VertexPositionColorTexture left, VertexPositionColorTexture right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != base.GetType())
            {
                return false;
            }
            return (this == ((VertexPositionColorTexture)obj));
        }
    }
}
