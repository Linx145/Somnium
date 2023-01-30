using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
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

        public void Clear(Color clearColor)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        if (VkEngine.currentRenderPass == null)
                        {
                            throw new InvalidOperationException("Cannot perform clear buffer operation in Vulkan when no VkRenderPass is active!");
                        }
                        //vkCmdClearAttachments is not affected by the bound pipeline state.
                        //it however must be issued within a valid render pass
                        int clearCount = 2;
                        ClearAttachment* clearAttachments = stackalloc ClearAttachment[clearCount];
                        clearAttachments[0] = new ClearAttachment(ImageAspectFlags.ColorBit, 0, new ClearValue(new ClearColorValue(clearColor.R / 255f, clearColor.G / 255f, clearColor.B / 255f, clearColor.A / 255f)));
                        clearAttachments[1] = new ClearAttachment(ImageAspectFlags.DepthBit, 0, new ClearValue(null, new ClearDepthStencilValue(1f, 0)));

                        ClearRect* clearAreas = stackalloc ClearRect[clearCount];
                        Extent2D extents;
                        if (currentRenderbuffer == null)
                        {
                            extents = VkEngine.swapChain.imageExtents;
                        }
                        else
                        {
                            extents = new Extent2D(currentRenderbuffer.width, currentRenderbuffer.height);
                        }
                        for (int i = 0; i < clearCount; i++)
                        {
                            clearAreas[i] = new ClearRect(new Rect2D(default(Offset2D), extents), 0, 1);
                        }

                        VkEngine.vk.CmdClearAttachments(VkEngine.commandBuffer, (uint)clearCount, clearAttachments, (uint)clearCount, clearAreas);
                    }
                    break;
                default:
                    break;
            }
        }

        public void SetRenderbuffer(RenderBuffer renderBuffer)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        if (VkEngine.currentRenderPass != null)
                        {
                            VkEngine.currentRenderPass.End(VkEngine.commandBuffer);
                            VkEngine.currentRenderPass = null;
                        }
                        if (renderBuffer == null)
                        {
                            VkEngine.SetRenderPass(VkEngine.renderPass, null);
                        }
                        else
                        {
                            VkEngine.SetRenderPass(VkEngine.framebufferRenderPass, renderBuffer);
                        }
                        currentRenderbuffer = renderBuffer;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Sets and begins a render pipeline state containing the shader to use.
        /// </summary>
        /// <param name="pipelineState"></param>
        public void SetPipeline(PipelineState pipelineState)
        {
            pipelineState.Begin();
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
            currentRenderbuffer = null;
        }
        /// <summary>
        /// Syncs the local state of the uniform buffers with the shader
        /// </summary>
        public void ForceUpdateUniforms()
        {
            currentPipeline.ForceUpdateUniforms(RenderStage.Graphics);
        }
        public void SetInstanceBuffer(InstanceBuffer buffer, uint bindingPoint)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handles[application.Window.frameNumber]);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), bindingPoint, 1, &vkBuffer, noOffset.AsSpan());
                        }
                        break;
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
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), bindingPoint, 1, &vkBuffer, noOffset.AsSpan());
                        }
                        break;
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
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindIndexBuffer(new CommandBuffer(VkEngine.commandBuffer.handle), vkBuffer, 0, IndexType.Uint16);
                        }
                        break;
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
                    case Backends.Vulkan:
                        ResetPipelineShaders();
                        VkEngine.vk.CmdDraw(new CommandBuffer(VkEngine.commandBuffer.handle), vertexCount, instanceCount, firstVertex, firstInstance);
                        break;
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
                    case Backends.Vulkan:
                        ResetPipelineShaders();
                        VkEngine.vk.CmdDrawIndexed(new CommandBuffer(VkEngine.commandBuffer.handle), indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void ResetPipelineShaders()
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
            if (updateUniforms) ForceUpdateUniforms();

            //need to update using the old descriptorForThisDrawCall state
            for (int i = 0; i < currentPipeline.shaders.Length; i++)
            {
                currentPipeline.shaders[i].descriptorForThisDrawCall++;
            }
        }
    }
}