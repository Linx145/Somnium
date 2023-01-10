using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public unsafe class ImageData : IDisposable
    {
        public ImageView handle;
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }
        private static Device device
        {
            get
            {
                return VkEngine.vkDevice;
            }
        }

        public static ImageData Create(Image image, Format imageFormat, ImageViewType viewType = ImageViewType.Type2D, ImageAspectFlags imagePurpose = ImageAspectFlags.ColorBit)
        {
            ImageViewCreateInfo createInfo = new ImageViewCreateInfo();
            createInfo.SType = StructureType.ImageViewCreateInfo;
            createInfo.Image = image;
            createInfo.ViewType = viewType;
            createInfo.Format = imageFormat;
            //RGBA map
            createInfo.Components.R = ComponentSwizzle.Identity;
            createInfo.Components.G = ComponentSwizzle.Identity;
            createInfo.Components.B = ComponentSwizzle.Identity;
            createInfo.Components.A = ComponentSwizzle.Identity;

            //The subresourceRange field describes what the image's purpose is and which part of
            //the image should be accessed. Our images will be used as color targets
            //without any mipmapping levels or multiple layers.
            createInfo.SubresourceRange.AspectMask = imagePurpose;
            createInfo.SubresourceRange.BaseMipLevel = 0;
            createInfo.SubresourceRange.LevelCount = 1;
            createInfo.SubresourceRange.BaseArrayLayer = 0;
            createInfo.SubresourceRange.LayerCount = 1;

            ImageData imageData = new ImageData();
            ImageView imageView;
            Result result = vk.CreateImageView(device, in createInfo, null, out imageView);
            if (result != Result.Success)
            {
                throw new InitializationException("Error creating Vulkan image view!");
            }
            imageData.handle = imageView;
            return imageData;
        }
        public void Dispose()
        {
            vk.DestroyImageView(device, handle, null);
        }
    }
}
