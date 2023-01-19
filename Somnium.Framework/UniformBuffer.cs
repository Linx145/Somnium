using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class UniformBuffer : IDisposable
    {
        private readonly Application application;
        /// <summary>
        /// Whether the buffer is used to store a single object type, or an entire shader's worth of uniforms
        /// </summary>
        public readonly bool isUnified;

        public ulong handle;
        public ulong uniformBufferObjectSize;

        public IntPtr bindingPoint;

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public UniformBuffer(Application application, ulong uniformBufferObjectSize, bool isUnified)
        {
            this.application = application;
            this.uniformBufferObjectSize = uniformBufferObjectSize;
            this.isUnified = isUnified;

            Construct();
        }
        public void SetData<T>(T data, ulong offset) where T : unmanaged
        {
#if DEBUG
            if (!isUnified)
            {
                unsafe
                {
                    int sizeofT = sizeof(T);
                    if ((ulong)sizeofT != uniformBufferObjectSize)
                    {
                        throw new AssetCreationException(typeof(T).Name + "with size " + sizeofT + " is not of the same size as the member within this uniform buffer (" + uniformBufferObjectSize.ToString() + ")!");
                    }
                }
            }
#endif
            unsafe
            {
                *(T*)((byte*)bindingPoint + offset) = data;
                //new Span<T>((void*)bindingPoint, 1)[0] = data;
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
