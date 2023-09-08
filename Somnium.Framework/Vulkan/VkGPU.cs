#if VULKAN
using Silk.NET.Vulkan;
using System;

namespace Somnium.Framework.Vulkan
{
    public struct VkGPU
    {
        public PhysicalDevice Device;
        public Device LogicalDevice;
        public QueueProperties queueInfo;
        public Limits limits;
        public string Name;

        public VkCommandQueue AllPurposeQueue;
        public VkCommandQueue DedicatedGraphicsQueue;
        public VkCommandQueue DedicatedComputeQueue;
        public VkCommandQueue DedicatedTransferQueue;

        public VkGPU(PhysicalDevice device, string name, QueueProperties queueInfo)
        {
            Device = device;
            this.Name = name;
            this.queueInfo = queueInfo;
            LogicalDevice = default;

            limits = default;

            AllPurposeQueue = default;
            DedicatedGraphicsQueue = default;
            DedicatedComputeQueue = default;
            DedicatedTransferQueue = default;

            limits = new Limits(this);
        }

        /// <summary>
        /// Gets a list of queues to create with this GPU (TODO: As per user specification/script)
        /// </summary>
        /// <returns></returns>
        public unsafe DeviceQueueCreateInfo[] GetQueuesToCreate()
        {
            //TODO: Make scriptable
            
            DeviceQueueCreateInfo[] result = new DeviceQueueCreateInfo[3];

            var info = new DeviceQueueCreateInfo();
            info.SType = StructureType.DeviceQueueCreateInfo;
            info.QueueFamilyIndex = queueInfo.GetGeneralPurposeQueue(in Device)!.Value;
            info.QueueCount = 1;
            float queuePriority = 1f;
            info.PQueuePriorities = &queuePriority;

            result[0] = info;

            var info2 = new DeviceQueueCreateInfo();
            info2.SType = StructureType.DeviceQueueCreateInfo;
            info2.QueueFamilyIndex = queueInfo.GetTransferQueue(in Device)!.Value;
            info2.QueueCount = 1;
            info2.PQueuePriorities = &queuePriority;

            result[1] = info2;

            var info3 = new DeviceQueueCreateInfo();
            info3.SType = StructureType.DeviceQueueCreateInfo;
            info3.QueueFamilyIndex = queueInfo.GetComputeQueue(in Device)!.Value;
            info3.QueueCount = 1;
            info3.PQueuePriorities = &queuePriority;

            result[2] = info3;

            return result;
        }
        public void GetCreatedQueueIndices(Device createdDevice)
        {
            LogicalDevice = createdDevice;
            VkEngine.vk.GetDeviceQueue(LogicalDevice, queueInfo.GetGeneralPurposeQueue(in Device)!.Value, 0, out var allPurposeQueue);
            VkEngine.vk.GetDeviceQueue(LogicalDevice, queueInfo.GetTransferQueue(in Device)!.Value, 0, out var dedicatedTransferQueue);
            VkEngine.vk.GetDeviceQueue(LogicalDevice, queueInfo.GetComputeQueue(in Device)!.Value, 0, out var dedicatedComputeQueue);

            AllPurposeQueue = new VkCommandQueue(allPurposeQueue);
            DedicatedTransferQueue = new VkCommandQueue(dedicatedTransferQueue);
            DedicatedComputeQueue = new VkCommandQueue(dedicatedComputeQueue);
        }

        public unsafe static bool SelectGPU(out VkGPU GPU)
        {
            uint deviceCount = 0;
            VkEngine.vk.EnumeratePhysicalDevices(VkEngine.vkInstance, ref deviceCount, null);

            if (deviceCount == 0)
            {
                Debugger.Log("No GPU Found!");
                GPU = default;
                return false;
                //throw new InitializationException("No GPU found!");
            }

            PhysicalDevice[] devices = new PhysicalDevice[deviceCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                VkEngine.vk.EnumeratePhysicalDevices(VkEngine.vkInstance, ref deviceCount, devicesPtr);
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
                Debugger.Log("No GPU was selected!");
                GPU = default;
                return false;
                //throw new InitializationException("No GPU was selected!");
            }
            GPU = new VkGPU(devices[selectedIndex], "", selectedQueueProperties);//devices[selectedIndex];
            return true;
        }
        public unsafe static bool IsDeviceSuitable(in PhysicalDevice device, ref int score, out QueueProperties queueProperties)
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
        public unsafe static VkExtensionProperties[] SupportedDeviceExtensions(in PhysicalDevice device)
        {
            if (!VkEngine.initialized)
            {
                throw new InvalidOperationException("Cannot call VulkanGPUs.SupportedExtensions() before initializing Vulkan!");
            }
            uint supportedExtensionsCount = 0;
            VkEngine.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &supportedExtensionsCount, null);

            ExtensionProperties[] extensionProperties = new ExtensionProperties[supportedExtensionsCount];

            VkEngine.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &supportedExtensionsCount, extensionProperties.AsSpan());

            VkExtensionProperties[] result = new VkExtensionProperties[supportedExtensionsCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new VkExtensionProperties(extensionProperties[i]);
            }
            return result;
        }
        public unsafe static bool AllRequiredDeviceExtensionsSupported(in PhysicalDevice device)
        {
            var extensions = SupportedDeviceExtensions(in device);
            int totalSupported = 0;
            for (int i = 0; i < extensions.Length; i++)
            {
                for (int j = 0; j < VkEngine.requiredDeviceExtensions.Length; j++)
                {
                    if (VkEngine.requiredDeviceExtensions[j] == extensions[i].ExtensionName)
                    {
                        totalSupported++;
                        break;
                    }
                }
            }
            return totalSupported == VkEngine.requiredDeviceExtensions.Length;
        }
        public unsafe static QueueProperties GetQueueInfo(in PhysicalDevice device)
        {
            uint flagsCount = 0;
            VkEngine.vk.GetPhysicalDeviceQueueFamilyProperties(device, ref flagsCount, null);

            QueueFamilyProperties[] properties = new QueueFamilyProperties[flagsCount];
            fixed (QueueFamilyProperties* ptr = properties)
            {
                VkEngine.vk.GetPhysicalDeviceQueueFamilyProperties(device, ref flagsCount, ptr);
            }

            return new QueueProperties(properties);
        }
    }
}
#endif