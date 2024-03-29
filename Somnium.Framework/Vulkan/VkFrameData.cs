﻿#if VULKAN
using Silk.NET.Vulkan;
using System;
using System.Threading;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Somnium.Framework.Vulkan
{
    public class VkFrameData : IDisposable
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
        public ReaderWriterLockSlim commandBufferLock;
        //public CommandBuffer commandBuffer;

        public bool isDisposed { get; private set; }

        public VkFrameData(Application application)//Semaphore presentSemaphore, Semaphore renderSemaphore, CommandPool commandPool)
        {
            commandPool = new CommandRegistrar(application, false, CommandQueueType.GeneralPurpose);
            commandBuffer = new CommandCollection(application, commandPool, true);
            presentSemaphore = VkEngine.CreateSemaphore();
            renderSemaphore = VkEngine.CreateSemaphore();
            fence = VkEngine.CreateFence();
            commandBufferLock = new ReaderWriterLockSlim();
        }

        public unsafe void Dispose()
        {
            if (!isDisposed)
            {
                var vk = VkEngine.vk;
                var vkDevice = VkEngine.vkDevice;

                commandBufferLock.Dispose();

                vk.DestroySemaphore(vkDevice, presentSemaphore, null);
                vk.DestroySemaphore(vkDevice, renderSemaphore, null);
                vk.DestroyFence(vkDevice, fence, null);

                commandPool.Dispose();
                isDisposed = true;
            }
        }
    }
}
#endif