#if VULKAN
using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public class VkCommandBuffer //automatically freed when command pool is disposed
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }
        public CommandBuffer handle;
        public CommandBufferAllocateInfo allocInfo;
        private VkCommandBuffer()
        {

        }
        public void Reset()
        {
            vk.ResetCommandBuffer(handle, CommandBufferResetFlags.None);
        }
        public void Begin()
        {
            unsafe
            {
                CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo();
                beginInfo.Flags = CommandBufferUsageFlags.None;
                beginInfo.SType = StructureType.CommandBufferBeginInfo;
                //only relevant for secondary command buffers. Checks which state to inherit from when beginning
                beginInfo.PInheritanceInfo = null;
                beginInfo.Flags = CommandBufferUsageFlags.OneTimeSubmitBit;

                if (vk.BeginCommandBuffer(handle, in beginInfo) != Result.Success)
                {
                    throw new ExecutionException("Failed to begin Vulkan Command Buffer!");
                }
            }
        }
        public void End()
        {
            if (vk.EndCommandBuffer(handle) != Result.Success)
            {
                throw new ExecutionException("Error ending Vulkan Command Buffer!");
            }
        }
        public static implicit operator CommandBuffer(VkCommandBuffer vkCommandBuffer)
        {
            return vkCommandBuffer.handle;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="level">Primary: Cannot be submitted to other command buffers, but can be submitted directly to a queue<br>Secondary: Can be submitted to other command buffers, but cannot be submitted directly to a queue</br></param>
        /// <returns></returns>
        public static unsafe VkCommandBuffer Create(CommandPool pool, CommandBufferLevel level = CommandBufferLevel.Primary)
        {
            VkCommandBuffer result = new VkCommandBuffer();

            var allocInfo = new CommandBufferAllocateInfo();
            allocInfo.SType = StructureType.CommandBufferAllocateInfo;
            allocInfo.CommandPool = pool;
            allocInfo.Level = level;
            allocInfo.CommandBufferCount = 1;

            CommandBuffer buffer;
            if (vk.AllocateCommandBuffers(VkEngine.vkDevice, in allocInfo, &buffer) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Command Buffer!");
            }
            result.allocInfo = allocInfo;
            result.handle = buffer;

            return result;
        }
    }
}
#endif