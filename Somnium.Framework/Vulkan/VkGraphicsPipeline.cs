using Silk.NET.Vulkan;
using Silk.NET.Core;

namespace Somnium.Framework.Vulkan
{
    //todo: make instance for multiple pipelines
    public static class VkGraphicsPipeline
    {
        public static PipelineShaderStageCreateInfo[] shaderStages;

        public static Viewport viewport;
        public static Rect2D scissor;

        static PipelineLayout pipelineLayout;

        static FrontFace frontFaceMode = FrontFace.CounterClockwise;
        static CullModeFlags cullMode = CullModeFlags.None;

        private static PipelineColorBlendAttachmentState colorBlendAttachment;
        private static PipelineVertexInputStateCreateInfo vertexInput;
        private static PipelineInputAssemblyStateCreateInfo inputAssembly;
        private static PipelineRasterizationStateCreateInfo rasterizer;
        private static PipelineMultisampleStateCreateInfo multisampler;
        private static PipelineColorBlendStateCreateInfo colorBlendingInfo;
        private static PipelineViewportStateCreateInfo viewportInfo;

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
            var info = new PipelineColorBlendAttachmentState();
            info.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
            if (blendState != null)
            {
                info.SrcColorBlendFactor = BlendStateToFactor[(int)blendState.SourceColorBlend];
                info.SrcAlphaBlendFactor = BlendStateToFactor[(int)blendState.SourceAlphaBlend];
                info.DstColorBlendFactor = BlendStateToFactor[(int)blendState.DestinationColorBlend];
                info.DstAlphaBlendFactor = BlendStateToFactor[(int)blendState.DestinationAlphaBlend];
                info.BlendEnable = new Bool32(true);
            }
            else info.BlendEnable = new Bool32(false);

            return info;
        }
        public static unsafe GraphicsPipelineCreateInfo CreateInfo(BlendState blendState, PrimitiveTopology topology, PolygonMode polygonMode, RenderPass pass, PipelineLayout layout)
        {
            #region create viewport info
            viewportInfo = new PipelineViewportStateCreateInfo();
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
            colorBlendingInfo = new PipelineColorBlendStateCreateInfo();
            colorBlendingInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
            colorBlendingInfo.LogicOpEnable = false;
            colorBlendingInfo.LogicOp = LogicOp.Copy;
            colorBlendingInfo.AttachmentCount = 1;

            colorBlendAttachment = CreateColorBlend(blendState);
            fixed (PipelineColorBlendAttachmentState* ptr = &colorBlendAttachment)
            {
                colorBlendingInfo.PAttachments = ptr;
            }
            #endregion

            GraphicsPipelineCreateInfo pipelineInfo = new GraphicsPipelineCreateInfo();
            pipelineInfo.SType = StructureType.GraphicsPipelineCreateInfo;

            //specify shader stages
            pipelineInfo.StageCount = (uint)shaderStages.Length;
            fixed (PipelineShaderStageCreateInfo* ptr = shaderStages)
            {
                pipelineInfo.PStages = ptr;
            }

            vertexInput = CreateVertexInputState();
            fixed (PipelineVertexInputStateCreateInfo* ptr = &vertexInput)
            {
                pipelineInfo.PVertexInputState = ptr;
            }

            inputAssembly = CreateInputAssembly(topology);
            fixed (PipelineInputAssemblyStateCreateInfo* ptr = &inputAssembly)
            {
                pipelineInfo.PInputAssemblyState = ptr;
            }

            fixed (PipelineViewportStateCreateInfo* ptr = &viewportInfo)
            {
                pipelineInfo.PViewportState = ptr;
            }

            rasterizer = CreateRasterizationState(polygonMode);
            fixed (PipelineRasterizationStateCreateInfo* ptr = &rasterizer)
            {
                pipelineInfo.PRasterizationState = ptr;
            }

            multisampler = CreateMultisampler();
            fixed (PipelineMultisampleStateCreateInfo* ptr = &multisampler)
            {
                pipelineInfo.PMultisampleState = ptr;
            }

            fixed (PipelineColorBlendStateCreateInfo* ptr = &colorBlendingInfo)
            {
                pipelineInfo.PColorBlendState = ptr;
            }

            pipelineInfo.Layout = layout;

            pipelineInfo.RenderPass = pass;
            pipelineInfo.Subpass = 0;

            return pipelineInfo;
        }
    }
}
