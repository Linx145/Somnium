using Silk.NET.Core;
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
    public unsafe class SwapChain : IDisposable
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }
        public static Device device
        {
            get
            {
                return VkEngine.vkDevice;
            }
        }
        private static KhrSurface surfaceAPI
        {
            get
            {
                return VkEngine.KhrSurfaceAPI;
            }
        }
        private static KhrSwapchain swapchainAPI
        {
            get
            {
                return VkEngine.KhrSwapchainAPI;
            }
        }
        public SwapchainKHR handle;

        public uint minImageCount;
        public Format imageFormat;
        public ColorSpaceKHR imageColorSpace;
        public Extent2D imageExtents;
        public uint imageArrayLayers;
        public ImageUsageFlags imageUsage;
        public SurfaceKHR windowSurface;
        public PresentModeKHR presentMode;

        public uint currentImageIndex;
        public Image[] images;
        public ImageData[] imageDatas;
        public VkFramebuffer[] imageFrameBuffers;

        public uint ImageCount
        {
            get
            {
                uint imageCount = 0;
                swapchainAPI.GetSwapchainImages(device, handle, ref imageCount, null);
                return imageCount;
            }
        }
        public VkFramebuffer CurrentFramebuffer
        {
            get
            {
                return imageFrameBuffers[currentImageIndex];
            }
        }

        //todo: implement
        /*public static SwapChain Create(
            uint minImageCount,
            Format imageFormat,
            ColorSpaceKHR imageColorSpace,
            Extent2D imageExtents,
            PresentModeKHR presentMode,
            SurfaceKHR windowSurface,
            uint imageArrayLayers = 1,
            ImageUsageFlags imageUsage = ImageUsageFlags.ColorAttachmentBit)
        {
            SwapChain swapChain = new SwapChain(minImageCount, imageFormat, imageColorSpace, imageExtents, presentMode, windowSurface, imageArrayLayers, imageUsage);
        }*/

        public SwapChain(
            uint minImageCount, 
            Format imageFormat,
            ColorSpaceKHR imageColorSpace, 
            Extent2D imageExtents,
            PresentModeKHR presentMode,
            SurfaceKHR windowSurface,
            uint imageArrayLayers = 1,
            ImageUsageFlags imageUsage = ImageUsageFlags.ColorAttachmentBit)
        {
            this.minImageCount = minImageCount;
            this.imageFormat = imageFormat;
            this.imageColorSpace = imageColorSpace;
            this.imageExtents = imageExtents;
            this.presentMode = presentMode;
            this.windowSurface = windowSurface;
            this.imageArrayLayers = imageArrayLayers;
            this.imageUsage = imageUsage;
        }

        public void SwapBuffers(Silk.NET.Vulkan.Semaphore semaphore, Fence fence)
        {
            if (swapchainAPI.AcquireNextImage(device, handle, 1000000000, semaphore, fence, ref currentImageIndex) != Result.Success)
            {
                throw new ExecutionException("Failed to acquire new Vulkan Swapchain Image!");
            }
        }

        /// <summary>
        /// Compiles this class's data into a SwapchainCreateInfoKHR
        /// </summary>
        /// <param name="supportDetails"></param>
        /// <returns></returns>
        public SwapchainCreateInfoKHR GetCreateInfo(SwapChainSupportDetails supportDetails)
        {
            SwapchainCreateInfoKHR createInfo = new SwapchainCreateInfoKHR();
            createInfo.SType = StructureType.SwapchainCreateInfoKhr;
            createInfo.Surface = windowSurface;
            createInfo.MinImageCount = minImageCount;
            createInfo.ImageFormat = imageFormat;
            createInfo.ImageColorSpace = imageColorSpace;
            createInfo.ImageExtent = imageExtents;
            createInfo.ImageArrayLayers = 1;
            createInfo.ImageUsage = ImageUsageFlags.ColorAttachmentBit;

            if (VkEngine.CurrentGPU.DedicatedTransferQueue.Handle != 0)
            {
                throw new NotImplementedException();
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
                createInfo.QueueFamilyIndexCount = 0;
                createInfo.PQueueFamilyIndices = null;
            }

            //no need to rotate the swapchain
            createInfo.PreTransform = supportDetails.Capabilities.CurrentTransform;
            //no need transparent window backgrounds
            createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;

            createInfo.PresentMode = presentMode;
            //Enable clipping so we don't care what pixels are being obscured if there is another window in front of us
            createInfo.Clipped = new Bool32(true);

            //TODO
            createInfo.OldSwapchain = default;

            return createInfo;
        }

        public void Recreate(SwapChainSupportDetails supportDetails)
        {
            if (handle.Handle != 0)
            {
                swapchainAPI.DestroySwapchain(device, handle, null);
            }

            var info = GetCreateInfo(supportDetails);
            swapchainAPI.CreateSwapchain(device, in info, null, out handle);

            uint imageCount = 0;
            swapchainAPI.GetSwapchainImages(device, handle, ref imageCount, null);

            images = new Image[imageCount];
            fixed (Image* ptr = images)
            {
                swapchainAPI.GetSwapchainImages(device, handle, ref imageCount, ptr);
            }
            imageDatas = new ImageData[imageCount];
            for (int i = 0; i < imageDatas.Length; i++)
            {
                imageDatas[i] = ImageData.Create(images[i], imageFormat);
            }
        }
        public unsafe void RecreateFramebuffers(RenderPass renderPass)
        {
            if (imageFrameBuffers != null)
            {
                for (int i = 0; i < imageFrameBuffers.Length; i++)
                {
                    imageFrameBuffers[i].Dispose();
                }
            }

            imageFrameBuffers = new VkFramebuffer[ImageCount];
            for (int i = 0; i < imageFrameBuffers.Length; i++)
            {
                imageFrameBuffers[i] = VkFramebuffer.Create(imageDatas[i], renderPass, imageExtents.Width, imageExtents.Height);
            }
        }

        public void Dispose()
        {
            if (handle.Handle != 0)
            {
                swapchainAPI.DestroySwapchain(device, handle, null);
            }
            for (int i = 0; i < imageFrameBuffers.Length; i++)
            {
                imageFrameBuffers[i].Dispose();
            }
            for (int i = 0; i < imageDatas.Length; i++)
            {
                imageDatas[i].Dispose();
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

            surfaceAPI.GetPhysicalDeviceSurfaceCapabilities(device, VkEngine.WindowSurface, &details.Capabilities);

            uint supportedSurfaceFormats;
            surfaceAPI.GetPhysicalDeviceSurfaceFormats(device, VkEngine.WindowSurface, &supportedSurfaceFormats, null);

            details.supportedSurfaceFormats = new SurfaceFormatKHR[supportedSurfaceFormats];
            fixed (SurfaceFormatKHR* ptr = details.supportedSurfaceFormats)
            {
                surfaceAPI.GetPhysicalDeviceSurfaceFormats(device, VkEngine.WindowSurface, &supportedSurfaceFormats, ptr);
            }

            uint supportedPresentModes;
            surfaceAPI.GetPhysicalDeviceSurfacePresentModes(device, VkEngine.WindowSurface, &supportedPresentModes, null);

            details.supportedPresentModes = new PresentModeKHR[supportedPresentModes];
            fixed (PresentModeKHR* ptr = details.supportedPresentModes)
            {
                surfaceAPI.GetPhysicalDeviceSurfacePresentModes(device, VkEngine.WindowSurface, &supportedPresentModes, ptr);
            }
            return details;
        }
    }
}
