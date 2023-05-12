#if VULKAN
using Silk.NET.Vulkan;
using System;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Somnium.Framework.Vulkan
{
    public class FrameData : IDisposable
    {
        /// <summary>
        /// waited on in the GPU for when presenting the queue
        /// </summary>
        public Semaphore presentSemaphore;
        /// <summary>
        /// waited on in the GPU for when submitting the queue
        /// </summary>
        public Semaphore renderSemaphore;
        /// <summary>
        /// waited on by the CPU for the render loop to finish rendering and submitting
        /// </summary>
        public Fence fence;

        //public CommandPoolCreateInfo poolCreateInfo;
        public CommandRegistrar commandPool;
        public CommandCollection commandBuffer;//CommandRegistrar commandPool;

        public UniformBuffer unifiedDynamicBuffer;
        //public CommandBuffer commandBuffer;

        public bool isDisposed { get; private set; }

        public FrameData(Application application)//Semaphore presentSemaphore, Semaphore renderSemaphore, CommandPool commandPool)
        {
            commandPool = new CommandRegistrar(application, false, CommandQueueType.GeneralPurpose);
            commandBuffer = new CommandCollection(application, commandPool, true);
            presentSemaphore = VkEngine.CreateSemaphore();
            renderSemaphore = VkEngine.CreateSemaphore();
            fence = VkEngine.CreateFence();

            unifiedDynamicBuffer = new UniformBuffer(application, 256, true);
        }

        public unsafe void Dispose()
        {
            if (!isDisposed)
            {
                var vk = VkEngine.vk;
                var vkDevice = VkEngine.vkDevice;

                vk.DestroySemaphore(vkDevice, presentSemaphore, null);
                vk.DestroySemaphore(vkDevice, renderSemaphore, null);
                vk.DestroyFence(vkDevice, fence, null);

                commandPool.Dispose();

                unifiedDynamicBuffer?.Dispose();
                isDisposed = true;
            }
        }
    }
}
#endif