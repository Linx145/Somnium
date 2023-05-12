#if VULKAN
using Somnium.Framework.Vulkan;
#endif
using System;
using System.Collections.Generic;
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
        public readonly bool depthWrite;
        public readonly bool depthTest;

        //public GenerationalIndex handle;
        public Dictionary<uint, GenerationalIndex> handles;

        public PipelineState(
            Application application,
            CullMode cullMode,
            PrimitiveType primitiveType,
            BlendState blendState,
            Shader shader,
            bool depthTest,
            bool depthWrite,
            params VertexDeclaration[] vertices)
        {
            this.application = application;
            this.cullMode = cullMode;
            this.primitiveType = primitiveType;
            this.blendState = blendState;
            this.depthTest = depthTest;
            this.depthWrite = depthWrite;
            shaders = new Shader[] { shader };
            this.vertices = vertices;

            handles = new Dictionary<uint, GenerationalIndex>();
            //Construct();
        }
        /// <summary>
        /// Public call available in Graphics.SetPipeline
        /// </summary>
        /// <param name="clearColor"></param>
        /// <param name="renderTarget"></param>
        /// <param name="renderStageToBindTo"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void Begin(RenderStage renderStageToBindTo = RenderStage.Graphics, Rectangle scissorRectangle = default)
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:

                    var renderPass = VkEngine.GetCurrentRenderPass(application);
                    VkEngine.SetRenderPass(renderPass, application.Graphics.currentRenderbuffer);
                    VkGraphicsPipeline pipeline;
                    if (!handles.TryGetValue(renderPass.hash, out var handle))
                    {
                        pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, renderPass, shaders[0], depthTest, depthWrite, vertices);
                        handle = VkEngine.AddPipeline(pipeline);
                        handles.Add(renderPass.hash, handle);
                    }
                    else pipeline = VkEngine.GetPipeline(handle);

                    pipeline.Bind(application.Graphics.currentRenderbuffer, VkEngine.commandBuffer, renderStageToBindTo, scissorRectangle);
                    Interlocked.Increment(ref VkEngine.begunPipelines);
                    break;
#endif
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
#if VULKAN
                case Backends.Vulkan:
                    var renderPass = VkEngine.GetCurrentRenderPass(application);
                    VkGraphicsPipeline pipeline;
                    if (!handles.TryGetValue(renderPass.hash, out var handle))
                    {
                        pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, renderPass, shaders[0], depthTest, depthWrite, vertices);
                        handle = VkEngine.AddPipeline(pipeline);
                        handles.Add(renderPass.hash, handle);
                    }
                    else pipeline = VkEngine.GetPipeline(handle);
                    pipeline.PushUniformUpdates(VkEngine.commandBuffer, bindType);
                    break;
#endif
                default:
                    break;
            }
        }
        internal void End()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    VkEngine.activeRenderPass.End(VkEngine.commandBuffer);
                    VkEngine.activeRenderPass = null;
                    
                    VkEngine.unifiedDynamicBuffer.ClearDynamicOffsets();
                    Interlocked.Decrement(ref VkEngine.begunPipelines);
                    break;
#endif
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
#if VULKAN
                case Backends.Vulkan:
                    foreach (var handle in handles.Values)
                    {
                        VkEngine.DestroyPipeline(handle);
                    }
                    //VkEngine.DestroyPipeline(handle);
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
