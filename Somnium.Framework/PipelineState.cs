using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
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
        /*private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (shaders.Length != 1) throw new NotImplementedException();

                    VkGraphicsPipeline pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, VkEngine.renderPass, shaders[0], depthTest, depthWrite, vertices);
                    handle = VkEngine.AddPipeline(pipeline);

                    //pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, VkEngine.framebufferRenderPass, shaders[0], depthTest, depthWrite, vertices);
                    //VkEngine.AddRenderbufferPipeline(pipeline);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }*/
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
                    /*if (application.Graphics.currentRenderbuffer == null)
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
                    }*/
                    pipeline.Bind(application.Graphics.currentRenderbuffer, VkEngine.commandBuffer, renderStageToBindTo, scissorRectangle);
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
                default:
                    break;
            }
        }
        internal void End()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    VkEngine.activeRenderPass.End(VkEngine.commandBuffer);
                    VkEngine.activeRenderPass = null;
                    
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
                    foreach (var handle in handles.Values)
                    {
                        VkEngine.DestroyPipeline(handle);
                    }
                    //VkEngine.DestroyPipeline(handle);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
