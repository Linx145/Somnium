using System;
using System.Numerics;

public struct WorldViewProjection
{
    Matrix4x4 World;
    Matrix4x4 View;
    Matrix4x4 Projection;

    public WorldViewProjection(Matrix4x4 World, Matrix4x4 View, Matrix4x4 Projection)
    {
        this.World = World;
        this.View = View;
        this.Projection = Projection;
    }
}