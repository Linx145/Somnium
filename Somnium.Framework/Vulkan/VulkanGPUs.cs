using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

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
            QueueProperties selectedQueueProperties = default;
            for (int i = 0; i < devices.Length; i++)
            {
                int score = 0;
                if (IsDeviceSuitable(in devices[i], ref score, out selectedQueueProperties))
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
            return new VulkanGPUInfo(devices[selectedIndex], "", selectedQueueProperties);//devices[selectedIndex];
        }
        public static bool IsDeviceSuitable(in PhysicalDevice device, ref int score, out QueueProperties queueProperties)
        {
            //TODO: Accommodate for the losers with multiple GPUs
            //TODO: Make scriptable
            var properties = GetQueueInfo(in device);
            queueProperties = properties;
            //check that we have at least one queue that can do Graphics rendering
            if (properties.GetGeneralPurposeQueue(in device) == null)
            {
                score = -1;
                return false;
            }
            //check that we support all the device extensions
            if (!AllRequiredDeviceExtensionsSupported(in device))
            {
                score = -1;
                return false;
            }
            //check if we support the swapchain
            var swapchainSupport = SwapChain.QuerySwapChainSupport(in device);
            if (swapchainSupport.supportedPresentModes.Length == 0 || swapchainSupport.supportedSurfaceFormats.Length == 0)
            {
                score = -1;
                return false;
            }
            return true;
        }
        public static VkExtensionProperties[] SupportedDeviceExtensions(in PhysicalDevice device)
        {
            if (!VulkanEngine.initialized)
            {
                throw new InvalidOperationException("Cannot call VulkanGPUs.SupportedExtensions() before initializing Vulkan!");
            }
            uint supportedExtensionsCount = 0;
            VulkanEngine.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &supportedExtensionsCount, null);

            ExtensionProperties[] extensionProperties = new ExtensionProperties[supportedExtensionsCount];

            VulkanEngine.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &supportedExtensionsCount, extensionProperties.AsSpan());

            VkExtensionProperties[] result = new VkExtensionProperties[supportedExtensionsCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new VkExtensionProperties(extensionProperties[i]);
            }
            return result;
        }
        public static bool AllRequiredDeviceExtensionsSupported(in PhysicalDevice device)
        {
            var extensions = SupportedDeviceExtensions(in device);
            int totalSupported = 0;
            for (int i = 0; i < extensions.Length; i++)
            {
                for (int j = 0; j < VulkanEngine.requiredDeviceExtensions.Length; j++)
                {
                    if (VulkanEngine.requiredDeviceExtensions[j] == extensions[i].ExtensionName)
                    {
                        totalSupported++;
                        break;
                    }
                }
            }
            return totalSupported == VulkanEngine.requiredDeviceExtensions.Length;
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
