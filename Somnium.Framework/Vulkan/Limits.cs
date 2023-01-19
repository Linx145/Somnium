using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public readonly struct Limits
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }
        public readonly uint maxMemoryAllocationsCount;
        public readonly ulong minMemoryAllocSize;
        public readonly ulong minUniformBufferOffsetAlignment;

        public readonly PhysicalDeviceLimits limits;

        public Limits(VkGPU gpu)
        {
            PhysicalDeviceProperties properties;
            vk.GetPhysicalDeviceProperties(gpu.Device, out properties);
            maxMemoryAllocationsCount = properties.Limits.MaxMemoryAllocationCount;
            minMemoryAllocSize = properties.Limits.BufferImageGranularity; //1024
            minUniformBufferOffsetAlignment = properties.Limits.MinUniformBufferOffsetAlignment;
            limits = properties.Limits;
            //PhysicalDeviceLimits
        }
    }
}
