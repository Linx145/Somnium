using Silk.NET.Vulkan;
#if VULKAN
using Somnium.Framework.Vulkan;
#endif
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class Graphics
    {
        public PipelineState currentPipeline { get; private set; }
        public readonly Application application;
        ulong[] noOffset = new ulong[1] { 0 };

        public RenderBuffer currentRenderbuffer
        {
            get; internal set;
        }

        public Graphics(Application application)
        {
            this.application = application;
        }

        /// <summary>
        /// Clears the target renderbuffer, or if none is specified, the backbuffer.
        /// </summary>
        public void ClearBuffer(Color clearColor, RenderBuffer targetBuffer = null)
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        if (targetBuffer == null)
                        {
                            var renderpass = VkRenderPass.GetOrCreate(VkEngine.swapChain.imageFormat, ImageLayout.PresentSrcKhr, VkEngine.swapChain.depthFormat, true);
                            VkEngine.SetRenderPass(renderpass, null, clearColor);
                            VkEngine.activeRenderPass = renderpass;
                        }
                        else
                        {
                            var renderpass = VkRenderPass.GetOrCreate(
                                Converters.ImageFormatToVkFormat[(int)targetBuffer.backendTexture.imageFormat],
                                ImageLayout.ColorAttachmentOptimal,
                                targetBuffer.depthBuffer == null ? DepthFormat.None : targetBuffer.depthBuffer.depthFormat,
                                true);
                            VkEngine.SetRenderPass(renderpass, targetBuffer, clearColor);
                            VkEngine.activeRenderPass = renderpass;
                            //VkEngine.SetRenderPass(VkEngine.framebufferRenderPass, targetBuffer);
                            //VkEngine.currentRenderPass = VkEngine.framebufferRenderPass;
                        }

                        //Clear(clearColor, targetBuffer == null ? default : new Point((int)targetBuffer.width, (int)targetBuffer.height));

                        VkEngine.activeRenderPass.End(VkEngine.commandBuffer);
                        VkEngine.activeRenderPass = null;
                    }
                    break;
#endif
                default:
                    throw new InvalidOperationException();
            }
            if (targetBuffer != null)
            {
                targetBuffer.hasBeenUsedBefore = true;
            }
        }

        /*public void Clear(Color clearColor, Point clearDimensions = default)
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        if (VkEngine.activeRenderPass == null)
                        {
                            throw new InvalidOperationException("Cannot perform clear buffer operation in Vulkan when no VkRenderPass is active!");
                        }
                        //vkCmdClearAttachments is not affected by the bound pipeline state.
                        //it however must be issued within a valid render pass
                        int clearCount = 1;
                        ClearAttachment* clearAttachments = stackalloc ClearAttachment[2];
                        clearAttachments[0] = new ClearAttachment(ImageAspectFlags.ColorBit, 0, new ClearValue(new ClearColorValue(clearColor.R / 255f, clearColor.G / 255f, clearColor.B / 255f, clearColor.A / 255f)));
                        if (VkEngine.activeRenderPass.hasDepthStencil)
                        {
                            clearCount++;
                            clearAttachments[1] = new ClearAttachment(ImageAspectFlags.DepthBit, 0, new ClearValue(null, new ClearDepthStencilValue(1f, 0)));
                        }

                        ClearRect* clearAreas = stackalloc ClearRect[clearCount];
                        Extent2D extents;
                        if (clearDimensions == default)
                        {
                            if (currentRenderbuffer == null)
                            {
                                extents = VkEngine.swapChain.imageExtents;
                            }
                            else
                            {
                                extents = new Extent2D(currentRenderbuffer.width, currentRenderbuffer.height);
                            }
                        }
                        else
                        {
                            extents = new Extent2D((uint)clearDimensions.X, (uint)clearDimensions.Y);
                        }
                        for (int i = 0; i < clearCount; i++)
                        {
                            clearAreas[i] = new ClearRect(new Rect2D(default(Offset2D), extents), 0, 1);
                        }

                        VkEngine.vk.CmdClearAttachments(VkEngine.commandBuffer, (uint)clearCount, clearAttachments, (uint)clearCount, clearAreas);
                    }
                    break;
#endif
                default:
                    break;
            }
        }*/
        public void SetClipArea(Rectangle rect)
        {
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("Can only call SetClipArea from within SetPipeline() and EndPipeline()! Otherwise, simply specify the clip area within SetPipeline!");
            }
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    if (rect == default)
                    {
                        Viewport viewport;
                        if (currentRenderbuffer != null)
                        {
                            viewport = new Viewport(0, 0, currentRenderbuffer.width, currentRenderbuffer.height, 0f, 1f);
                        }
                        else viewport = new Viewport(0, 0, application.Window.Size.X, application.Window.Size.Y, 0, 1);

                        VkEngine.vk.CmdSetScissor(new CommandBuffer(VkEngine.commandBuffer.handle), 0, 1, new Rect2D(new Offset2D((int)viewport.X, (int)viewport.Y), new Extent2D((uint)viewport.Width, (uint)viewport.Height)));
                    }
                    else
                    {
                        VkEngine.vk.CmdSetScissor(new CommandBuffer(VkEngine.commandBuffer.handle), 0, 1, new Rect2D(new Offset2D(rect.X, rect.Y), new Extent2D((uint)rect.Width, (uint)rect.Height)));
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void AwaitImageFinishModifying(Texture2D image)
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    VkEngine.AwaitImageFinishModifying(image);
                    break;
#endif
                default:
                    break;
            }
        }
        public void AwaitGraphicsIdle()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    VkEngine.vk.QueueWaitIdle(VkEngine.CurrentGPU.AllPurposeQueue);
                    break;
