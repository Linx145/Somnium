using Somnium.Framework.Vulkan;
using System;
using Silk.NET.Vulkan;

namespace Somnium.Framework
{
    public class DepthBuffer : IDisposable
    {
        public ulong handle;
        public readonly Application application;
        public readonly uint width;
        public readonly uint height;
        public readonly DepthFormat depthFormat;

        #region Vulkan
        public ulong imageViewHandle;
        public AllocatedMemoryRegion memoryRegion;
        #endregion

        public DepthBuffer(Application application, uint width, uint height, DepthFormat depthFormat)
        {
            this.application = application;
            this.width = width;
            this.height = height;
            this.depthFormat = depthFormat;

            Construct();
        }
        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        ImageCreateInfo createInfo = new ImageCreateInfo();
                        createInfo.SType = StructureType.ImageCreateInfo;
                        createInfo.ImageType = ImageType.Type2D;
                        createInfo.Extent = new Extent3D(width, height, 1);
                        createInfo.MipLevels = 1;
                        createInfo.ArrayLayers = 1;
                        createInfo.Format = Converters.DepthFormatToVkFormat[(int)depthFormat];//Format.R8G8B8A8Unorm;
                        createInfo.Tiling = ImageTiling.Optimal;
                        //Undefined: transitioning to this image discards the pixels
                        //LayoutPreinitialized: transitioning to this image preserves the current pixels
                        createInfo.InitialLayout = ImageLayout.Undefined;
                        createInfo.Usage = ImageUsageFlags.DepthStencilAttachmentBit;
                        /*if (usedForRenderTarget)
                        {
                            createInfo.Usage = createInfo.Usage | ImageUsageFlags.ColorAttachmentBit;
                        }*/
                        createInfo.SharingMode = SharingMode.Exclusive;
                        createInfo.Samples = SampleCountFlags.Count1Bit;
                        createInfo.Flags = ImageCreateFlags.None;

                        Image image;

                        if (VkEngine.vk.CreateImage(VkEngine.vkDevice, in createInfo, null, &image) != Result.Success)
                        {
                            throw new AssetCreationException("Failed to create Vulkan depth buffer(image) handle!");
                        }

                        handle = image.Handle;

                        memoryRegion = VkMemory.malloc(image, MemoryPropertyFlags.DeviceLocalBit);

                        ImageViewCreateInfo viewInfo = new ImageViewCreateInfo();
                        viewInfo.SType = StructureType.ImageViewCreateInfo;
                        viewInfo.Image = image;
                        viewInfo.ViewType = ImageViewType.Type2D;
                        viewInfo.Format = Converters.DepthFormatToVkFormat[(int)depthFormat];
                        viewInfo.SubresourceRange.AspectMask = ImageAspectFlags.DepthBit;
                        if (Converters.DepthFormatHasStencil(depthFormat))
                        {
                            viewInfo.SubresourceRange.AspectMask = viewInfo.SubresourceRange.AspectMask | ImageAspectFlags.StencilBit;
                        }
                        viewInfo.SubresourceRange.BaseMipLevel = 0;
                        viewInfo.SubresourceRange.LevelCount = 1;
                        viewInfo.SubresourceRange.BaseArrayLayer = 0;
                        viewInfo.SubresourceRange.LayerCount = 1;

                        ImageView depthImageView;
                        if (VkEngine.vk.CreateImageView(VkEngine.vkDevice, &viewInfo, null, &depthImageView) != Result.Success)
                        {
                            throw new AssetCreationException("Failed to create view interface into Vulkan depth buffer(image)!");
                        }
                        imageViewHandle = depthImageView.Handle;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        VkEngine.vk.DestroyImageView(VkEngine.vkDevice, new ImageView(imageViewHandle), null);
                        VkEngine.vk.DestroyImage(VkEngine.vkDevice, new Image(handle), null);
                        if (memoryRegion.isBound) memoryRegion.Unbind();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
