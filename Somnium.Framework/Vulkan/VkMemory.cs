using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Somnium.Framework.Vulkan
{
    public readonly struct AllocatedMemoryRegion
    {
        public readonly DeviceMemory handle;
        public readonly ulong start;
        public readonly ulong width;

        public AllocatedMemoryRegion(DeviceMemory handle, ulong startPosition, ulong memorySize)
        {
            this.handle = handle;
            start = startPosition;
            width = memorySize;
        }

        public unsafe void Bind<T>(void** data) where T: unmanaged
        {
            VkEngine.vk.MapMemory(VkEngine.vkDevice, handle, start, width, 0, data);
        }
        public void Unbind()
        {
            VkEngine.vk.UnmapMemory(VkEngine.vkDevice, handle);
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
    }
    public struct AllocatedMemory
    {
        public static int totalDeviceMemories { get; private set; }

        public ulong maxSize;
        public DeviceMemory handle;
        public UnorderedList<AllocatedMemoryRegion> regions;
        public UnorderedList<AllocatedMemoryRegion> gaps;

        public AllocatedMemory(DeviceMemory handle, ulong size)
        {
            this.maxSize = size;
            this.handle = handle;

            regions = new UnorderedList<AllocatedMemoryRegion>();
            gaps = new UnorderedList<AllocatedMemoryRegion>();

            totalDeviceMemories++;
        }

        /// <summary>
        /// Checks whether the DeviceMemory has <paramref name="requiredSpace"/>.
        /// </summary>
        /// <param name="requiredSpace"></param>
        /// <returns>The index of the area with space to fit the memory</returns>
        public AllocatedMemoryRegion TryAllocate(ulong requiredSpace)
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
                        AllocatedMemoryRegion result = new AllocatedMemoryRegion(handle, gaps[i].start, requiredSpace);
                        regions.Add(result);
                        if (remainder > 0)
                        {
                            //if our memory is smaller than the gap, allocate and shrink the gap
                            gaps[i] = new AllocatedMemoryRegion(handle, copy.start + requiredSpace, remainder);
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
                    return new AllocatedMemoryRegion(handle, finalPosition, requiredSpace);
                }
            }
            //if either there is not enough space or not enough continuous space, we return 'null'
            return default;
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
        public AllocatedMemoryRegion AllocateMemory(VkGPU GPU, ulong requiredSpace)
        {
            for (int i = 0; i < allocatedMemories.Count; i++)
            {
                var result = allocatedMemories[i].TryAllocate(requiredSpace);
                if (result != default)
                {
                    return result;
                }
            }
            if (AllocatedMemory.totalDeviceMemories >= GPU.limits.maxMemoryAllocationsCount)
            {
                throw new AssetCreationException("Reached the max amount of VkDeviceMemories and cannot create another one!");
            }
            ulong minSize = GPU.limits.minMemoryAllocSize;
            ulong sizeToAllocate = ((ulong)Mathf.Ceiling((float)requiredSpace / (float)minSize) * minSize);

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
            allocatedMemories.Add(allocatedMemory);
            AllocatedMemoryRegion region = allocatedMemory.TryAllocate(requiredSpace); //we dont allocate the sizeToAllocate: That's the size for the buffer
            if (region == default)
            {
                throw new AssetCreationException("Failed to allocate memory in new buffer!");
            }
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
        /// <returns></returns>
        public static unsafe AllocatedMemoryRegion malloc(Buffer buffer, MemoryPropertyFlags memoryPropertyFlags)
        {
            MemoryRequirements memoryRequirements;
            VkEngine.vk.GetBufferMemoryRequirements(VkEngine.vkDevice, buffer, &memoryRequirements);

            uint memoryTypeIndex = Utils.FindMemoryType(memoryRequirements.MemoryTypeBits, memoryPropertyFlags, VkEngine.CurrentGPU);

            if (!memoryPools.WithinLength(memoryTypeIndex) || memoryPools[memoryTypeIndex] == null)
            {
                memoryPools.Insert(memoryTypeIndex, new MemoryPool(memoryTypeIndex));
            }
            MemoryPool pool = memoryPools[memoryTypeIndex];
            return pool.AllocateMemory(VkEngine.CurrentGPU, memoryRequirements.Size);
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
