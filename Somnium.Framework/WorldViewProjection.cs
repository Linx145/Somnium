using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ViewProjection
{
    [FieldOffset(0)]
    public Matrix4x4 View;
    [FieldOffset(0 + Mathf.MatrixSize)]
    public Matrix4x4 Projection;

    public ViewProjection(Matrix4x4 View, Matrix4x4 Projection)
    {
        this.View = View;
        this.Projection = Projection;
    }
}