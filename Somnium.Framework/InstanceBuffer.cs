using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class InstanceBuffer
    {
        public bool constructed { get; private set; }
        public bool isDisposed { get; private set; }
        public ulong handle;
        private readonly Application application;
        public readonly uint instanceDataSize;
        public readonly uint instanceCount;

        public VertexDeclaration instanceDataDeclaration;

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public InstanceBuffer(Application application, uint instanceDataSize, uint instanceCount)
        {
            this.application = application;
            this.instanceDataSize = instanceDataSize;
            this.instanceCount = instanceCount;
            instanceDataDeclaration = VertexDeclaration.NewVertexDeclaration(application.runningBackend, instanceDataSize, VertexElementInputRate.Instance);
        }
        public static InstanceBuffer New<T>(Application application, uint instanceCount) where T : unmanaged
        {
            unsafe
            {
                return new InstanceBuffer(application, (uint)sizeof(T), instanceCount);
            }
        }
        public void Construct()
        {
            if (constructed) throw new InvalidOperationException("Buffer already constructed!");
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer(instanceDataSize * instanceCount, BufferUsageFlags.VertexBufferBit);
                        memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            constructed = true;
        }
        public void SetData<T>(T[] instanceData) where T : unmanaged
        {
            SetData(instanceData, 0, instanceData.Length);
        }
        public void SetData<T>(T[] instanceData, int offset, int Length) where T : unmanaged
        {
            if (!constructed) throw new InvalidOperationException("Attempted to set data to an instance data buffer that has not been constructed yet!");
            if (Length > instanceData.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data outside of this vertex buffer!");
            }

            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        T* data = memoryRegion.Bind<T>();
                        instanceData.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                        memoryRegion.Unbind();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, new Buffer(handle), null);
                            memoryRegion.Free();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
                isDisposed = true;
            }
        }
    }
}