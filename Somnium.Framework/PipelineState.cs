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
        public bool begun { get; private set; }

        readonly Application application;
        public readonly VertexDeclaration[] vertices;
        public readonly CullMode cullMode;
        public readonly PrimitiveType primitiveType;
        public readonly BlendState blendState;
        public readonly Shader[] shaders;

        public GenerationalIndex handle;

        public PipelineState(
            Application application,
            CullMode cullMode,
            PrimitiveType primitiveType,
            BlendState blendState,
            Shader shader,
            params VertexDeclaration[] vertices)
        {
            this.application = application;
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

                    VkGraphicsPipeline pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, VkEngine.renderPass, shaders[0], vertices);
                    handle = VkEngine.AddPipeline(pipeline);

                    pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, VkEngine.framebufferRenderPass, shaders[0], vertices);
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
        internal void Begin(RenderStage renderStageToBindTo = RenderStage.Graphics, bool autoUpdateUniforms = true)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    VkGraphicsPipeline pipeline;
                    if (application.Graphics.currentRenderbuffer == null)
                    {
                        pipeline = VkEngine.GetPipeline(handle);
                        VkEngine.SetRenderPass(VkEngine.renderPass, application.Graphics.currentRenderbuffer);
                        //VkEngine.renderPass.Begin(VkEngine.commandBuffer, VkEngine.swapChain, renderTarget);
                    }
                    else
                    {
                        pipeline = VkEngine.GetRenderbufferPipeline(handle);
                        VkEngine.SetRenderPass(VkEngine.framebufferRenderPass, application.Graphics.currentRenderbuffer);
                        //VkEngine.framebufferRenderPass.Begin(VkEngine.commandBuffer, null, renderTarget);
                    }
                    pipeline.Bind(VkEngine.commandBuffer, renderStageToBindTo, autoUpdateUniforms);
                    Interlocked.Increment(ref VkEngine.begunPipelines);
                    break;
                default:
                    throw new NotImplementedException();
            }
            begun = true;
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
        internal void End()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.currentRenderPass.End(VkEngine.commandBuffer);
                    VkEngine.currentRenderPass = null;
                    
                    VkEngine.unifiedDynamicBuffer.ClearDynamicOffsets();
                    Interlocked.Decrement(ref VkEngine.begunPipelines);
                    break;
                default:
                    throw new NotImplementedException();
            }
            begun = false;
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
