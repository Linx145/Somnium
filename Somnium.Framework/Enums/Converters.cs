using Silk.NET.Vulkan;
using System;

namespace Somnium.Framework;

public static class Converters
{
    public static readonly Format[] ImageFormatToVkFormat = new Format[]
    {
        Format.R8G8B8A8Unorm,
        Format.R8G8B8A8SNorm,
        Format.R8G8B8A8Srgb,
        Format.B8G8R8A8Unorm,
        Format.B8G8R8A8SNorm,
        Format.B8G8R8A8Srgb
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
    public static readonly PrimitiveTopology[] PrimitiveTypeToTopology = new PrimitiveTopology[]
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

    public static readonly ShaderStageFlags[] ShaderTypeToFlags = new ShaderStageFlags[]
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

    public static readonly CullModeFlags[] CullModeToFlags = new CullModeFlags[]
    {
            CullModeFlags.BackBit,
            CullModeFlags.FrontBit,
            CullModeFlags.None
    };

    public static readonly PipelineBindPoint[] RenderStageToBindPoint = new PipelineBindPoint[]
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