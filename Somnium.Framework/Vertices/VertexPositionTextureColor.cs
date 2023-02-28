using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 36)]
    public struct VertexPositionTextureColor
    {
        [FieldOffset(0)]
        public Vector3 Position; //12

        [FieldOffset(12)]
        public Vector2 UV; //20

        [FieldOffset(20)]
        public Vector4 Color; //36

        public const int size = 36;

        public static VertexDeclaration VertexDeclaration
        {
            get
            {
                return internalVertexDeclaration;
            }
        }
        internal static VertexDeclaration internalVertexDeclaration;

        public VertexPositionTextureColor(Vector3 position, Color color, Vector2 UV)
        {
            this.Position = position;
            Color = color.ToVector4();
            this.UV = UV;
        }

        public override string ToString()
        {
            return "{Position:" + this.Position + ", Color:" + this.Color + ", UV: " + UV + "}";
        }

        public static bool operator ==(VertexPositionTextureColor left, VertexPositionTextureColor right)
        {
            return ((left.Color == right.Color) && (left.Position == right.Position) && (left.UV == right.UV));
        }

        public static bool operator !=(VertexPositionTextureColor left, VertexPositionTextureColor right)
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
            return (this == ((VertexPositionTextureColor)obj));
        }
    }
}
