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
                            VkEngine.vk.CmdBindVertexBuffers(VkEngine.commandBuffer, 0, &vkBuffer, noOffset.AsSpan());
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
                            VkEngine.vk.CmdBindIndexBuffer(VkEngine.commandBuffer, vkBuffer, 0, IndexType.Uint16);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void DrawPrimitives(uint vertexCount, uint instanceCount)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        VkEngine.vk.CmdDraw(VkEngine.commandBuffer, vertexCount, instanceCount, 0, 0);
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
                        VkEngine.vk.CmdDrawIndexed(VkEngine.commandBuffer, indexCount, instanceCount, 0, 0, 0);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}