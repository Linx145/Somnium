using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;

namespace Somnium.Framework
{
    public class Graphics
    {
        public readonly Application application;
        ulong[] noOffset = new ulong[1] { 0 };

        public Graphics(Application application)
        {
            this.application = application;
        }

        public void SetVertexBuffer(VertexBuffer buffer)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        Silk.NET.Vulkan.Buffer vkBuffer = new Silk.NET.Vulkan.Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), 0, &vkBuffer, noOffset.AsSpan());
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
                        Silk.NET.Vulkan.Buffer vkBuffer = new Silk.NET.Vulkan.Buffer(buffer.handle);
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
        public void SetRenderTarget(RenderTarget2D renderTarget)
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        if (!VkEngine.renderPass.begun)
                        {
                            RenderPassBeginInfo beginInfo = new RenderPassBeginInfo();
                            beginInfo.SType = StructureType.RenderPassBeginInfo;
                            beginInfo.RenderPass = VkEngine.renderPass;
                            beginInfo.Framebuffer = new Framebuffer(renderTarget.framebufferHandle);
                            beginInfo.RenderArea = new Rect2D(new Offset2D(0, 0), new Extent2D(renderTarget.width, renderTarget.height));
                            beginInfo.ClearValueCount = 0;
                            //beginInfo.PClearValues = &clearValue;
                        }
                        else throw new InvalidOperationException("Cannot set RenderTarget during render pass");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void DrawPrimitives(uint vertexCount, uint instanceCount)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        VkEngine.vk.CmdDraw(new CommandBuffer(VkEngine.commandBuffer.handle), vertexCount, instanceCount, 0, 0);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void DrawIndexedPrimitives(uint indexCount, uint instanceCount)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        VkEngine.vk.CmdDrawIndexed(new CommandBuffer(VkEngine.commandBuffer.handle), indexCount, instanceCount, 0, 0, 0);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}