using Silk.NET.Vulkan;
using Silk.NET.Core;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

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
        private readonly Application application;
        public Pipeline handle;

        public Shader[] shaders;
        public PipelineShaderStageCreateInfo[] shaderStages;
        public Silk.NET.Vulkan.Viewport viewport;
        public Rect2D scissor;
        public BlendState blendState;
        public PrimitiveTopology topology;
        public PolygonMode polygonMode;
        public VkRenderPass renderPass;
        public PipelineLayout pipelineLayout;
        public VkVertex[] vertices;

        FrontFace frontFaceMode = FrontFace.CounterClockwise;
        CullModeFlags cullMode = CullModeFlags.None;

        public PipelineColorBlendAttachmentState colorBlendAttachment;
        public PipelineVertexInputStateCreateInfo vertexInput;
        public PipelineInputAssemblyStateCreateInfo inputAssembly;
        public PipelineRasterizationStateCreateInfo rasterizer;
        public PipelineMultisampleStateCreateInfo multisampler;
        public PipelineColorBlendStateCreateInfo colorBlendingInfo;
        public PipelineViewportStateCreateInfo viewportInfo;
        public PipelineDepthStencilStateCreateInfo depthInfo;

        public VertexInputBindingDescription[] compiledVertexBindingDescriptions;
        public VertexInputAttributeDescription[] compiledVertexAttributeDescriptions;

        public VkGraphicsPipeline(
            Application application,
            Silk.NET.Vulkan.Viewport viewport,
            CullMode cullMode,
            BlendState blendState,
            PrimitiveType primitiveType,
            VkRenderPass renderPass,
            Shader shader,
            params VertexDeclaration[] vertexTypes)
        {
            this.application = application;
            this.vertices = new VkVertex[vertexTypes.Length];//new VkVertex(vertexType);
            for (int i = 0; i < this.vertices.Length; i++)
            {
                vertices[i] = new VkVertex(vertexTypes[i]);
            }
            this.viewport = viewport;
            this.scissor = new Rect2D(new Offset2D((int)viewport.X, (int)viewport.Y), new Extent2D((uint)viewport.Width, (uint)viewport.Height));
            this.cullMode = Converters.CullModeToFlags[(int)cullMode];
            this.frontFaceMode = FrontFace.Clockwise;
            this.blendState = blendState;
            this.topology = Converters.PrimitiveTypeToTopology[(int)primitiveType];
            if (this.topology == PrimitiveTopology.LineList || this.topology == PrimitiveTopology.LineStrip || this.topology == PrimitiveTopology.LineListWithAdjacency)
            {
                this.polygonMode = PolygonMode.Line;
            }
            else this.polygonMode = PolygonMode.Fill;
            this.renderPass = renderPass;
            this.shaders = new Shader[] { shader };

            switch (shader.type)
            {
                case ShaderType.VertexAndFragment:
                    this.shaderStages = new PipelineShaderStageCreateInfo[]
                    {
                        CreateShaderStage(ShaderStageFlags.VertexBit, new ShaderModule(shader.shaderHandle)),
                        CreateShaderStage(ShaderStageFlags.FragmentBit, new ShaderModule(shader.shaderHandle2))
                    };
                    break;
                case ShaderType.Tessellation:
                    this.shaderStages = new PipelineShaderStageCreateInfo[]
                    {
                        CreateShaderStage(ShaderStageFlags.TessellationControlBit, new ShaderModule(shader.shaderHandle)),
                        CreateShaderStage(ShaderStageFlags.TessellationEvaluationBit, new ShaderModule(shader.shaderHandle2))
                    };
                    break;
                default:
                    this.shaderStages = new PipelineShaderStageCreateInfo[]
                    {
                        CreateShaderStage(Converters.ShaderTypeToFlags[(int)shader.type], new ShaderModule(shader.shaderHandle))
                    };
                    break;
            }
            BuildPipeline();
        }

        public static unsafe PipelineShaderStageCreateInfo CreateShaderStage(ShaderStageFlags stage, ShaderModule shaderModule)
        {
            var createInfo = new PipelineShaderStageCreateInfo();
            createInfo.SType = StructureType.PipelineShaderStageCreateInfo;
            //what stage of the pipeline this should be created for
            createInfo.Stage = stage;
            //the shader pointer itself
            createInfo.Module = shaderModule;
            //the entry point of the shader
            createInfo.PName = Shader.Main();
            return createInfo;
        }
        internal unsafe PipelineVertexInputStateCreateInfo CreateVertexInputState()
        {
            //TODO
            var createInfo = new PipelineVertexInputStateCreateInfo();
            createInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;

            //compile vertex bindings
            uint totalBindings = (uint)vertices.Length;
            compiledVertexBindingDescriptions = new VertexInputBindingDescription[totalBindings];
            for (int i = 0; i < totalBindings; i++)
            {
                compiledVertexBindingDescriptions[i] = vertices[i].bindingDescription;
                compiledVertexBindingDescriptions[i].Binding = (uint)i;
            }

            //compile attribute bindings
            uint totalAttributes = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                totalAttributes += (uint)vertices[i].attributeDescriptions.Length;
            }
            compiledVertexAttributeDescriptions = new VertexInputAttributeDescription[totalAttributes];
            int attributeDescriptionIndex = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = 0; j < vertices[i].attributeDescriptions.Length; j++)
                {
                    compiledVertexAttributeDescriptions[attributeDescriptionIndex] = vertices[i].attributeDescriptions[j];
                    compiledVertexAttributeDescriptions[attributeDescriptionIndex].Binding = (uint)i;
                    compiledVertexAttributeDescriptions[attributeDescriptionIndex].Location = (uint)attributeDescriptionIndex;
                    attributeDescriptionIndex++;
                }
                //compiledVertexAttributeDescription += (uint)vertices[i].attributeDescriptions.Length;
            }

            createInfo.VertexBindingDescriptionCount = totalBindings;
            fixed (VertexInputBindingDescription* ptr = compiledVertexBindingDescriptions)
            {
                createInfo.PVertexBindingDescriptions = ptr;
            }

            createInfo.VertexAttributeDescriptionCount = totalAttributes;
            fixed (VertexInputAttributeDescription* ptr = compiledVertexAttributeDescriptions)
            {
                createInfo.PVertexAttributeDescriptions = ptr;
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
                info.SrcColorBlendFactor = Converters.BlendStateToFactor[(int)blendState.SourceColorBlend];
                info.SrcAlphaBlendFactor = Converters.BlendStateToFactor[(int)blendState.SourceAlphaBlend];
                info.DstColorBlendFactor = Converters.BlendStateToFactor[(int)blendState.DestinationColorBlend];
                info.DstAlphaBlendFactor = Converters.BlendStateToFactor[(int)blendState.DestinationAlphaBlend];
                info.ColorBlendOp = BlendOp.Add;
                info.AlphaBlendOp = BlendOp.Add;
                info.BlendEnable = new Bool32(true);
            }
            else info.BlendEnable = new Bool32(false);

            return info;
        }
        internal unsafe PipelineLayout CreatePipelineLayout()
        {
            if (shaders.Length > 1)
            {
                throw new NotImplementedException();
            }
            //todo: Account for shaders without descriptor sets
            uint descriptorSetLayoutCount = (uint)(shaders.Length);

            PipelineLayoutCreateInfo createInfo = new PipelineLayoutCreateInfo();
            createInfo.SType = StructureType.PipelineLayoutCreateInfo;
            createInfo.Flags = PipelineLayoutCreateFlags.None;
            createInfo.SetLayoutCount = descriptorSetLayoutCount;

            if (descriptorSetLayoutCount > 0)
            {
                fixed (DescriptorSetLayout* ptr = &shaders[0].descriptorSetLayout)
                {
                    createInfo.PSetLayouts = ptr;
                }
            }
            else
            {
                createInfo.PSetLayouts = null;
            }
            createInfo.PushConstantRangeCount = 0;
            createInfo.PPushConstantRanges = null;

            PipelineLayout layout;

            Result result = vk.CreatePipelineLayout(VkEngine.vkDevice, in createInfo, null, &layout);
            if (result != Result.Success)
            {
                throw new InitializationException("Failed to initialize Vulkan pipeline layout!");
            }

            return layout;
        }
        internal unsafe PipelineDepthStencilStateCreateInfo CreateDepthStencilState(bool depthTest, bool depthWrite, CompareOp compareOperation)
        {
            PipelineDepthStencilStateCreateInfo createInfo = new PipelineDepthStencilStateCreateInfo();
            createInfo.SType = StructureType.PipelineDepthStencilStateCreateInfo;
            createInfo.DepthTestEnable = new Bool32(depthTest);
            createInfo.DepthWriteEnable = new Bool32(depthWrite);
            createInfo.DepthCompareOp = depthTest ? compareOperation : CompareOp.Always;
            createInfo.DepthBoundsTestEnable = new Bool32(false);
            createInfo.MinDepthBounds = 0.0f; // Optional
            createInfo.MaxDepthBounds = 1.0f; // Optional
            createInfo.StencilTestEnable = new Bool32(false);

            return createInfo;
        }
        internal unsafe GraphicsPipelineCreateInfo CreateInfo()
        {
            #region create viewport info
            viewportInfo = new PipelineViewportStateCreateInfo();
            viewportInfo.SType = StructureType.PipelineViewportStateCreateInfo;

            viewportInfo.ViewportCount = 1;
            fixed (Silk.NET.Vulkan.Viewport* ptr = &viewport)
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
            float[] blendConstants = new float[4] { 1f, 1f, 1f, 1f };

            colorBlendingInfo = new PipelineColorBlendStateCreateInfo();
            colorBlendingInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
            colorBlendingInfo.LogicOpEnable = false;
            colorBlendingInfo.LogicOp = LogicOp.Copy;
            colorBlendingInfo.AttachmentCount = 1;

            colorBlendingInfo.BlendConstants[0] = 1f;
            colorBlendingInfo.BlendConstants[1] = 1f;
            colorBlendingInfo.BlendConstants[2] = 1f;
            colorBlendingInfo.BlendConstants[3] = 1f;

            colorBlendAttachment = CreateColorBlend(blendState);
            fixed (PipelineColorBlendAttachmentState* ptr = &colorBlendAttachment)
            {
                colorBlendingInfo.PAttachments = ptr;
            }
            #endregion

            pipelineLayout = CreatePipelineLayout();//VkPipelineLayout.Create();

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

            depthInfo = CreateDepthStencilState(true, true, CompareOp.LessOrEqual);
            fixed (PipelineDepthStencilStateCreateInfo* ptr = &depthInfo)
            {
                pipelineInfo.PDepthStencilState = ptr;
            }

            pipelineInfo.Layout = pipelineLayout;

            pipelineInfo.RenderPass = renderPass;
            pipelineInfo.Subpass = 0;

            return pipelineInfo;
        }

        public unsafe void Bind(CommandCollection commandBuffer, RenderStage bindType)
        {
            vk.CmdBindPipeline(new CommandBuffer(commandBuffer.handle), Converters.RenderStageToBindPoint[(int)bindType], handle);

            PushUniformUpdates(commandBuffer, bindType);
            //for (int i = )
                //vk.CmdBindDescriptorSets(new CommandBuffer(commandBuffer.handle), Converters.RenderStageToBindPoint[(int)bindType], pipelineLayout, 0, (uint)descriptorSets.Length, ptr, 0, null);
        }

        public unsafe void PushUniformUpdates(CommandCollection commandBuffer, RenderStage bindType)
        {
            for (int i = 0; i < shaders.Length; i++)
            {
                DescriptorSet currentFrameDescriptorSet = shaders[i].descriptorSet;

                if (shaders[i].useDynamicUniformBuffer)
                {
                    vk.CmdBindDescriptorSets(
                        new CommandBuffer(commandBuffer.handle),
                        Converters.RenderStageToBindPoint[(int)bindType],
                        pipelineLayout,
                        0,
                        1,
                        &currentFrameDescriptorSet,
                        (uint)VkEngine.unifiedDynamicBuffer.dataOffsets.Count,
                        VkEngine.unifiedDynamicBuffer.dataOffsets.AsReadonlySpan()
                        );
                }
                else
                {
                    vk.CmdBindDescriptorSets(
    new CommandBuffer(commandBuffer.handle),
    Converters.RenderStageToBindPoint[(int)bindType],
    pipelineLayout,
    0,
    1,
    &currentFrameDescriptorSet,
    0,
    null);
                }
            }
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
        private void BuildPipeline()
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