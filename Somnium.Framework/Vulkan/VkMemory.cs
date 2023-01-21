using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Somnium.Framework.Vulkan
{
    public struct AllocatedMemoryRegion
    {
        public readonly AllocatedMemory memory;
        public readonly DeviceMemory handle;
        public readonly ulong start;
        public readonly ulong width;

        public bool isBound { get; private set; }

        public bool IsValid
        {
            get
            {
                return memory != null && !memory.isDisposed;
            }
        }

        public AllocatedMemoryRegion(AllocatedMemory memory, DeviceMemory handle, ulong startPosition, ulong memorySize)
        {
            this.memory = memory;
            this.handle = handle;
            start = startPosition;
            width = memorySize;
            isBound = false;
        }

        public unsafe void Clear<T>(T defaultT = default(T)) where T : unmanaged
        {
            int amountToAlloc = (int)width / sizeof(T);
            Span<T> temp = stackalloc T[amountToAlloc];

            T* ptr = Bind<T>();
            temp.CopyTo(new Span<T>(ptr, amountToAlloc));
            Unbind();
        }
        public unsafe T* Pin<T>() where T : unmanaged
        {
            if (memory.memoryPtr == null)
            {
                lock (memory.memoryPtrLock)
                {
                    //start, width
                    //map the entirety of the memory to the host's memory pointer
                    fixed (void** ptr = &memory.memoryPtr)
                    {
                        Interlocked.Increment(ref memory.amountBound);
                        if (VkEngine.vk.MapMemory(VkEngine.vkDevice, handle, 0, memory.maxSize, 0, ptr) != Result.Success)
                        {
                            throw new ExecutionException("Error binding to Vulkan memory!");
                        }
                        else
                        {
                            Console.WriteLine("Map memory called to " + this.ToString());
                        }
                    }
                }
            }
            return (T*)((byte*)memory.memoryPtr + start);
        }
        /// <summary>
        /// Binds the memory region by calling VkMapMemory if memory is not yet mapped,
        /// then/otherwise, returning a pointer of <typeparamref name="T"/> to this memory's region.
        /// It is not necessary to call Unbind() after calling Bind. Not calling Unbind will just keep the
        /// memory bound. It is also safe to call Bind() every frame.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ExecutionException"></exception>
        public unsafe T* Bind<T>() where T : unmanaged
        {
            if (memory.memoryPtr == null)
            {
                lock (memory.memoryPtrLock)
                {
                    //start, width
                    //map the entirety of the memory to the host's memory pointer
                    fixed (void** ptr = &memory.memoryPtr)
                    {
                        if (VkEngine.vk.MapMemory(VkEngine.vkDevice, handle, 0, memory.maxSize, 0, ptr) != Result.Success)
                        {
                            throw new ExecutionException("Error binding to Vulkan memory!");
                        }
                        else
                        {
                            Console.WriteLine("Map memory called to " + this.ToString());
                        }
                    }
                }
            }
            if (!isBound)
            {
                isBound = true;
                Interlocked.Increment(ref memory.amountBound);
            }
            //fixed (void* ptr = memory.memoryPtr)
            //{
            return (T*)((byte*)memory.memoryPtr + start);
            //}
        }
        public unsafe void* Bind()
        {
            if (memory.memoryPtr == null)
            {
                lock (memory.memoryPtrLock)
                {
                    //start, width
                    //map the entirety of the memory to the host's memory pointer
                    fixed (void** ptr = &memory.memoryPtr)
                    {
                        if (VkEngine.vk.MapMemory(VkEngine.vkDevice, handle, 0, memory.maxSize, 0, ptr) != Result.Success)
                        {
                            throw new ExecutionException("Error binding to Vulkan memory!");
                        }
                        else
                        {
                            Console.WriteLine("Map memory called to " + this.ToString());
                        }
                    }
                }
            }
            if (!isBound)
            {
                isBound = true;
                Interlocked.Increment(ref memory.amountBound);
            }
            fixed (void** ptr = &memory.memoryPtr)
            {
                return (byte*)*ptr + start;
            }
            //return (memory.memoryPtr + start);
        }
        public unsafe void Unbind()
        {
            isBound = false;
            Interlocked.Decrement(ref memory.amountBound);
            if (memory.amountBound < 0)
            {
                throw new ExecutionException("Amount of memory regions bound to memory somehow negative!");
            }
            if (memory.amountBound == 0)
            {
                Console.WriteLine("Unmap Memory called to " + this.ToString());
                VkEngine.vk.UnmapMemory(VkEngine.vkDevice, handle);

                lock (memory.memoryPtrLock)
                {
                    memory.memoryPtr = null;
                }

            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            AllocatedMemoryRegion other = (AllocatedMemoryRegion)obj;
            return this == other;
        }
        public static bool operator ==(AllocatedMemoryRegion A, AllocatedMemoryRegion B)
        {
            return A.handle.Handle == B.handle.Handle && A.start == B.start && A.width == B.width;
        }
        public static bool operator !=(AllocatedMemoryRegion A, AllocatedMemoryRegion B)
        {
            return A.handle.Handle != B.handle.Handle || A.start != B.start || A.width != B.width;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(handle.Handle, start, width);
        }
        public override string ToString()
        {
            return $"(Handle: {handle.Handle}, Start: {start}, Size: {width})";
        }
        public void Free() => memory.Free(this);
    }
    /// <summary>
    /// A handler for a single VkDeviceMemory pointer.
    /// </summary>
    public unsafe class AllocatedMemory : IDisposable
    {
        public static int totalDeviceMemories { get; private set; }

        /// <summary>
        /// Amount of memory regions that have called map to this
        /// </summary>
        public uint amountBound;
        public ulong maxSize;
        public DeviceMemory handle;
        public UnorderedList<AllocatedMemoryRegion> regions;
        public UnorderedList<AllocatedMemoryRegion> gaps;

        public void* memoryPtr = null;
        public object memoryPtrLock = new object();

        public AllocatedMemory(DeviceMemory handle, ulong size)
        {
            amountBound = 0;
            this.maxSize = size;
            this.handle = handle;

            regions = new UnorderedList<AllocatedMemoryRegion>();
            gaps = new UnorderedList<AllocatedMemoryRegion>();

            totalDeviceMemories++;
        }

        public bool isDisposed { get; private set; }

        /// <summary>
        /// Checks whether the DeviceMemory has <paramref name="requiredSpace"/>.
        /// </summary>
        /// <param name="requiredSpace"></param>
        /// <returns>The index of the area with space to fit the memory</returns>
        public virtual AllocatedMemoryRegion TryAllocate(ulong requiredSpace)
        {
            ulong finalPosition = 0;
            ulong totalSize = maxSize;
            for (int i = 0; i < regions.Count; i++)
            {
                totalSize -= regions[i].width;
                //get the maximum extents of the area of filled device memory
                finalPosition = Math.Max(finalPosition, regions[i].start + regions[i].width);
            }
            if (requiredSpace <= totalSize)
            {
                for (int i = gaps.Count - 1; i >= 0; i--)
                {
                    //memory allocated needs to be continuous (thankfully)

                    //if our memory can fit within the gaps (The width is equal to or smaller)
                    if (requiredSpace <= gaps[i].width)
                    {
                        AllocatedMemoryRegion copy = gaps[i];
                        ulong remainder = copy.width - requiredSpace;
                        AllocatedMemoryRegion result = new AllocatedMemoryRegion(this, handle, gaps[i].start, requiredSpace);
                        regions.Add(result);
                        if (remainder > 0)
                        {
                            //if our memory is smaller than the gap, allocate and shrink the gap
                            gaps[i] = new AllocatedMemoryRegion(this, handle, copy.start + requiredSpace, remainder);
                        }
                        else
                        {
                            //otherwise, we fit perfectly and we can remove the gap
                            gaps.RemoveAt(i);
                        }
                        return result;
                    }
                }
                //otherwise, there are no gaps or no gaps of sufficient size
                //and thus we should create new memory if there is enough memory at the back.
                //if this is false, we lead to the final (negative) case, where there is enough space in total
                //in the buffer, but not enough continuous space for us.
                if (finalPosition + requiredSpace <= maxSize)
                {
                    AllocatedMemoryRegion region = new AllocatedMemoryRegion(this, handle, finalPosition, requiredSpace);
                    regions.Add(region);
                    return region;
                }
            }
            //if either there is not enough space or not enough continuous space, we return 'null'
            return default;
        }
        public virtual void Free(AllocatedMemoryRegion region, bool suppressErrors = false)
        {
            if (region.handle.Handle != handle.Handle)
            {
                throw new InvalidOperationException("Attempting to remove a region of memory from this AllocatedMemory that does not belong to it!");
            }
            for (int i = regions.Count - 1; i >= 0; i--)
            {
                if (regions[i] == region)
                {
                    regions.RemoveAt(i);
                    gaps.Add(region);
                    Console.WriteLine("Freed memory at " + region.ToString());
                    return;
                }
            }
            if (!suppressErrors) throw new System.Collections.Generic.KeyNotFoundException("Could not locate memory region of starting position " + region.start + " in the pool!");
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                unsafe
                {
                    VkEngine.vk.FreeMemory(VkEngine.vkDevice, handle, null);
                }
                regions = null;
                gaps = null;
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
    //one memory pool for each type of device memory
    public unsafe class MemoryPool : IDisposable
    {
        public bool isDisposed { get; private set; }
        public readonly uint memoryTypeIndex;
        public UnorderedList<AllocatedMemory> allocatedMemories;
        public MemoryPool(uint memoryTypeIndex)
        {
            this.memoryTypeIndex = memoryTypeIndex;
            allocatedMemories = new UnorderedList<AllocatedMemory>();
        }
        public AllocatedMemoryRegion AllocateMemory(VkGPU GPU, ulong requiredSpace, ulong memoryCreationSize)
        {
            for (int i = 0; i < allocatedMemories.Count; i++)
            {
                var result = allocatedMemories[i].TryAllocate(requiredSpace);
                if (result != default)
                {
                    Console.WriteLine("Allocated memory at " + result.ToString());
                    return result;
                }
            }
            if (AllocatedMemory.totalDeviceMemories >= GPU.limits.maxMemoryAllocationsCount)
            {
                throw new AssetCreationException("Reached the max amount of VkDeviceMemories and cannot create another one!");
            }
            ulong minSize = GPU.limits.minMemoryAllocSize;
            ulong sizeToAllocate = Math.Max(((ulong)Mathf.Ceiling((float)memoryCreationSize / (float)minSize) * minSize), ((ulong)Mathf.Ceiling((float)requiredSpace / (float)minSize) * minSize));

            MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
            allocInfo.SType = StructureType.MemoryAllocateInfo;
            allocInfo.MemoryTypeIndex = memoryTypeIndex;
            allocInfo.AllocationSize = sizeToAllocate;

            DeviceMemory deviceMemory;
            if (VkEngine.vk.AllocateMemory(VkEngine.vkDevice, in allocInfo, null, &deviceMemory) != Result.Success)
            {
                throw new AssetCreationException("Failed to allocate Vulkan memory!");
            }

            AllocatedMemory allocatedMemory = new AllocatedMemory(deviceMemory, sizeToAllocate);
            Console.WriteLine("Created new memory for device " + deviceMemory.Handle + " with size " + sizeToAllocate);
            allocatedMemories.Add(allocatedMemory);
            AllocatedMemoryRegion region = allocatedMemory.TryAllocate(requiredSpace); //we dont allocate the sizeToAllocate: That's the size for the buffer
            if (region == default)
            {
                throw new AssetCreationException("Failed to allocate memory in new buffer!");
            }
            Console.WriteLine("Allocated memory at " + region.ToString());
            return region;
        }
        public void FreeMemory(AllocatedMemory allocatedMemory)
        {
            for (int i = 0; i < allocatedMemories.Count; i++)
            {
                if (allocatedMemories[i].handle.Handle == allocatedMemory.handle.Handle)
                {
                    VkEngine.vk.FreeMemory(VkEngine.vkDevice, allocatedMemories[i].handle, null);
                    allocatedMemories.RemoveAt(i);
                    return;
                }
            }
            throw new System.Collections.Generic.KeyNotFoundException("Could not locate memory of handle " + allocatedMemory.handle.Handle + " in the pool!");
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                for (int i = allocatedMemories.Count - 1; i >= 0; i--)
                {
                    VkEngine.vk.FreeMemory(VkEngine.vkDevice, allocatedMemories[i].handle, null);
                }
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }
    }
    public static class VkMemory
    {

        public static SparseArray<MemoryPool> memoryPools = new SparseArray<MemoryPool>(null);
        public static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }

        /// <summary>
        /// Allocates a region of memory for the input buffer
        /// </summary>
        /// <param name="buffer">The buffer you wish to create memory for</param>
        /// <param name="memoryPropertyFlags">What properties the allocated memory should possess</param>
        ///<param name="autoBind">Whether to automatically bind the buffer to the memory region</param>
        /// <returns></returns>
        public static unsafe AllocatedMemoryRegion malloc(Buffer buffer, MemoryPropertyFlags memoryPropertyFlags, bool autoBind = true)
        {
            MemoryRequirements memoryRequirements;
            VkEngine.vk.GetBufferMemoryRequirements(VkEngine.vkDevice, buffer, &memoryRequirements);

            AllocatedMemoryRegion memoryRegion = malloc(memoryRequirements, memoryPropertyFlags, (ulong)(Application.memoryForBuffersInMiB * 1024 * 1024));

            if (autoBind)
            {
                if (vk.BindBufferMemory(VkEngine.vkDevice, buffer, memoryRegion.handle, memoryRegion.start) != Result.Success)
                {
                    throw new AssetCreationException("Failed to bind Vulkan Vertex Buffer to allocated Memory!");
                }
            }

            return memoryRegion;
        }
        /// <summary>
        /// Allocates a region of memory for the input image
        /// </summary>
        /// <param name="image">The image you wish to create memory for</param>
        /// <param name="memoryPropertyFlags">What properties the allocated memory should possess</param>
        ///<param name="autoBind">Whether to automatically bind the buffer to the memory region</param>
        /// <returns></returns>
        public static unsafe AllocatedMemoryRegion malloc(Image image, MemoryPropertyFlags memoryPropertyFlags, bool autoBind = true)
        {
            MemoryRequirements memoryRequirements;
            VkEngine.vk.GetImageMemoryRequirements(VkEngine.vkDevice, image, &memoryRequirements);

            AllocatedMemoryRegion memoryRegion = malloc(memoryRequirements, memoryPropertyFlags, (ulong)(Application.memoryForImagesInMiB * 1024 * 1024)); //we malloc a much larger DeviceMemory size for potentially image-holding buffers

            if (autoBind)
            {
                if (vk.BindImageMemory(VkEngine.vkDevice, image, memoryRegion.handle, memoryRegion.start) != Result.Success)
                {
                    throw new AssetCreationException("Failed to bind Vulkan Image to allocated Memory!");
                }
            }

            return memoryRegion;
        }
        public static unsafe AllocatedMemoryRegion malloc(MemoryRequirements memoryRequirements, MemoryPropertyFlags memoryPropertyFlags, ulong memoryCreationSize = 65536)
        {
            uint memoryTypeIndex = Utils.FindMemoryType(memoryRequirements.MemoryTypeBits, memoryPropertyFlags, VkEngine.CurrentGPU);

            if (!memoryPools.WithinLength(memoryTypeIndex) || memoryPools[memoryTypeIndex] == null)
            {
                Console.WriteLine("Created new Memory Pool at index " + memoryTypeIndex);
                memoryPools.Insert(memoryTypeIndex, new MemoryPool(memoryTypeIndex));
            }
            MemoryPool pool = memoryPools[memoryTypeIndex];
            AllocatedMemoryRegion memoryRegion = pool.AllocateMemory(VkEngine.CurrentGPU, memoryRequirements.Size, memoryCreationSize);

            return memoryRegion;
        }
        public static void Dispose()
        {
            for (int i = 0; i < memoryPools.values.Length; i++)
            {
                memoryPools.values[i]?.Dispose();
            }
        }
    }
}
