#if VULKAN
using Silk.NET.Vulkan;
using System;

namespace Somnium.Framework
{
    public static partial class Converters
    {
        public static readonly Format[] VertexFormatToVkFormat = new Format[]
    {
        Format.R32Sfloat,
        Format.R32G32Sfloat,
        Format.R32G32B32Sfloat,
        Format.R32G32B32A32Sfloat,
        Format.R32Sint,
        Format.R8G8B8A8Unorm,
        Format.R32Uint
    };
        public static readonly Format[] DepthFormatToVkFormat = new Format[]
        {
        Format.Undefined,
        Format.D16Unorm,
        Format.D16UnormS8Uint,
        Format.D24UnormS8Uint,
        Format.D32Sfloat
        };
        public static readonly Format[] ImageFormatToVkFormat = new Format[]
        {
        Format.R8G8B8A8Unorm,
        Format.R8G8B8A8SNorm,
        Format.R8G8B8A8Srgb,
        Format.B8G8R8A8Unorm,
        Format.B8G8R8A8SNorm,
        Format.B8G8R8A8Srgb,
        Format.R16G16B16A16Sfloat,
        Format.R32G32B32A32Sfloat,
        Format.R32Uint
        };
        public static ImageFormat VkFormatToImageFormat(Format vkFormat)
        {
            switch (vkFormat)
            {
                case Format.R8G8B8A8Unorm:
                    return ImageFormat.R8G8B8A8Unorm;
                case Format.R8G8B8A8SNorm:
                    return ImageFormat.R8G8B8A8SNorm;
                case Format.R8G8B8A8Srgb:
                    return ImageFormat.R8G8B8A8Srgb;
                case Format.B8G8R8A8Unorm:
                    return ImageFormat.B8G8R8A8Unorm;
                case Format.B8G8R8A8SNorm:
                    return ImageFormat.B8G8R8A8SNorm;
                case Format.B8G8R8A8Srgb:
                    return ImageFormat.B8G8R8A8Srgb;
                case Format.R32Uint:
                    return ImageFormat.R32Uint;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Converts the Somnium.Framework generic BlendState to the Vulkan specific BlendFactor
        /// </summary>
        public static readonly BlendFactor[] BlendStateToFactor = new BlendFactor[]
        {
            BlendFactor.One,
            BlendFactor.Zero,
            BlendFactor.SrcColor,
            BlendFactor.OneMinusSrcColor,
            BlendFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha,
            BlendFactor.DstColor,
            BlendFactor.OneMinusDstColor,
            BlendFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha
        };
        /// <summary>
        /// Converts the Somnium.Framework generic PrimitiveType to the Vulkan specific PrimitiveTopology
        /// </summary>
        public static readonly PrimitiveTopology[] PrimitiveTypeToVkTopology = new PrimitiveTopology[]
        {
            PrimitiveTopology.TriangleList,
            PrimitiveTopology.TriangleStrip,
            PrimitiveTopology.LineList,
            PrimitiveTopology.LineStrip,
            PrimitiveTopology.PointList,
            PrimitiveTopology.TriangleFan,
            PrimitiveTopology.TriangleListWithAdjacency,
            PrimitiveTopology.LineStripWithAdjacency,
        };

        public static readonly ShaderStageFlags[] ShaderTypeToVkFlags = new ShaderStageFlags[]
        {
            ShaderStageFlags.None,
            ShaderStageFlags.VertexBit,
            ShaderStageFlags.FragmentBit,
            ShaderStageFlags.None,
            ShaderStageFlags.TessellationControlBit,
            ShaderStageFlags.TessellationEvaluationBit,
            ShaderStageFlags.GeometryBit,
            ShaderStageFlags.ComputeBit
        };

        public static readonly CullModeFlags[] CullModeToVkFlags = new CullModeFlags[]
        {
            CullModeFlags.BackBit,
            CullModeFlags.FrontBit,
            CullModeFlags.None
        };

        public static readonly PipelineBindPoint[] RenderStageToVkBindPoint = new PipelineBindPoint[]
        {
            PipelineBindPoint.Graphics,
            PipelineBindPoint.Compute,
            PipelineBindPoint.RayTracingKhr
        };

        public static readonly Filter[] FilterModeToVkFilter = new Filter[]
        {
            Filter.Nearest,
            Filter.Linear
        };

        public static readonly SamplerAddressMode[] RepeatModeToVkSamplerAddressMode = new SamplerAddressMode[]
        {
            SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.Repeat,
            SamplerAddressMode.ClampToBorder
        };

        public static readonly DescriptorType[] UniformTypeToVkDescriptorType = new DescriptorType[]
        {
            DescriptorType.UniformBuffer,
            DescriptorType.SampledImage,
            DescriptorType.StorageImage,
            DescriptorType.Sampler,
            DescriptorType.CombinedImageSampler
        };
    }
}
#endif