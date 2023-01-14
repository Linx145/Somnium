using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework
{
    /// <summary>
    /// Represents an interchangeable state of the graphics pipeline, including what shaders is currently to be used, etc
    /// </summary>
    public class PipelineState : IDisposable
    {
        readonly Application application;
        public readonly Somnium.Framework.Viewport viewport;
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
            Shader shader)
        {
            this.application = application;
            this.viewport = viewport;
            this.cullMode = cullMode;
            this.primitiveType = primitiveType;
            this.blendState = blendState;
            shaders = new Shader[] { shader };

            Construct();
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (shaders.Length != 1) throw new NotImplementedException();

                    VkGraphicsPipeline pipeline = new VkGraphicsPipeline(viewport.ToVulkanViewport(), cullMode, blendState, primitiveType, VkEngine.renderPass, shaders[0]);
                    handle = VkEngine.AddPipeline(pipeline);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Bind(RenderStage renderStageToBindTo = RenderStage.Graphics)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    var pipeline = VkEngine.GetPipeline(handle);
                    pipeline.Bind(VkEngine.commandBuffer, renderStageToBindTo);
                    break;
                default:
                    throw new NotImplementedException();
            }
            //vk.CmdBindPipeline(commandBuffer.handle, PipelineBindPoint.Graphics, handle);
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
