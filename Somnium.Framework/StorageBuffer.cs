#if VULKAN
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
#endif
using System;

namespace Somnium.Framework
{
    public class StorageBuffer : IDisposable
    {
        private readonly Application application;

        public ulong maxSize;
        public ulong handle;

        public bool accessAsVertexBuffer;

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public StorageBuffer(Application application, ulong maxSize, bool accessAsVertexBuffer)
        {
            this.application = application;
            this.maxSize = maxSize;
            this.accessAsVertexBuffer = accessAsVertexBuffer;

            Construct();
        }

        public unsafe void SetData<T>(T[] elements, int offset, int Length) where T : unmanaged
        {
            if (offset + Length > elements.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data outside of this storage buffer!");
            }
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    T* data = memoryRegion.Bind<T>();
                    elements.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void Construct()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    BufferUsageFlags usageFlags = BufferUsageFlags.TransferDstBit | BufferUsageFlags.StorageBufferBit;
                    if (accessAsVertexBuffer)
                    {
                        usageFlags = usageFlags | BufferUsageFlags.VertexBufferBit;
                    }
                    Buffer buffer = VkEngine.CreateResourceBuffer(maxSize, usageFlags);
                    memoryRegion = VkMemory.malloc("Storage Buffer", buffer, MemoryPropertyFlags.DeviceLocalBit);
                    handle = buffer.Handle;
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
                        //memoryRegion.Unbind();
                        if (memoryRegion.IsValid)
                        {
                            memoryRegion.Free();
                        }
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
    }
}