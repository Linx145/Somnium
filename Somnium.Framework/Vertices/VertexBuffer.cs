using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
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
        AllocatedMemoryRegion memoryRegion;
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
            if (Length > vertices.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data outside of this vertex buffer!");
            }
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        if (!isDynamic)
                        {
                            T* data;
                            var stagingBuffer = VkEngine.CreateResourceBuffer((ulong)(vertexDeclaration.size * Length), BufferUsageFlags.TransferSrcBit);
                            var stagingMemoryRegion = VkMemory.malloc(stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                            data = stagingMemoryRegion.Bind<T>();
                            vertices.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                            stagingMemoryRegion.Unbind();

                            CopyData(Backends.Vulkan, isDynamic, stagingBuffer.Handle, handle, (ulong)(vertexCount * vertexDeclaration.size));

                            VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, stagingBuffer, null);
                            stagingMemoryRegion.Free();
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public static void CopyData(Backends runningBackend, bool isDynamic, ulong fromHandle, ulong toHandle, ulong copySize)
        {
            switch (runningBackend)
            {
                case Backends.Vulkan:
                    if (!isDynamic)
                    {
                        VkEngine.StaticCopyResourceBuffer(new Buffer(fromHandle), new Buffer(toHandle), copySize);
                    }
                    else throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (!isDynamic)
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(vertexCount * vertexDeclaration.size), BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit);
                        memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
                    }
                    else
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(vertexCount * vertexDeclaration.size), BufferUsageFlags.VertexBufferBit);
                        memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        Buffer buffer = new Buffer(handle);
                        VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, buffer, null);
                        if (memoryRegion.IsValid)
                        {
                            memoryRegion.Free();
                        }
                        //VkEngine.vk.FreeMemory(VkEngine.vkDevice, deviceMemory, null);
                    }
                    break; 
                default: 
                    throw new NotImplementedException();
            }
        }
    }
}
