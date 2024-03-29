﻿using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct WorldViewProjection
{
    [FieldOffset(0)]
    public Matrix4x4 World;
    [FieldOffset(Mathf.MatrixSize)]
    public Matrix4x4 View;
    [FieldOffset(Mathf.MatrixSize * 2)]
    public Matrix4x4 Projection;

    public WorldViewProjection(Matrix4x4 World, Matrix4x4 View, Matrix4x4 Projection)
    {
        this.World = World;
        this.View = View;
        this.Projection = Projection;
    }
}