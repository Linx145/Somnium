using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionColor
    {
        public Vector3 Position;

        public Color Color;

        public static VertexDeclaration VertexDeclaration
        {
            get
            {
                return internalVertexDeclaration;
            }
        }
        internal static VertexDeclaration internalVertexDeclaration;

        public VertexPositionColor(Vector3 position, Color color)
        {
            this.Position = position;
            Color = color;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ Color.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "{{Position:" + this.Position + " Color:" + this.Color + "}}";
        }

        public static bool operator ==(VertexPositionColor left, VertexPositionColor right)
        {
            return ((left.Color == right.Color) && (left.Position == right.Position));
        }

        public static bool operator !=(VertexPositionColor left, VertexPositionColor right)
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
            return (this == ((VertexPositionColor)obj));
        }
    }
}
