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
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Begin(Color clearColor, RenderTarget2D? renderTarget = null, RenderStage renderStageToBindTo = RenderStage.Graphics)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.renderPass.Begin(VkEngine.commandBuffer, VkEngine.swapChain, clearColor, renderTarget);

                    var pipeline = VkEngine.GetPipeline(handle);
                    pipeline.Bind(VkEngine.commandBuffer, renderStageToBindTo);
                    Interlocked.Increment(ref VkEngine.begunPipelines);
                    break;
                default:
                    throw new NotImplementedException();
            }
            //vk.CmdBindPipeline(commandBuffer.handle, PipelineBindPoint.Graphics, handle);
        }
        public void End()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.renderPass.End(VkEngine.commandBuffer);
                    VkEngine.commandBuffer.End();
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
