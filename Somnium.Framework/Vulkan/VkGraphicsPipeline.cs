using Silk.NET.Vulkan;
using Silk.NET.Core;
using System;

namespace Somnium.Framework.Vulkan
{
    //todo: make instance for multiple pipelines
    public class VkGraphicsPipeline : IDisposable
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }

        public Pipeline handle;

        public PipelineShaderStageCreateInfo[] shaderStages;
        public Viewport viewport;
        public Rect2D scissor;
        public BlendState blendState;
        public PrimitiveTopology topology;
        public PolygonMode polygonMode;
        public VkRenderPass renderPass;
        public PipelineLayout pipelineLayout;
        public VkVertex[] vertexDescriptors;

        FrontFace frontFaceMode = FrontFace.CounterClockwise;
        CullModeFlags cullMode = CullModeFlags.None;

        public PipelineColorBlendAttachmentState colorBlendAttachment;
        public PipelineVertexInputStateCreateInfo vertexInput;
        public PipelineInputAssemblyStateCreateInfo inputAssembly;
        public PipelineRasterizationStateCreateInfo rasterizer;
        public PipelineMultisampleStateCreateInfo multisampler;
        public PipelineColorBlendStateCreateInfo colorBlendingInfo;
        public PipelineViewportStateCreateInfo viewportInfo;

        public VertexInputBindingDescription[] compiledVertexBindingDescriptions;
        public VertexInputAttributeDescription[] compiledVertexAttributeDescriptions;

        public VkGraphicsPipeline(
            Viewport viewport,
            Rect2D scissor,
            FrontFace frontFace,
            CullModeFlags cullMode,
            BlendState blendState,
            PrimitiveTopology topology,
            PolygonMode polygonMode,
            VkRenderPass renderPass,
            VkVertex[] vertexDescriptors)
        {
            this.vertexDescriptors = vertexDescriptors;
            this.viewport = viewport;
            this.scissor = scissor;
            this.frontFaceMode = frontFace;
            this.cullMode = cullMode;
            shaderStages = null;

            this.blendState = blendState;
            this.topology = topology;
            this.polygonMode = polygonMode;
            this.renderPass = renderPass;
        }

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
        internal unsafe PipelineVertexInputStateCreateInfo CreateVertexInputState()
        {
            //TODO
            var createInfo = new PipelineVertexInputStateCreateInfo();
            createInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;

            uint totalAttributes = 0;

            for (int i = 0; i < vertexDescriptors.Length; i++)
            {
                totalAttributes += (uint)vertexDescriptors[i].attributeDescriptions.Length;
            }

            compiledVertexBindingDescriptions = new VertexInputBindingDescription[vertexDescriptors.Length];
            compiledVertexAttributeDescriptions = new VertexInputAttributeDescription[totalAttributes];

            int attributeDescriptionIndex = 0;
            for (int i = 0; i < vertexDescriptors.Length; i++)
            {
                compiledVertexBindingDescriptions[i] = vertexDescriptors[i].bindingDescription;
                for (int j = 0; j < vertexDescriptors[i].attributeDescriptions.Length; j++)
                {
                    compiledVertexAttributeDescriptions[attributeDescriptionIndex] = vertexDescriptors[i].attributeDescriptions[j];
                    attributeDescriptionIndex++;
                }
            }
            createInfo.VertexBindingDescriptionCount = (uint)vertexDescriptors.Length;
            createInfo.VertexAttributeDescriptionCount = totalAttributes;

            fixed (VertexInputAttributeDescription* ptr = compiledVertexAttributeDescriptions)
            {
                createInfo.PVertexAttributeDescriptions = ptr;
            }
            fixed (VertexInputBindingDescription* ptr = compiledVertexBindingDescriptions)
            {
                createInfo.PVertexBindingDescriptions = ptr;
            }

            return createInfo;
        }
        internal PipelineInputAssemblyStateCreateInfo CreateInputAssembly(PrimitiveTopology topology)
        {
            var createInfo = new PipelineInputAssemblyStateCreateInfo();
            createInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
            createInfo.Topology = topology;
            createInfo.PrimitiveRestartEnable = false;
            return createInfo;
        }
        internal PipelineRasterizationStateCreateInfo CreateRasterizationState(PolygonMode polygonMode)
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
        internal PipelineMultisampleStateCreateInfo CreateMultisampler()
        {
            var createInfo = new PipelineMultisampleStateCreateInfo();
            createInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
            createInfo.SampleShadingEnable = false;
            //1 sample per pixel, aka no multisampling
            createInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
            createInfo.MinSampleShading = 1f;
            return createInfo;
        }
        internal PipelineColorBlendAttachmentState CreateColorBlend(BlendState blendState)
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
        internal unsafe GraphicsPipelineCreateInfo CreateInfo()
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

            pipelineLayout = VkPipelineLayout.Create();

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

            pipelineInfo.Layout = pipelineLayout;

            pipelineInfo.RenderPass = renderPass;
            pipelineInfo.Subpass = 0;

            return pipelineInfo;
        }

        public void Bind(VkCommandBuffer commandBuffer)
        {
            vk.CmdBindPipeline(commandBuffer.handle, PipelineBindPoint.Graphics, handle);
        }

        public unsafe void Dispose()
        {
            vk.DestroyPipelineLayout(VkEngine.vkDevice, pipelineLayout, null);
            vk.DestroyPipeline(VkEngine.vkDevice, handle, null);
        }

        public static implicit operator Pipeline(VkGraphicsPipeline pipeline)
        {
            return pipeline.handle;
        }
        /// <summary>
        /// Builds the pipeline with the inputted variables AND shaders
        /// </summary>
        /// <exception cref="InitializationException"></exception>
        public void BuildPipeline()
        {
            if (shaderStages == null)
            {
                throw new InvalidOperationException("Cannot create Vulkan Graphics Pipeline Creation Info without shaders!");
            }

            var pipelineInfo = CreateInfo();

            unsafe
            {
                fixed (Pipeline* ptr = &handle)
                {
                    if (vk.CreateGraphicsPipelines(VkEngine.vkDevice, default, 1, pipelineInfo, null, ptr) != Result.Success)
                    {
                        throw new InitializationException("Error creating Vulkan Graphics Pipeline!");
                    }
                }
            }
        }
        /// <summary>
        /// builds multiple pipelines at the same time
        /// </summary>
        /// <param name="pipelines">all pipelines to build</param>
        /// <exception cref="InitializationException"></exception>
        public static unsafe void BuildPipelines(Span<VkGraphicsPipeline> pipelines)
        {
            GraphicsPipelineCreateInfo* pipelineInfos = stackalloc GraphicsPipelineCreateInfo[pipelines.Length];
            for (int i = 0; i < pipelines.Length; i++)
            {
                *(pipelineInfos + i) = pipelines[i].CreateInfo();
            }

            Span<Pipeline> handles = stackalloc Pipeline[pipelines.Length];
            if (vk.CreateGraphicsPipelines(VkEngine.vkDevice, default, pipelineInfos, null, handles) != Result.Success)
            {
                throw new InitializationException("Error creating Vulkan Graphics Pipelines!");
            }
            for (int i = 0; i < pipelines.Length; i++)
            {
                pipelines[i].handle = handles[i];
            }
        }
    }
}