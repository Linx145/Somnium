using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionTexture
    {
        public Vector3 Position;

        public Vector2 UV;

        public static VertexDeclaration VertexDeclaration
        {
            get
            {
                return internalVertexDeclaration;
            }
        }
        internal static VertexDeclaration internalVertexDeclaration;

        public VertexPositionTexture(Vector3 position, Vector2 UV)
        {
            this.Position = position;
            this.UV = UV;
        }

        public override string ToString()
        {
            return "{Position:" + this.Position + ", UV: " + UV + "}";
        }

        public static bool operator ==(VertexPositionTexture left, VertexPositionTexture right)
        {
            return ((left.Position == right.Position) && (left.UV == right.UV));
        }

        public static bool operator !=(VertexPositionTexture left, VertexPositionTexture right)
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
            return (this == ((VertexPositionTexture)obj));
        }
    }
}
