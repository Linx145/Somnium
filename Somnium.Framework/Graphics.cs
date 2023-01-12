using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;

namespace Somnium.Framework
{
    public static class Graphics
    {
        public static Application application;
        public static void SetVertexBuffer(VertexBuffer buffer)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        Silk.NET.Vulkan.Buffer vkBuffer = new Silk.NET.Vulkan.Buffer(buffer.handle);
                        ulong* offsets = stackalloc ulong[1];
                        *offsets = 0;
                        VkEngine.vk.CmdBindVertexBuffers(VkEngine.commandBuffer, 0, &vkBuffer, new ReadOnlySpan<ulong>(offsets, 1));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public static void DrawPrimitives(uint vertexCount, uint instanceCount)
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
        public static void DrawIndexedPrimitives()
        {
            
        }
    }
}