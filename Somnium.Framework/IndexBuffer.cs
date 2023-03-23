using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class IndexBuffer : IDisposable
    {
        private readonly Application application;
        private readonly bool isDynamic;

        public ulong handle;
        public byte indexSize { get; private set; }
        public int indexCount { get; private set; }

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public IndexBuffer(Application application, IndexSize indexSize, int indexCount, bool isDynamic)
        {
            this.application = application;
            this.isDynamic = isDynamic;
            switch (indexSize)
            {
                case IndexSize.Uint16:
                    this.indexSize = 2;
                    break;
                case IndexSize.Uint32:
                    this.indexSize = 4;
                    break;
                default:
                    this.indexSize = 2;
                    break;
            }
            this.indexCount = indexCount;

            Construct();
        }

        public void SetData<T>(T[] indices, int offset, int Length) where T : unmanaged
        {
            if (offset + Length > indices.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data outside of this index buffer!");
            }
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        if (!isDynamic)
                        {
                            T* data;
                            var stagingBuffer = VkEngine.CreateResourceBuffer((ulong)(indexSize * Length), BufferUsageFlags.TransferSrcBit);
                            var stagingMemoryRegion = VkMemory.malloc("Index Buffer", stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                            data = stagingMemoryRegion.Bind<T>();
                            indices.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                            stagingMemoryRegion.Unbind();

                            //Since there is no distinction between vertex and index buffers in Vulkan
                            //up until the point where we utilise them, we can share the same copy code
                            VertexBuffer.CopyData(application, isDynamic, stagingBuffer.Handle, handle, (ulong)(indexCount * indexSize));

                            VkEngine.DestroyResourceBuffer(stagingBuffer);
                            //VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, stagingBuffer, null);
                            stagingMemoryRegion.Free();
                        }
                        else
                        {
                            T* data = memoryRegion.Bind<T>();
                            indices.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (!isDynamic)
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(indexCount * indexSize), BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit);
                        memoryRegion = VkMemory.malloc("Index Buffer", buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
                    }
                    else
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(indexCount * indexSize), BufferUsageFlags.IndexBufferBit);
                        memoryRegion = VkMemory.malloc("Index Buffer", buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
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
                        VkEngine.DestroyResourceBuffer(buffer);
                        //VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, buffer, null);
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
