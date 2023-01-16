﻿using System;
using System.IO;
using StbImageSharp;
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;

namespace Somnium.Framework
{
    public class Texture2D : IDisposable
    {
        private readonly Application application;
        private byte[] data;
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public bool isDisposed { get; private set; }
        public bool constructed { get; private set; }
        /// <summary>
        /// Whether the Texture2D owns the image handle, and hence should destroy it when we are disposed.
        /// False in cases where the image is passed already-created into the texture2D from a third party/graphics API primitive object, such as a swapchain
        /// </summary>
        public bool imageBelongsToMe;

        public ulong imageHandle;
        public SamplerState samplerState;
        public readonly ImageFormat imageFormat;

        #region Vulkan
        public ulong imageViewHandle;
        public AllocatedMemoryRegion memoryRegion;
        #endregion

        public Texture2D(Application application, ulong fromExistingHandle, uint width, uint height, ImageFormat imageFormat, SamplerState samplerState = null, bool imageBelongsToMe = false)
        {
            this.imageFormat = imageFormat;
            this.Width = width;
            this.Height = height;
            this.application = application;
            imageHandle = fromExistingHandle;
            this.samplerState = samplerState;
            this.imageBelongsToMe = imageBelongsToMe;

            Construct();
        }
        public Texture2D(Application application, byte[] data, uint Width, uint Height, SamplerState samplerState, ImageFormat imageFormat)
        {
            this.imageFormat = imageFormat;
            this.application = application;
            this.data = data;
            this.Width = Width;
            this.Height = Height;
            this.samplerState = samplerState;
            imageBelongsToMe = true;

            Construct();
        }
        public Texture2D(Application application, uint Width, uint Height, ImageFormat imageFormat)
        {
            this.imageFormat = imageFormat;
            this.application = application;
            this.data = new byte[Width * Height * 4];
            this.Width = Width;
            this.Height = Height;
            this.samplerState = null;
            imageBelongsToMe = true;

            Construct();
        }

        public Span<byte> GetData()
        {
            return data.AsSpan();
        }
        public void SetColor(int colorIndex, Color color)
        {
            int size = 4;
            data[colorIndex * size] = color.R;
            data[colorIndex * size + 1] = color.G;
            data[colorIndex * size + 2] = color.B;
            data[colorIndex * size + 3] = color.A;
        }
        public void Construct()
        {
            if (constructed)
            {
                throw new InvalidOperationException("Image already constructed!");
            }
            
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        Image image;

                        if (imageHandle == 0) //this is true in cases where the images are passed in already created,  EG: From a swapchain
                        {
                            ImageCreateInfo createInfo = new ImageCreateInfo();
                            createInfo.SType = StructureType.ImageCreateInfo;
                            createInfo.ImageType = ImageType.Type2D;
                            createInfo.Extent = new Extent3D((uint)Width, (uint)Height, 1);
                            createInfo.MipLevels = 1;
                            createInfo.ArrayLayers = 1;
                            createInfo.Format = Converters.ImageFormatToVkFormat[(int)imageFormat];//Format.R8G8B8A8Unorm;
                            createInfo.Tiling = ImageTiling.Optimal;
                            //Undefined: transitioning to this image discards the pixels
                            //LayoutPreinitialized: transitioning to this image preserves the current pixels
                            createInfo.InitialLayout = ImageLayout.Undefined;
                            createInfo.Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit;
                            //Though we must share it between the transfer queue and the graphics queue, we won't be
                            //reading and writing to the image at the same time, hence set it to exclusive
                            createInfo.SharingMode = SharingMode.Exclusive;
                            createInfo.Samples = SampleCountFlags.Count1Bit;
                            createInfo.Flags = ImageCreateFlags.None;

                            if (VkEngine.vk.CreateImage(VkEngine.vkDevice, in createInfo, null, &image) != Result.Success)
                            {
                                throw new AssetCreationException("Failed to create Vulkan image handle!");
                            }

                            imageHandle = image.Handle;
                        }
                        image = new Image(imageHandle);

                        if (data != null && data.Length > 0 && imageBelongsToMe)
                        {
                            //set data into image using a staging buffer
                            memoryRegion = VkMemory.malloc(image, MemoryPropertyFlags.DeviceLocalBit);

                            var stagingBuffer = VkEngine.CreateResourceBuffer((ulong)(data.LongLength), BufferUsageFlags.TransferSrcBit);
                            var stagingMemoryRegion = VkMemory.malloc(stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                            byte* stagingData = stagingMemoryRegion.Bind<byte>();
                            //stagingMemoryRegion.Bind(&stagingData);
                            data.AsSpan().CopyTo(new Span<byte>(stagingData, data.Length));
                            stagingMemoryRegion.Unbind();

                            VkEngine.TransitionImageLayout(this, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
                            VkEngine.StaticCopyBufferToImage(stagingBuffer, this);

                            VkEngine.TransitionImageLayout(this, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

                            VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, stagingBuffer, null);
                            stagingMemoryRegion.Free();
                        }

                        //create image view
                        ImageViewCreateInfo viewInfo = new ImageViewCreateInfo();
                        viewInfo.SType = StructureType.ImageViewCreateInfo;
                        viewInfo.Image = image;
                        viewInfo.ViewType = ImageViewType.Type2D;
                        viewInfo.Format = Converters.ImageFormatToVkFormat[(int)imageFormat];//Format.R8G8B8A8Unorm; //todo: Make scriptable
                        viewInfo.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
                        viewInfo.SubresourceRange.BaseMipLevel = 0;
                        viewInfo.SubresourceRange.LevelCount = 1;
                        viewInfo.SubresourceRange.BaseArrayLayer = 0;
                        viewInfo.SubresourceRange.LayerCount = 1;

                        viewInfo.Components.R = ComponentSwizzle.Identity;
                        viewInfo.Components.G = ComponentSwizzle.Identity;
                        viewInfo.Components.B = ComponentSwizzle.Identity;
                        viewInfo.Components.A = ComponentSwizzle.Identity;

                        ImageView textureImageView;
                        if (VkEngine.vk.CreateImageView(VkEngine.vkDevice, &viewInfo, null, &textureImageView) != Result.Success)
                        {
                            throw new AssetCreationException("Failed to create view interface into Vulkan image!");
                        }
                        imageViewHandle = textureImageView.Handle;
                    }
                    break;
                default:
                    throw new Exception();
            }
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                data = null;
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            VkEngine.vk.DestroyImageView(VkEngine.vkDevice, new ImageView(imageViewHandle), null);
                            if (imageBelongsToMe)
                                VkEngine.vk.DestroyImage(VkEngine.vkDevice, new Image(imageHandle), null);
                            if (memoryRegion.IsValid)
                                memoryRegion.Free();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                GC.SuppressFinalize(this);
            }
        }

        #region static functions
        /// <summary>
        /// Loads a texture from the following formats: .png, .jpg, .bmp, .tga
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Texture2D FromStream(Application application, Stream stream, SamplerState samplerState, ImageFormat format = ImageFormat.R8G8B8A8Unorm)
        {
            ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            return new Texture2D(application, result.Data, (uint)result.Width, (uint)result.Height, samplerState, format);
        }
#endregion
    }
}
