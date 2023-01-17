using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public struct FrameData
    {
        /// <summary>
        /// waited on in the GPU for when presenting the queue
        /// </summary>
        public Semaphore presentSemaphore;
        /// <summary>
        /// waited on in the GPU for when submitting the queue
        /// </summary>
        public Semaphore renderSemaphore;

        //public CommandPoolCreateInfo poolCreateInfo;
        public CommandPool commandPool;
        //public CommandBuffer commandBuffer;
        public FrameData(Semaphore presentSemaphore, Semaphore renderSemaphore, CommandPool commandPool)
        {
            this.presentSemaphore = presentSemaphore;
            this.renderSemaphore = renderSemaphore;
            this.commandPool = commandPool;
        }
    }
}
