using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Threading;

namespace Somnium.Framework
{
    /// <summary>
    /// Represents an interchangeable state of the graphics pipeline, including what shaders is currently to be used, etc
    /// </summary>
    public class PipelineState : IDisposable
    {
        readonly Application application;
        public readonly VertexDeclaration[] vertices;
        public readonly Viewport viewport;
        public readonly CullMode cullMode;
        public readonly PrimitiveType primitiveType;
        public readonly BlendState blendState;
        public readonly Shader[] shaders;

        public GenerationalIndex handle;

        public RenderBuffer? currentRenderbuffer { get; private set; }

        public PipelineState(
            Application application, 
            Somnium.Framework.Viewport viewport, 
            CullMode cullMode,
            PrimitiveType primitiveType,
            BlendState blendState,
            Shader shader,
            params VertexDeclaration[] vertices)
        {
            this.application = application;
            this.viewport = viewport;
            this.cullMode = cullMode;
            this.primitiveType = primitiveType;
            this.blendState = blendState;
            shaders = new Shader[] { shader };
            this.vertices = vertices;

            Construct();
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (shaders.Length != 1) throw new NotImplementedException();

                    VkGraphicsPipeline pipeline = new VkGraphicsPipeline(application, viewport.ToVulkanViewport(), cullMode, blendState, primitiveType, VkEngine.renderPass, shaders[0], vertices);
                    handle = VkEngine.AddPipeline(pipeline);

                    pipeline = new VkGraphicsPipeline(application, viewport.ToVulkanViewport(), cullMode, blendState, primitiveType, VkEngine.framebufferRenderPass, shaders[0], vertices);
                    VkEngine.AddRenderbufferPipeline(pipeline);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Public call available in Graphics.SetPipeline
        /// </summary>
        /// <param name="clearColor"></param>
        /// <param name="renderTarget"></param>
        /// <param name="renderStageToBindTo"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void Begin(Color? clearColor, RenderBuffer? renderTarget = null, RenderStage renderStageToBindTo = RenderStage.Graphics)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    this.currentRenderbuffer = renderTarget;
                    VkGraphicsPipeline pipeline;
                    if (renderTarget == null)
                    {
                        pipeline = VkEngine.GetPipeline(handle);
                        VkEngine.renderPass.Begin(VkEngine.commandBuffer, VkEngine.swapChain, clearColor, renderTarget);
                    }
                    else
                    {
                        pipeline = VkEngine.GetRenderbufferPipeline(handle);
                        VkEngine.framebufferRenderPass.Begin(VkEngine.commandBuffer, null, clearColor, renderTarget);
                    }
                    pipeline.Bind(VkEngine.commandBuffer, renderStageToBindTo);
                    Interlocked.Increment(ref VkEngine.begunPipelines);
                    break;
                default:
                    throw new NotImplementedException();
            }
            //vk.CmdBindPipeline(commandBuffer.handle, PipelineBindPoint.Graphics, handle);
        }
        internal void ForceUpdateUniforms(RenderStage bindType)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    //dont care about renderbuffer here because we are ultimately only accessing the
                    //shader of the pipeline, which is shared between both renderbuffered pipeline and regular pipeline
                    VkGraphicsPipeline pipeline = VkEngine.GetPipeline(handle);
                    pipeline.PushUniformUpdates(VkEngine.commandBuffer, bindType);
                    break;
                default:
                    break;
            }
        }
        public void End()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (currentRenderbuffer == null)
                    {
                        VkEngine.renderPass.End(VkEngine.commandBuffer);
                    }
                    else VkEngine.framebufferRenderPass.End(VkEngine.commandBuffer);
                    VkEngine.unifiedDynamicBuffer.ClearDynamicOffsets();
                    Interlocked.Decrement(ref VkEngine.begunPipelines);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            //do not dispose shaders
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.DestroyPipeline(handle);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
