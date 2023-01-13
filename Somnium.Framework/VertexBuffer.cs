using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class VertexBuffer : IDisposable
    {
        private readonly Application application;
        private readonly bool isDynamic;

        public ulong handle;
        public int vertexCount { get; private set; }
        public VertexDeclaration vertexDeclaration { get; private set; }

        #region Vulkan
        AllocatedMemoryRegion memoryRegion;
        #endregion

        public VertexBuffer(Application application, VertexDeclaration vertexType, int vertexCount, bool isDynamic)
        {
            this.application = application;
            this.vertexCount = vertexCount;
            this.vertexDeclaration = vertexType;
            this.isDynamic = isDynamic;

            Construct();
        }
        public void SetData<T>(T[] vertices, int offset, int Length) where T : unmanaged
        {
            if (Length > vertices.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data to ");
            }
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        T* data;
                        //VkEngine.vk.MapMemory(VkEngine.vkDevice, memoryRegion.handle, memoryRegion.start, memoryRegion.width, /*0, (ulong)(vertexCount * vertexDeclaration.size)*/ 0, (void**)&data);
                        memoryRegion.Bind<T>((void**)&data);
                        vertices.AsSpan().CopyTo(new Span<T>(data + offset, Length));
                        memoryRegion.Unbind();

                        //VkEngine.vk.UnmapMemory(VkEngine.vkDevice, memoryRegion.handle);
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
                    BufferCreateInfo createInfo = new BufferCreateInfo();
                    createInfo.SType = StructureType.BufferCreateInfo;
                    createInfo.Size = (uint)(vertexCount * vertexDeclaration.size);
                    createInfo.Usage = BufferUsageFlags.VertexBufferBit;
                    createInfo.SharingMode = SharingMode.Exclusive;
                    
                    unsafe
                    {
                        Buffer buffer;
                        if (VkEngine.vk.CreateBuffer(VkEngine.vkDevice, in createInfo, null, &buffer) != Result.Success)
                        {
                            throw new AssetCreationException("Error creating Vulkan (Vertex) Buffer!");
                        }
                        handle = buffer.Handle;

                        memoryRegion = VkMemory.malloc(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                        if (VkEngine.vk.BindBufferMemory(VkEngine.vkDevice, buffer, memoryRegion.handle, memoryRegion.start) != Result.Success)
                        {
                            throw new AssetCreationException("Failed to bind Vulkan Vertex Buffer to allocated Memory!");
                        }
                            /*MemoryRequirements memoryRequirements;
                            VkEngine.vk.GetBufferMemoryRequirements(VkEngine.vkDevice, buffer, &memoryRequirements);

                            MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
                            allocInfo.SType = StructureType.MemoryAllocateInfo;
                            allocInfo.AllocationSize = memoryRequirements.Size;
                            allocInfo.MemoryTypeIndex = Utils.FindMemoryType(memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, VkEngine.CurrentGPU);

                            fixed (DeviceMemory* ptr = &deviceMemory)
                            {
                                if (VkEngine.vk.AllocateMemory(VkEngine.vkDevice, in allocInfo, null, ptr) != Result.Success)
                                {
                                    throw new AssetCreationException("Failed to allocate Vulkan memory!");
                                }
                                //offset is 0 because it refers to the offset of the vertex buffer within the alloc'd memory.
                                //However, since we explicitly created the memory for the vertex buffer, we set the offset to 0
                                if (VkEngine.vk.BindBufferMemory(VkEngine.vkDevice, buffer, deviceMemory, 0) != Result.Success)
                                {
                                    throw new AssetCreationException("Failed to bind Vulkan Vertex Buffer to allocated Memory!");
                                }
                            }*/
                            //locate a suitable memory type
                            //Utils.FindMemoryType(memoryProperties);
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
                        //VkEngine.vk.FreeMemory(VkEngine.vkDevice, deviceMemory, null);
                    }
                    break; 
                default: 
                    throw new NotImplementedException();
            }
        }
    }
}
