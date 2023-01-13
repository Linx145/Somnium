using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    public static unsafe class Utils
    {
        public static byte* StringToBytePtr(string str, out IntPtr ptr)
        {
            IntPtr intPtr = Marshal.StringToHGlobalAnsi(str);
            ptr = intPtr;
            return (byte*)intPtr;
        }
        public static byte** StringArrayToPointer(string[] strArray, out IntPtr ptr)
        {
            IntPtr intPtr = SilkMarshal.StringArrayToPtr(strArray);
            ptr = intPtr;
            return (byte**)intPtr;
        }
        public static uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties, VkGPU gpu)
        {
            PhysicalDeviceMemoryProperties memoryProperties;
            VkEngine.vk.GetPhysicalDeviceMemoryProperties(gpu.Device, &memoryProperties);

            for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
            {
                if (((typeFilter & (1 << i)) != 0) && ((memoryProperties.MemoryTypes[i].PropertyFlags & properties) != 0))
                {
                    return (uint)i;
                }
            }
            throw new InitializationException("Vulkan memory type not found!");
        }
    }
}
