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
#if DEBUG
            unsafe
            {
                int sizeofT = sizeof(T);
                if (sizeofT != indexSize)
                {
                    throw new AssetCreationException(typeof(T).Name + "with size " + sizeofT + " is not of the same size as the member within this index buffer(" + indexSize.ToString() + ")!");
                }
            }
#endif
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
                            var stagingMemoryRegion = VkMemory.malloc(stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                            stagingMemoryRegion.Bind((void**)&data);
                            indices.AsSpan().CopyTo(new Span<T>(data + offset, Length));
                            stagingMemoryRegion.Unbind();

                            //Since there is no distinction between vertex and index buffers in Vulkan
                            //up until the point where we utilise them, we can share the same copy code
                            VertexBuffer.CopyData(Backends.Vulkan, isDynamic, stagingBuffer.Handle, handle, (ulong)(indexCount * indexSize));

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
        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (!isDynamic)
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer((ulong)(indexCount * indexSize), BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit);
                        memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
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
