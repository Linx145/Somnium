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
        public readonly bool isDynamic;
        public ulong dynamicOffset { get; private set; }
        /// <summary>
        /// For use in dynamic buffers to specify the offsets of each uniform. Cleared once per frame
        /// </summary>
        public UnorderedList<uint> dataOffsets;

        public ulong handle;
        public ulong size;

        public IntPtr bindingPoint;

        public bool isDisposed { get; private set; }

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public UniformBuffer(Application application, ulong size, bool isDynamic = false)
        {
            this.application = application;
            this.size = size;
            this.isDynamic = isDynamic;

            if (size == 0)
            {
                throw new InvalidOperationException();
            }

            if (isDynamic)
            {
                dataOffsets = new UnorderedList<uint>();
            }
            Construct();
        }

        public void ClearDynamicOffsets()
        {
            dynamicOffset = 0;
            dataOffsets.Clear();
        }
        public void SetData<T>(T data, ulong offset) where T : unmanaged
        {
            unsafe
            {
#if DEBUG
                if (!isDynamic)
                {
                    int sizeofT = sizeof(T);
                    if ((ulong)sizeofT != size)
                    {
                        throw new AssetCreationException(typeof(T).Name + "with size " + sizeofT + " is not of the same size as the member within this uniform buffer (" + size.ToString() + ")!");
                    }
                }
#endif
                if (application.runningBackend == Backends.Vulkan)
                {
                    if (isDynamic)
                    {
                        offset = dynamicOffset;
                        dataOffsets.Add((uint)offset);

                        int sizeofT = sizeof(T);
                        ulong filledBufferExtents = offset + (ulong)sizeofT;
                        if (filledBufferExtents > size)
                        {
                            ExpandDynamicBuffer(filledBufferExtents);
                        }
                        //advance the offset to the new extents of the buffer
                        dynamicOffset = filledBufferExtents;
                    }
                }
                *(T*)((byte*)bindingPoint + offset) = data;
                //new Span<T>((void*)bindingPoint, 1)[0] = data;
            }
        }
        public void SetData<T>(T[] data, ulong offset) where T : unmanaged
        {
            unsafe
            {
                if (application.runningBackend == Backends.Vulkan)
                {
                    if (isDynamic)
                    {
                        offset = dynamicOffset;
                        dataOffsets.Add((uint)offset);

                        int sizeofT = sizeof(T);
                        ulong filledBufferExtents = offset + (ulong)(sizeofT * data.Length);
                        if (filledBufferExtents > size)
                        {
                            ExpandDynamicBuffer(filledBufferExtents);
                        }
                        dynamicOffset = filledBufferExtents;
                    }
                }
                //int sizeofT = sizeof(T);
                T* ptr = (T*)((byte*)bindingPoint + offset);
                /*Parallel.For(0, data.Length, (int i) =>
                {
                    *(ptr + i) = data[i];
                });*/
                data.CopyTo(new Span<T>(ptr, data.Length));
            }
        }
        public unsafe void ExpandDynamicBuffer(ulong newMaxSize)
        {
            if (!isDynamic) throw new InvalidOperationException("Cannot dynamically resize a non-dynamic uniform buffer!");
            //grow the dynamic uniform buffer by blocks of 256 as it is a universally recognised min uniform buffer alignment size
            newMaxSize = (ulong)(Math.Ceiling(newMaxSize / 256f) * 256f);

            if (newMaxSize <= size)
            {
                return;
            }

            //create new buffer
            //WARNING: TransferSrcBit and TransferDstBit together may prevent some optimisations, idk
            Buffer newBuffer = VkEngine.CreateResourceBuffer(newMaxSize, BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferSrcBit | BufferUsageFlags.TransferDstBit);
            var newMemory = VkMemory.malloc(newBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            
            //copy old buffer data into new buffer
            Buffer oldBuffer = new Buffer(handle);
            VkEngine.StaticCopyResourceBuffer(application, oldBuffer, newBuffer, size);

            //dispose of the old memory region
            if (memoryRegion.isBound) memoryRegion.Unbind();
            memoryRegion.Free();

            //dispose of the old buffer
            VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, new Buffer(handle), null);

            //update variables
            size = newMaxSize;
            handle = newBuffer.Handle;

            memoryRegion = newMemory;

            //and lastly, bind new memory
            void* pointer = memoryRegion.Bind();
            bindingPoint = (IntPtr)pointer;
        }
        public void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        if (!isDynamic)
                        {
                            Buffer buffer = VkEngine.CreateResourceBuffer(size, BufferUsageFlags.UniformBufferBit);
                            memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                            Console.WriteLine("Frame " + application.Window.frameNumber + ": Uniform buffer malloc'd " + memoryRegion.ToString());
                            handle = buffer.Handle;

                            void* pointer = memoryRegion.Bind();
                            bindingPoint = (IntPtr)pointer;
                        }
                        else
                        {
                            //dont need TransferDstBit because when we are constructing the buffer for the first time, there
                            //is nothing to transfer from
                            Buffer buffer = VkEngine.CreateResourceBuffer(size, BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferSrcBit);
                            memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                            handle = buffer.Handle;

                            void* pointer = memoryRegion.Bind();
                            bindingPoint = (IntPtr)pointer;
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            if (isDisposed) throw new InvalidOperationException("Uniform buffer already disposed!");
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        //if (memoryRegion.isBound) memoryRegion.Unbind();
                        memoryRegion.Free();
                        VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, new Buffer(handle), null);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
