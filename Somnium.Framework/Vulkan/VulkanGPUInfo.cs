using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework.Vulkan
{
    public struct VulkanGPUInfo
    {
        public PhysicalDevice Device;
        public QueueProperties queueInfo;
        public string Name;

        public VulkanGPUInfo(PhysicalDevice device, string name, QueueProperties queueInfo)
        {
            Device = device;
            this.Name = name;
            this.queueInfo = queueInfo;
        }
    }
}
