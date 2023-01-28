using System;

namespace Somnium.Framework;

public enum ShaderType
{
    VertexAndFragment, Vertex, Fragment, Tessellation, TessellationControl, TessellationEvaluation, Geometry, Compute
}

[Flags]
public enum ShaderTypeFlags
{
    None = 0,
    Vertex = 1,
    Fragment = 2,
    TessellationControl = 4,
    TessellationEvaluation = 8,
    Geometry = 16,
    Compute = 32
}