#endif
                default:
                    break;
            }
        }
        public void SetRenderbuffer(RenderBuffer renderBuffer)
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        if (VkEngine.activeRenderPass != null)
                        {
                            throw new InvalidOperationException("Cannot set target renderbuffer when renderpass is active!");
                        }
                        currentRenderbuffer = renderBuffer;
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
            if (renderBuffer != null) renderBuffer.hasBeenUsedBefore = true;
        }
        /// <summary>
        /// Sets and begins a render pipeline state containing the shader to use.
        /// </summary>
        /// <param name="pipelineState"></param>
        /// <param name="autoUpdateUniforms">If true, updates the shaders uniform state as well</param>
        public void SetPipeline(PipelineState pipelineState, Rectangle areaToDrawTo = default, Color? clearColor = null)
        {
            pipelineState.Begin(RenderStage.Graphics, areaToDrawTo, clearColor);
            currentPipeline = pipelineState;
        }
        public void EndPipeline()
        {
            if (currentPipeline == null || !currentPipeline.begun)
            {
                throw new InvalidOperationException("Attempted to call EndPipeline() while the active pipeline is either missing or has not begun/already ended!");
            }
            currentPipeline.End();
            currentPipeline = null;
        }
        /// <summary>
        /// Syncs the local state of the uniform buffers with the shader
        /// </summary>
        public void SendUpdatedUniforms()
        {
            bool updateUniforms = false;
            for (int i = 0; i < currentPipeline.shaders.Length; i++)
            {
                if (currentPipeline.shaders[i].uniformHasBeenSet)
                {
                    updateUniforms = true;
                }
                currentPipeline.shaders[i].uniformHasBeenSet = false;
            }
            //at least one uniform has been updated, so we need to update our descriptors
            if (updateUniforms) currentPipeline.ForceUpdateUniforms(RenderStage.Graphics);
        }
        public void SetInstanceBuffer(InstanceBuffer buffer, uint bindingPoint)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handles[application.Window.frameNumber]);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), bindingPoint, 1, &vkBuffer, noOffset.AsSpan());
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void SetVertexBuffer(VertexBuffer buffer, uint bindingPoint = 0)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), bindingPoint, 1, &vkBuffer, noOffset.AsSpan());
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void SetIndexBuffer(IndexBuffer buffer)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindIndexBuffer(new CommandBuffer(VkEngine.commandBuffer.handle), vkBuffer, 0, IndexType.Uint16);
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void DrawPrimitives(uint vertexCount, uint instanceCount, uint firstVertex = 0, uint firstInstance = 0)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        ResetPipelineShaders();
                        VkEngine.vk.CmdDraw(new CommandBuffer(VkEngine.commandBuffer.handle), vertexCount, instanceCount, firstVertex, firstInstance);
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void DrawIndexedPrimitives(uint indexCount, uint instanceCount, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        ResetPipelineShaders();
                        VkEngine.vk.CmdDrawIndexed(new CommandBuffer(VkEngine.commandBuffer.handle), indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void ResetPipelineShaders()
        {
            SendUpdatedUniforms();

            //need to update using the old descriptorForThisDrawCall state
            for (int i = 0; i < currentPipeline.shaders.Length; i++)
            {
                currentPipeline.shaders[i].descriptorForThisDrawCall++;
            }
        }
    }
}