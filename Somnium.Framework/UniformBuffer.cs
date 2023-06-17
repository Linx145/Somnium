#if VULKAN
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
#endif
#if WGPU
using Silk.NET.WebGPU;
using Somnium.Framework.WGPU;
using Buffer = Silk.NET.WebGPU.Buffer;
#endif
using System;

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
#if VULKAN
        AllocatedMemoryRegion memoryRegion;
#endif
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

#if VULKAN
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
#endif
#if WGPU
                if (application.runningBackend == Backends.WebGPU)
                {
                    unsafe
                    {
                        CommandEncoderDescriptor commandEncoderDescriptor = new CommandEncoderDescriptor();
                        var commandEncoder = WGPUEngine.wgpu.DeviceCreateCommandEncoder(WGPUEngine.device, &commandEncoderDescriptor);

                        WGPUEngine.wgpu.QueueWriteBuffer(WGPUEngine.queue, (Buffer*)handle, offset, &data, (nuint)sizeof(T));

                        var commandBuffer = WGPUEngine.wgpu.CommandEncoderFinish(commandEncoder, new CommandBufferDescriptor());
                        WGPUEngine.wgpu.QueueSubmit(WGPUEngine.queue, 1, &commandBuffer);
                    }
                }
#endif
            }
        }
        public void SetData<T>(T[] data, ulong offset) where T : unmanaged
        {
            unsafe
            {
#if VULKAN
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
                T* ptr = (T*)((byte*)bindingPoint + offset);
                data.CopyTo(new Span<T>(ptr, data.Length));
#endif
#if WGPU
                if (application.runningBackend == Backends.WebGPU)
                {
                    unsafe
                    {
                        CommandEncoderDescriptor commandEncoderDescriptor = new CommandEncoderDescriptor();
                        var commandEncoder = WGPUEngine.wgpu.DeviceCreateCommandEncoder(WGPUEngine.device, &commandEncoderDescriptor);

                        fixed (void* ptr = &data[0])
                        {
                            WGPUEngine.wgpu.QueueWriteBuffer(WGPUEngine.queue, (Buffer*)handle, offset, ptr, (nuint)(sizeof(T) * data.Length));

                            var commandBuffer = WGPUEngine.wgpu.CommandEncoderFinish(commandEncoder, new CommandBufferDescriptor());
                            WGPUEngine.wgpu.QueueSubmit(WGPUEngine.queue, 1, &commandBuffer);
                        }
                    }
                }
#endif
            }
        }
        public void SetDataBytes(ReadOnlySpan<byte> bytes, ulong offset)
        {
#if VULKAN
            if (application.runningBackend == Backends.Vulkan)
            {
                unsafe
                {
                    byte* ptr = (byte*)bindingPoint + offset;
                    bytes.CopyTo(new Span<byte>(ptr, bytes.Length));
                }
            }
#endif
#if WGPU
            if (application.runningBackend == Backends.WebGPU)
            {
                unsafe
                {
                    CommandEncoderDescriptor commandEncoderDescriptor = new CommandEncoderDescriptor();
                    var commandEncoder = WGPUEngine.wgpu.DeviceCreateCommandEncoder(WGPUEngine.device, &commandEncoderDescriptor);

                    fixed (void* ptr = &bytes[0])
                    {
                        WGPUEngine.wgpu.QueueWriteBuffer(WGPUEngine.queue, (Buffer*)handle, offset, ptr, (nuint)(bytes.Length));

                        var commandBuffer = WGPUEngine.wgpu.CommandEncoderFinish(commandEncoder, new CommandBufferDescriptor());
                        WGPUEngine.wgpu.QueueSubmit(WGPUEngine.queue, 1, &commandBuffer);
                    }
                }
            }
#endif
        }
#if VULKAN
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
            var newMemory = VkMemory.malloc("Uniform Buffer", newBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            
            //copy old buffer data into new buffer
            Buffer oldBuffer = new Buffer(handle);
            VkEngine.StaticCopyResourceBuffer(application, oldBuffer, newBuffer, size);

            //dispose of the old memory region
            if (memoryRegion.isBound) memoryRegion.Unbind();
            memoryRegion.Free();

            //dispose of the old buffer
            VkEngine.DestroyResourceBuffer(new Buffer(handle));

            //update variables
            size = newMaxSize;
            handle = newBuffer.Handle;

            memoryRegion = newMemory;

            //and lastly, bind new memory
            void* pointer = memoryRegion.Bind();
            bindingPoint = (IntPtr)pointer;
        }
#endif
        public void Construct()
        {
            switch (application.runningBackend)
            {
#if WGPU
                case Backends.WebGPU:
                    unsafe
                    {
                        var descriptor = new BufferDescriptor()
                        {
                            Size = size,
                            Usage = BufferUsage.Uniform | BufferUsage.CopyDst
                        };
                        handle = (ulong)WGPUEngine.wgpu.DeviceCreateBuffer(WGPUEngine.device, &descriptor);
                    }
                    break;
#endif
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        if (!isDynamic)
                        {
                            Buffer buffer = VkEngine.CreateResourceBuffer(size, BufferUsageFlags.UniformBufferBit);
                            memoryRegion = VkMemory.malloc("Uniform Buffer", buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                            handle = buffer.Handle;

                            void* pointer = memoryRegion.Bind();
                            bindingPoint = (IntPtr)pointer;
                        }
                        else
                        {
                            //dont need TransferDstBit because when we are constructing the buffer for the first time, there
                            //is nothing to transfer from
                            Buffer buffer = VkEngine.CreateResourceBuffer(size, BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferSrcBit);
                            memoryRegion = VkMemory.malloc("Uniform Buffer", buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                            handle = buffer.Handle;

                            void* pointer = memoryRegion.Bind();
                            bindingPoint = (IntPtr)pointer;
                        }
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            if (isDisposed) throw new InvalidOperationException("Uniform buffer already disposed!");
            switch (application.runningBackend)
            {
#if WGPU
                case Backends.WebGPU:
                    unsafe
                    {
                        WGPUEngine.crab.BufferDrop((Buffer*)handle);
                    }
                    break;
#endif
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        //if (memoryRegion.isBound) memoryRegion.Unbind();
                        memoryRegion.Free();
                        VkEngine.DestroyResourceBuffer(new Buffer(handle));
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
