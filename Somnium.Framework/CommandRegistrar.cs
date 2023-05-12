#if VULKAN
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
#endif
using System;

namespace Somnium.Framework
{
    /// <summary>
    /// A pool to allocate command collections from.
    /// </summary>
    public class CommandRegistrar : IDisposable
    {
        private Application application;
        public ulong handle;
        /// <summary>
        /// Whether the command memory should be used to allocate 'transient' (short-lived) command buffers.
        /// These are usually used for one time submissions.
        /// By default, there are numeorus command memories
        /// </summary>
        public readonly bool isForTransientCommands;
        /// <summary>
        /// The command queue that command collections allocated from this pool should utilise
        /// </summary>
        public readonly CommandQueueType commandQueueType;
        public CommandRegistrar(Application application, bool isForTransientCommands, CommandQueueType commandQueueType)
        {
            this.application = application;
            this.isForTransientCommands = isForTransientCommands;
            this.commandQueueType = commandQueueType;
            Construct();
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        var poolCreateInfo = new CommandPoolCreateInfo();
                        poolCreateInfo.SType = StructureType.CommandPoolCreateInfo;
                        //we reset our command buffers every frame individually, so use this
                        poolCreateInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;
                        if (isForTransientCommands)
                        {
                            poolCreateInfo.Flags = poolCreateInfo.Flags | CommandPoolCreateFlags.TransientBit;
                        }
                        poolCreateInfo.QueueFamilyIndex = VkEngine.CurrentGPU.queueInfo.GetQueue(VkEngine.CurrentGPU.Device, commandQueueType)!.Value;//.GetGeneralPurposeQueue(CurrentGPU.Device)!.Value;

                        CommandPool result;
                        if (VkEngine.vk.CreateCommandPool(VkEngine.vkDevice, in poolCreateInfo, null, &result) != Result.Success)
                        {
                            throw new InitializationException("Failed to create general Vulkan Command Pool!");
                        }
                        handle = result.Handle;
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        if (handle != 0)
                        {
                            VkEngine.vk.DestroyCommandPool(VkEngine.vkDevice, new CommandPool(handle), null);
                        }
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
