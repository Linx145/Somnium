using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public struct VulkanGPUInfo
    {
        public PhysicalDevice Device;
        public Device LogicalDevice;
        public QueueProperties queueInfo;
        public string Name;

        public Queue AllPurposeQueue;
        public Queue DedicatedGraphicsQueue;
        public Queue DedicatedComputeQueue;
        public Queue DedicatedTransferQueue;

        public VulkanGPUInfo(PhysicalDevice device, string name, QueueProperties queueInfo)
        {
            Device = device;
            this.Name = name;
            this.queueInfo = queueInfo;
            LogicalDevice = default;

            AllPurposeQueue = default;
            DedicatedGraphicsQueue = default;
            DedicatedComputeQueue = default;
            DedicatedTransferQueue = default;
        }

        /// <summary>
        /// Gets a list of queues to create with this GPU (TODO: As per user specification/script)
        /// </summary>
        /// <returns></returns>
        public unsafe DeviceQueueCreateInfo[] GetQueuesToCreate()
        {
            //TODO: Make scriptable
            
            DeviceQueueCreateInfo[] result = new DeviceQueueCreateInfo[1];

            var info = new DeviceQueueCreateInfo();
            info.SType = StructureType.DeviceQueueCreateInfo;
            info.QueueFamilyIndex = queueInfo.GetGeneralPurposeQueue(in Device)!.Value;
            info.QueueCount = 1;
            float queuePriority = 1f;
            info.PQueuePriorities = &queuePriority;

            result[0] = info;

            return result;
        }
        public void GetCreatedQueueIndices(Device createdDevice)
        {
            LogicalDevice = createdDevice;
            VulkanEngine.vk.GetDeviceQueue(LogicalDevice, queueInfo.GetGeneralPurposeQueue(in Device)!.Value, 0, out AllPurposeQueue);
        }
    }
}
