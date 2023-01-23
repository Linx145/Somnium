using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Reflection.Emit;

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
                case Backends.Vulkan:
                    unsafe
                    {
                        VkEngine.vk.ResetCommandBuffer(new CommandBuffer(handle), CommandBufferResetFlags.None);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Begin()
        {
            switch (application.runningBackend)
            {
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
                default:
                    throw new NotImplementedException();
            }
        }
        public void End()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        if (VkEngine.vk.EndCommandBuffer(new CommandBuffer(handle)) != Result.Success)
                        {
                            throw new ExecutionException("Error ending Vulkan Command Buffer!");
                        }
                    }
                    break;
                default: 
                    throw new NotImplementedException();
            }
        }
        public void Construct()
        {
            switch (application.runningBackend)
            {
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
               default:
                    throw new NotImplementedException();
            }
        }

        public static implicit operator CommandBuffer(CommandCollection collection)
        {
            return new CommandBuffer(collection.handle);
        }
    }
}
