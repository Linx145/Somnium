using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionNormalTexture
    {
        public Vector3 Position;

        public Vector3 Normal;

        public Vector2 UV;

        public static VertexDeclaration VertexDeclaration
        {
            get
            {
                return internalVertexDeclaration;
            }
        }
        internal static VertexDeclaration internalVertexDeclaration;

        public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 UV)
        {
            this.Position = position;
            this.Normal = normal;
            this.UV = UV;
        }

        public override string ToString()
        {
            return "{Position:" + this.Position + ", UV: " + UV + "}";
        }

        public static bool operator ==(VertexPositionNormalTexture left, VertexPositionNormalTexture right)
        {
            return ((left.Position == right.Position) && (left.UV == right.UV) && (left.Normal == right.Normal));
        }

        public static bool operator !=(VertexPositionNormalTexture left, VertexPositionNormalTexture right)
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
            return (this == ((VertexPositionNormalTexture)obj));
        }
    }
}
