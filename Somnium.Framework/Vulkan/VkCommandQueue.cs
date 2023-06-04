#if VULKAN
using Silk.NET.Vulkan;
using System.Threading;

namespace Somnium.Framework.Vulkan
{
    public sealed class VkCommandQueue
    {
        public Queue queue;
        public ReaderWriterLockSlim externalLock;

        public VkCommandQueue(Queue queue)
        {
            this.queue = queue;
            externalLock = new ReaderWriterLockSlim();
        }

        public static implicit operator Queue(VkCommandQueue queue)
        {
            return queue.queue;
        }

        public void Lock()
        {
            externalLock.EnterWriteLock();
        }
        public void Unlock()
        {
            externalLock.ExitWriteLock();
        }
    }
}
#endif