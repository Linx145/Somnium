﻿#if VULKAN
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
#endif
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class VertexBuffer : IDisposable
    {
        private readonly Application application;
        private readonly bool isDynamic;

        public ulong handle;
        public int vertexCount { get; private set; }
        public VertexDeclaration vertexDeclaration { get; private set; }

        #region Vulkan
#if VULKAN
        AllocatedMemoryRegion memoryRegion;
#endif
#endregion

        public VertexBuffer(Application application, VertexDeclaration vertexType, int vertexCount, bool isDynamic)
        {
            this.application = application;
            this.vertexCount = vertexCount;
            this.vertexDeclaration = vertexType;
            this.isDynamic = isDynamic;

            Construct();
        }
        public void SetData<T>(T[] vertices, int offset, int Length) where T : unmanaged
        {
            if (offset + Length > vertices.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data outside of this vertex buffer!");
            }
            unsafe
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        if (!isDynamic)
                        {
                            T* data;
                            var stagingBuffer = VkEngine.CreateResourceBuffer((ulong)(vertexDeclaration.size * Length), BufferUsageFlags.TransferSrcBit);
                            var stagingMemoryRegion = VkMemory.malloc("Vertex Buffer", stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                            data = stagingMemoryRegion.Bind<T>();
                            vertices.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                            stagingMemoryRegion.Unbind();

                            CopyData(application, isDynamic, stagingBuffer.Handle, handle, (ulong)(vertexCount * vertexDeclaration.size));

                            VkEngine.DestroyResourceBuffer(stagingBuffer);
                            stagingMemoryRegion.Free();
                        }
                        else
                        {
                            T* data = memoryRegion.Bind<T>();
                            vertices.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public static void CopyData(Application application, bool isDynamic, ulong fromHandle, ulong toHandle, ulong copySize)
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    if (!isDynamic)
                    {
                        VkEngine.StaticCopyResourceBuffer(application, new Buffer(fromHandle), new Buffer(toHandle), copySize);
                    }
                    else throw new NotImplementedException();
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    if (!isDynamic)
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(vertexCount * vertexDeclaration.size), BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit);
                        memoryRegion = VkMemory.malloc("Vertex Buffer", buffer, MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
                    }
                    else
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(vertexCount * vertexDeclaration.size), BufferUsageFlags.VertexBufferBit);
                        memoryRegion = VkMemory.malloc("Vertex Buffer", buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        Buffer buffer = new Buffer(handle);
                        VkEngine.DestroyResourceBuffer(buffer);
                        if (memoryRegion.IsValid)
                        {
                            memoryRegion.Free();
                        }
                        //VkEngine.vk.FreeMemory(VkEngine.vkDevice, deviceMemory, null);
                    }
                    break;
#endif
                default: 
                    throw new NotImplementedException();
            }
        }
    }
}
