#if WGPU
using Silk.NET.WebGPU;

namespace Somnium.Framework
{
    public static partial class Converters
    {
        public static readonly BlendFactor[] BlendStateToFactor = new BlendFactor[]
        {
            BlendFactor.One,
            BlendFactor.Zero,
            BlendFactor.Src,
            BlendFactor.OneMinusSrc,
            BlendFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha,
            BlendFactor.Dst,
            BlendFactor.OneMinusDst,
            BlendFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha
        };
        public static readonly TextureFormat[] DepthFormatToWGPUFormat = new TextureFormat[]
{
        TextureFormat.Undefined,
        TextureFormat.Depth16Unorm,
        TextureFormat.Undefined,
        TextureFormat.Depth24PlusStencil8,
        TextureFormat.Depth32float
};
        public static readonly Silk.NET.WebGPU.FilterMode[] FilterModeToWGPUFilter = new Silk.NET.WebGPU.FilterMode[]
{
            Silk.NET.WebGPU.FilterMode.Nearest,
            Silk.NET.WebGPU.FilterMode.Linear
};
        public static readonly AddressMode[] RepeatModeToWGPUSamplerAddressMode = new AddressMode[]
{
            AddressMode.ClampToEdge,
            AddressMode.Repeat,
            AddressMode.ClampToEdge
};
        public static readonly TextureFormat[] ImageFormatToWGPUFormat = new TextureFormat[]
{
        TextureFormat.Rgba8Unorm,
        TextureFormat.Rgba8Snorm,
        TextureFormat.Rgba8UnormSrgb,
        TextureFormat.Bgra8Unorm,
        TextureFormat.Undefined,
        TextureFormat.Bgra8UnormSrgb,
        TextureFormat.Rgba16float,
        TextureFormat.Rgba32float,
        TextureFormat.R32Uint
};
        public static readonly uint[] ImageFormatToBytes = new uint[]
        {
            32,
            32,
            32,
            32,
            0,
            32,
            64,
            128,
            32
        };
        public static readonly PrimitiveTopology[] PrimitiveTypeToWGPUTopology = new PrimitiveTopology[]
{
            PrimitiveTopology.TriangleList,
            PrimitiveTopology.TriangleStrip,
            PrimitiveTopology.LineList,
            PrimitiveTopology.LineStrip,
            PrimitiveTopology.PointList,
            PrimitiveTopology.TriangleList,
            PrimitiveTopology.TriangleList,
            PrimitiveTopology.LineStrip,
};
        public static readonly Silk.NET.WebGPU.CullMode[] CullModeToWGPUCullMode = new Silk.NET.WebGPU.CullMode[]
        {
            Silk.NET.WebGPU.CullMode.Back,
            Silk.NET.WebGPU.CullMode.Back,
            Silk.NET.WebGPU.CullMode.None
        };
    }
}
#endif