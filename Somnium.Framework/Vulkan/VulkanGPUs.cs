using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VulkanGPUs
    {
        public static VulkanGPUInfo SelectGPU()
        {
            uint deviceCount = 0;
            VulkanEngine.vk.EnumeratePhysicalDevices(VulkanEngine.vkInstance, ref deviceCount, null);

            if (deviceCount == 0)
            {
                throw new InitializationException("No GPU found!");
            }

            PhysicalDevice[] devices = new PhysicalDevice[deviceCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                VulkanEngine.vk.EnumeratePhysicalDevices(VulkanEngine.vkInstance, ref deviceCount, devicesPtr);
            }
            int highestScore = 0;
            int selectedIndex = -1;
            for (int i = 0; i < devices.Length; i++)
            {
                int score = 0;
                if (IsDeviceSuitable(in devices[i], ref score))
                {
                    selectedIndex = i;
                    break;
                }
                else
                {
                    if (score > highestScore)
                    {
                        highestScore = score;
                        selectedIndex = i;
                    }
                }
            }
            if (selectedIndex < 0)
            {
                throw new InitializationException("No GPU was selected!");
            }
            return new VulkanGPUInfo(devices[selectedIndex], "", GetQueueInfo(devices[selectedIndex]));//devices[selectedIndex];
        }
        public static bool IsDeviceSuitable(in PhysicalDevice device, ref int score)
        {
            //TODO: Accommodate for the losers with multiple GPUs
            //TODO: Make scriptable
            var properties = GetQueueInfo(in device);
            if (properties.graphicsBitIndex == null)
            {
                score = -1;
            }
            return true;
        }
        public static QueueProperties GetQueueInfo(in PhysicalDevice device)
        {
            uint flagsCount = 0;
            VulkanEngine.vk.GetPhysicalDeviceQueueFamilyProperties(device, ref flagsCount, null);

            QueueFamilyProperties[] properties = new QueueFamilyProperties[flagsCount];
            fixed (QueueFamilyProperties* ptr = properties)
            {
                VulkanEngine.vk.GetPhysicalDeviceQueueFamilyProperties(device, ref flagsCount, ptr);
            }

            return new QueueProperties(properties);
        }
    }
}
