#if VULKAN
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
#endif
#if DX12
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Somnium.Framework.DX12;
#endif
using System;

namespace Somnium.Framework
{
    public class CommandCollection
    {
        Application application;
        public nint handle;
        public CommandRegistrar memoryPool;
        public bool usedForDirectSubmission;

        public CommandCollection(Application application, CommandRegistrar memoryPool, bool usedForDirectSubmission = true)
        {
            this.application = application;
            this.memoryPool = memoryPool;
            this.usedForDirectSubmission = usedForDirectSubmission;

            Construct();
        }

        public void Reset()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        VkEngine.vk.ResetCommandBuffer(new CommandBuffer(handle), CommandBufferResetFlags.None);
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void Begin()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo();
                        beginInfo.Flags = CommandBufferUsageFlags.None;
                        beginInfo.SType = StructureType.CommandBufferBeginInfo;
                        //only relevant for secondary command buffers. Checks which state to inherit from when beginning
                        beginInfo.PInheritanceInfo = null;
                        beginInfo.Flags = CommandBufferUsageFlags.OneTimeSubmitBit;

                        if (VkEngine.vk.BeginCommandBuffer(new CommandBuffer(handle), in beginInfo) != Result.Success)
                        {
                            throw new ExecutionException("Failed to begin Vulkan Command Buffer!");
                        }
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void End()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        if (VkEngine.vk.EndCommandBuffer(new CommandBuffer(handle)) != Result.Success)
                        {
                            throw new ExecutionException("Error ending Vulkan Command Buffer!");
                        }
                    }
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
                    unsafe
                    {
                        var allocInfo = new CommandBufferAllocateInfo();
                        allocInfo.SType = StructureType.CommandBufferAllocateInfo;
                        allocInfo.CommandPool = new CommandPool(memoryPool.handle);
                        allocInfo.Level = usedForDirectSubmission ? CommandBufferLevel.Primary : CommandBufferLevel.Secondary;
                        allocInfo.CommandBufferCount = 1;

                        CommandBuffer result;
                        if (VkEngine.vk.AllocateCommandBuffers(VkEngine.vkDevice, in allocInfo, &result) != Result.Success)
                        {
                            throw new InitializationException("Failed to create Vulkan Command Buffer!");
                        }
                        handle = result.Handle;
                    }
                    break;
#endif
#if DX12
                case Backends.DX12:
                    unsafe
                    {
                        CommandQueueDesc queueDesc = new CommandQueueDesc();
                        var ID = ID3D12CommandQueue.Guid;

                        ID3D12CommandQueue* cmdQueue;

                        if (new HResult(Dx12Engine.device->CreateCommandQueue(queueDesc, &ID, (void**)&cmdQueue)).IsSuccess)
                        {
                            throw new AssetCreationException("Failed to create command queue!");
                        }
                        handle = (nint)cmdQueue;
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }

#if VULKAN
        public static implicit operator CommandBuffer(CommandCollection collection)
        {
            return new CommandBuffer(collection.handle);
        }
#endif
#if DX12
        public static unsafe implicit operator ID3D12CommandQueue*(CommandCollection collection)
        {
            return (ID3D12CommandQueue*)collection.handle;
        }
#endif
    }
}
