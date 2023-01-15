using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class UniformBuffer : IDisposable
    {
        private readonly Application application;

        public ulong handle;
        public uint uniformBufferObjectSize;

        public IntPtr bindingPoint;

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public UniformBuffer(Application application, uint uniformBufferObjectSize)
        {
            this.application = application;
            this.uniformBufferObjectSize = uniformBufferObjectSize;

            Construct();
        }
        public void SetData<T>(T data) where T : unmanaged
        {
#if DEBUG
            unsafe
            {
                int sizeofT = sizeof(T);
                if (sizeofT != uniformBufferObjectSize)
                {
                    throw new AssetCreationException(typeof(T).Name + "with size " + sizeofT + " is not of the same size as the member within this uniform buffer (" + uniformBufferObjectSize.ToString() + ")!");
                }
            }
#endif
            unsafe
            {
                new Span<T>((void*)bindingPoint, 1)[0] = data;
            }
        }
        public void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer(uniformBufferObjectSize, BufferUsageFlags.UniformBufferBit);
                        memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                        handle = buffer.Handle;

                        void* pointer = memoryRegion.Bind();
                        bindingPoint = (IntPtr)pointer;
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
                        memoryRegion.Unbind();
                        VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, new Buffer(handle), null);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
