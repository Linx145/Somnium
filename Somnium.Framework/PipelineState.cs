#if VULKAN
using Somnium.Framework.Vulkan;
#endif
#if WGPU
using Somnium.Framework.WGPU;
using Silk.NET.WebGPU;
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

#if VULKAN
        public Dictionary<VkRenderPassHash, GenerationalIndex> handles;
#endif
#if WGPU
        public Dictionary<>
#endif

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

            #if VULKAN
            handles = new Dictionary<VkRenderPassHash, GenerationalIndex>();
            #endif
            //Construct();
        }
        /// <summary>
        /// Public call available in Graphics.SetPipeline
        /// </summary>
        /// <param name="clearColor"></param>
        /// <param name="renderTarget"></param>
        /// <param name="renderStageToBindTo"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void Begin(RenderStage renderStageToBindTo = RenderStage.Graphics, Rectangle scissorRectangle = default, Color? clearColor = null)
        {
            switch (application.runningBackend)
            {
#if WGPU
                case Backends.WebGPU:
                    unsafe
                    {
                        //we dont need to get a prebuilt renderpass from WGPUEngine as WGPU
                        //automatically hashes the renderpasses for us

                        const double OneOver255 = 1.0 / 255.0;

                        TextureView* imageView = null;
                        if (application.Graphics.currentRenderbuffer == null)
                        {
                            imageView = WGPUEngine.backbufferTextureView;
                        }
                        else imageView = (TextureView*)application.Graphics.currentRenderbuffer.backendTexture.imageViewHandle;

                        var colorAttachment = new RenderPassColorAttachment()
                        {
                            LoadOp = clearColor != null ? LoadOp.Clear : LoadOp.Load,
                            StoreOp = StoreOp.Store,
                            ClearValue = clearColor != null ? new Silk.NET.WebGPU.Color(clearColor.Value.R * OneOver255, clearColor.Value.G * OneOver255, clearColor.Value.B * OneOver255, clearColor.Value.A * OneOver255) : new Silk.NET.WebGPU.Color(),
                            View = imageView
                        };
                        var depthAttachment = new RenderPassDepthStencilAttachment()
                        {
                            DepthLoadOp = clearColor != null ? LoadOp.Clear : LoadOp.Load,
                            DepthStoreOp = depthWrite ? StoreOp.Store : StoreOp.Discard,
                            StencilLoadOp = clearColor != null ? LoadOp.Clear : LoadOp.Load,
                            StencilStoreOp = depthWrite ? StoreOp.Store : StoreOp.Discard,
                            DepthClearValue = 1f,
                            StencilClearValue = 0,
                            DepthReadOnly = !depthWrite,
                            StencilReadOnly = !depthWrite,
                            View = imageView
                        };

                        RenderPassDescriptor descriptor = new RenderPassDescriptor()
                        {
                            ColorAttachmentCount = 1,
                            ColorAttachments = &colorAttachment,
                            DepthStencilAttachment = depthTest ? &depthAttachment : null
                        };
                        var renderPass = WGPUEngine.wgpu.CommandEncoderBeginRenderPass(WGPUEngine.commandEncoder, &descriptor);

                        WGPUGraphicsPipeline pipeline;
                        if (!)
                    }
                    break;
#endif
#if VULKAN
                case Backends.Vulkan:
                {
                    var renderPass = VkEngine.GetCurrentRenderPass(application, clearColor != null);
                    VkEngine.SetRenderPass(renderPass, application.Graphics.currentRenderbuffer, clearColor);
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
                    }
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
                {
                    var renderPass = VkEngine.GetCurrentRenderPass(application, false);
                    VkGraphicsPipeline pipeline;
                    if (!handles.TryGetValue(renderPass.hash, out var handle))
                    {
                        pipeline = new VkGraphicsPipeline(application, cullMode, blendState, primitiveType, renderPass, shaders[0], depthTest, depthWrite, vertices);
                        handle = VkEngine.AddPipeline(pipeline);
                        handles.Add(renderPass.hash, handle);
                    }
                    else pipeline = VkEngine.GetPipeline(handle);
                    pipeline.PushUniformUpdates(VkEngine.commandBuffer, bindType);
                    }
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
