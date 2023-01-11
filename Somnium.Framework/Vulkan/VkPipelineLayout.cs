using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VkPipelineLayout
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }
        public static PipelineLayout Create()
        {
            PipelineLayoutCreateInfo createInfo = new PipelineLayoutCreateInfo();
            createInfo.SType = StructureType.PipelineLayoutCreateInfo;
            createInfo.Flags = PipelineLayoutCreateFlags.None;
            createInfo.SetLayoutCount = 0;
            createInfo.PSetLayouts = null;
            createInfo.PushConstantRangeCount = 0;
            createInfo.PPushConstantRanges = null;

            PipelineLayout layout;

            Result result = vk.CreatePipelineLayout(VkEngine.vkDevice, in createInfo, null, &layout);
            if (result != Result.Success)
            {
                throw new InitializationException("Failed to initialize Vulkan pipeline layout!");
            }
            return layout;
        }
    }
}
