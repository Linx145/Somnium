using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Somnium.Framework.Vulkan
{
    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] supportedSurfaceFormats;
        public PresentModeKHR[] supportedPresentModes;
    }
    public unsafe class SwapChain
    {
        private static Vk vk
        {
            get
            {
                return VulkanEngine.vk;
            }
        }
        public static KhrSurface khr
        {
            get
            {
                return VulkanEngine.KhrSurfaceAPI;
            }
        }
        public static SurfaceFormatKHR FindSurfaceWith(ColorSpaceKHR colorSpace, Format format, SurfaceFormatKHR[] toChooseFrom)
        {
            for (int i = 0; i < toChooseFrom.Length; i++)
            {
                if (toChooseFrom[i].ColorSpace == colorSpace && toChooseFrom[i].Format == format)
                {
                    return toChooseFrom[i];
                }
            }
            return toChooseFrom[0];
        }
        public static SwapChainSupportDetails QuerySwapChainSupport(in PhysicalDevice device)
        {
            SwapChainSupportDetails details = new SwapChainSupportDetails();

            khr.GetPhysicalDeviceSurfaceCapabilities(device, VulkanEngine.WindowSurface, &details.Capabilities);

            uint supportedSurfaceFormats;
            khr.GetPhysicalDeviceSurfaceFormats(device, VulkanEngine.WindowSurface, &supportedSurfaceFormats, null);

            details.supportedSurfaceFormats = new SurfaceFormatKHR[supportedSurfaceFormats];
            fixed (SurfaceFormatKHR* ptr = details.supportedSurfaceFormats)
            {
                khr.GetPhysicalDeviceSurfaceFormats(device, VulkanEngine.WindowSurface, &supportedSurfaceFormats, ptr);
            }

            uint supportedPresentModes;
            khr.GetPhysicalDeviceSurfacePresentModes(device, VulkanEngine.WindowSurface, &supportedPresentModes, null);

            details.supportedPresentModes = new PresentModeKHR[supportedPresentModes];
            fixed (PresentModeKHR* ptr = details.supportedPresentModes)
            {
                khr.GetPhysicalDeviceSurfacePresentModes(device, VulkanEngine.WindowSurface, &supportedPresentModes, ptr);
            }
            return details;
        }
    }
}
