using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework.Vulkan
{
    public static class VkGraphicsPipeline
    {
        public static PipelineShaderStageCreateInfo[] shaderStages;

        public static Viewport viewport;
        public static Rect2D scissor;

        static PipelineLayout pipelineLayout;

        static FrontFace frontFaceMode = FrontFace.CounterClockwise;
        static CullModeFlags cullMode = CullModeFlags.None;

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

        public static unsafe PipelineShaderStageCreateInfo CreateShaderStage(ShaderStageFlags stage, ShaderModule shaderModule)
        {
            var createInfo = new PipelineShaderStageCreateInfo();
            createInfo.SType = StructureType.PipelineShaderStageCreateInfo;
            //what stage of the pipeline this should be created for
            createInfo.Stage = stage;
            //the shader pointer itself
            createInfo.Module = shaderModule;
            //the entry point of the shader
            createInfo.PName = VkShader.Main();
            return createInfo;
        }
        public static PipelineVertexInputStateCreateInfo CreateVertexInputState()
        {
            //TODO
            var createInfo = new PipelineVertexInputStateCreateInfo();
            createInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
            createInfo.VertexAttributeDescriptionCount = 0;
            createInfo.VertexBindingDescriptionCount = 0;

            return createInfo;
        }
        public static PipelineInputAssemblyStateCreateInfo CreateInputAssembly(PrimitiveTopology topology)
        {
            var createInfo = new PipelineInputAssemblyStateCreateInfo();
            createInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
            createInfo.Topology = topology;
            createInfo.PrimitiveRestartEnable = false;
            return createInfo;
        }
        public static PipelineRasterizationStateCreateInfo CreateRasterizationState(PolygonMode polygonMode)
        {
            var createInfo = new PipelineRasterizationStateCreateInfo();
            createInfo.SType = StructureType.PipelineRasterizationStateCreateInfo;
            createInfo.DepthClampEnable = false;
            //this will discard triangles before they make it to rasterization, so set it to false
            //since we are interested in drawing triangles, not doing black magic with them
            createInfo.RasterizerDiscardEnable = false;
            createInfo.PolygonMode = polygonMode;
            createInfo.LineWidth = 1f;
            createInfo.CullMode = cullMode;
            createInfo.FrontFace = frontFaceMode;

            createInfo.DepthBiasEnable = false;
            createInfo.DepthBiasConstantFactor = 0f;
            createInfo.DepthBiasClamp = 0f;
            createInfo.DepthBiasSlopeFactor = 0f;

            return createInfo;
        }
        public static PipelineMultisampleStateCreateInfo CreateMultisampler()
        {
            var createInfo = new PipelineMultisampleStateCreateInfo();
            createInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
            createInfo.SampleShadingEnable = false;
            //1 sample per pixel, aka no multisampling
            createInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
            createInfo.MinSampleShading = 1f;
            return createInfo;
        }
        public static PipelineColorBlendAttachmentState CreateColorBlend(BlendState blendState)
        {
            var createInfo = new PipelineColorBlendAttachmentState();
            createInfo.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
            if (blendState != null)
            {
                createInfo.SrcColorBlendFactor = BlendStateToFactor[(int)blendState.SourceColorBlend];
                createInfo.SrcAlphaBlendFactor = BlendStateToFactor[(int)blendState.SourceAlphaBlend];
                createInfo.DstColorBlendFactor = BlendStateToFactor[(int)blendState.DestinationColorBlend];
                createInfo.DstAlphaBlendFactor = BlendStateToFactor[(int)blendState.DestinationAlphaBlend];
                createInfo.BlendEnable = true;
            }
            else createInfo.BlendEnable = false;

            return createInfo;
        }
        public static unsafe GraphicsPipelineCreateInfo CreateInfo(BlendState blendState, PrimitiveTopology topology, PolygonMode polygonMode, RenderPass pass, PipelineLayout layout)
        {
            #region create viewport info
            var viewportInfo = new PipelineViewportStateCreateInfo();
            viewportInfo.SType = StructureType.PipelineViewportStateCreateInfo;

            viewportInfo.ViewportCount = 1;
            fixed (Viewport* ptr = &viewport)
            {
                viewportInfo.PViewports = ptr;
            }
            viewportInfo.ScissorCount = 1;
            fixed (Rect2D* ptr = &scissor)
            {
                viewportInfo.PScissors = ptr;
            }
            #endregion

            #region create color blending info
            var colorBlendingInfo = new PipelineColorBlendStateCreateInfo();
            colorBlendingInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
            colorBlendingInfo.LogicOpEnable = false;
            colorBlendingInfo.LogicOp = LogicOp.Copy;
            colorBlendingInfo.AttachmentCount = 1;
            PipelineColorBlendAttachmentState colorBlendAttachment = CreateColorBlend(blendState);
            colorBlendingInfo.PAttachments = &colorBlendAttachment;
            #endregion

            GraphicsPipelineCreateInfo pipelineInfo = new GraphicsPipelineCreateInfo();
            pipelineInfo.SType = StructureType.GraphicsPipelineCreateInfo;

            //specify shader stages
            pipelineInfo.StageCount = (uint)shaderStages.Length;
            fixed (PipelineShaderStageCreateInfo* ptr = shaderStages)
            {
                pipelineInfo.PStages = ptr;
            }

            var vertexInput = CreateVertexInputState();
            pipelineInfo.PVertexInputState = &vertexInput;

            var inputAssembly = CreateInputAssembly(topology);
            pipelineInfo.PInputAssemblyState = &inputAssembly;

            pipelineInfo.PViewportState = &viewportInfo;

            var rasterizer = CreateRasterizationState(polygonMode);
            pipelineInfo.PRasterizationState = &rasterizer;

            var multisampler = CreateMultisampler();
            pipelineInfo.PMultisampleState = &multisampler;

            pipelineInfo.PColorBlendState = &colorBlendingInfo;

            pipelineInfo.Layout = layout;

            pipelineInfo.RenderPass = pass;
            pipelineInfo.Subpass = 0;

            return pipelineInfo;
        }
    }
}
