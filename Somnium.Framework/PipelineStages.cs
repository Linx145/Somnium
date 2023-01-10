using System;

namespace Somnium.Framework
{
    [Flags]
    public enum PipelineStages
    {
        VertexShader, Tessellation, GeometryShader, FragmentShader, ComputeShader
    }
}
