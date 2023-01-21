using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Threading.Tasks;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class UniformBuffer : IDisposable
    {
        private readonly Application application;

        public ulong handle;
        public ulong size;

        public IntPtr bindingPoint;

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public UniformBuffer(Application application, ulong size)
        {
            this.application = application;
            this.size = size;

            if (size == 0)
            {
                throw new InvalidOperationException();
            }

            Construct();
        }
        public void SetData<T>(T data, ulong offset) where T : unmanaged
        {
            unsafe
            {
#if DEBUG
                int sizeofT = sizeof(T);
                if ((ulong)sizeofT != size)
                {
                    throw new AssetCreationException(typeof(T).Name + "with size " + sizeofT + " is not of the same size as the member within this uniform buffer (" + size.ToString() + ")!");
                }
#endif
                *(T*)((byte*)bindingPoint + offset) = data;
                //new Span<T>((void*)bindingPoint, 1)[0] = data;
            }
        }
        public void SetData<T>(T[] data, ulong offset) where T : unmanaged
        {
            unsafe
            {
                //int sizeofT = sizeof(T);
                T* ptr = (T*)((byte*)bindingPoint + offset);
                /*Parallel.For(0, data.Length, (int i) =>
                {
                    *(ptr + i) = data[i];
                });*/
                data.CopyTo(new Span<T>(ptr, data.Length));
            }
        }
        public void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        Buffer buffer = VkEngine.CreateResourceBuffer(size, BufferUsageFlags.UniformBufferBit);
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
