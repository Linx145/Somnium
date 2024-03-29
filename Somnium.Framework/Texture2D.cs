﻿using System;
using System.IO;
using StbImageSharp;
#if VULKAN
using Somnium.Framework.Vulkan;
using Silk.NET.Vulkan;
#endif
#if WGPU
using Somnium.Framework.WGPU;
using Silk.NET.WebGPU;
#endif

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
        private bool storeData;
        /// <summary>
        /// Whether the Texture2D owns the image handle, and hence should destroy it when we are disposed.
        /// False in cases where the image is passed already-created into the texture2D from a third party/graphics API primitive object, such as a swapchain
        /// </summary>
        public bool imageBelongsToMe;
        public bool usedForRenderTarget;

        public ulong imageHandle;
        public ulong imageViewHandle;
        public SamplerState samplerState;
        public readonly ImageFormat imageFormat;

        #region Vulkan
#if VULKAN
        public ImageLayout imageLayout;
        public AllocatedMemoryRegion memoryRegion;
#endif
        #endregion

        public Texture2D(Application application, ulong fromExistingHandle, uint width, uint height, ImageFormat imageFormat, SamplerState samplerState = null, bool imageBelongsToMe = false, bool usedForRenderTarget = false)
        {
            this.imageFormat = imageFormat;
            this.Width = width;
            this.Height = height;
            this.application = application;
            imageHandle = fromExistingHandle;
            this.samplerState = samplerState;
            this.imageBelongsToMe = imageBelongsToMe;
            this.usedForRenderTarget = usedForRenderTarget;

            Construct();
        }
        public Texture2D(Application application, byte[] data, uint Width, uint Height, SamplerState samplerState, ImageFormat imageFormat, bool usedForRenderTarget = false, bool storeData = false)
        {
            this.imageFormat = imageFormat;
            this.application = application;
            this.data = data;
            this.Width = Width;
            this.Height = Height;
            this.samplerState = samplerState;
            imageBelongsToMe = true;
            this.usedForRenderTarget = usedForRenderTarget;
            this.storeData = storeData;

            Construct();
        }
        public Texture2D(Application application, uint Width, uint Height, ImageFormat imageFormat, bool usedForRenderTarget = false)
        {
            this.imageFormat = imageFormat;
            this.application = application;
            this.data = new byte[Width * Height * 4];
            this.Width = Width;
            this.Height = Height;
            this.samplerState = SamplerState.PointClamp;
            imageBelongsToMe = true;
            this.usedForRenderTarget = usedForRenderTarget;

            Construct();
        }

        /// <summary>
        /// Reads a texture's worth of images. Warning: Very slow, don't do this every frame
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public unsafe Span<T> GetData<T>() where T : unmanaged
        {
            if (!constructed)
            {
                throw new InvalidOperationException("Cannot retrieve data from texture2d that has yet to be constructed!");
            }
            //if our data array is empty or we are a render target, that means we need to update
            //our data array 
            if (usedForRenderTarget || data == null)
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        {
                            int expectedDataSize = (int)Width * (int)Height * sizeof(T);
                            if (data == null || data.Length < expectedDataSize)
                            {
                                data = new byte[expectedDataSize];
                            }

                            var stagingBuffer = VkEngine.CreateResourceBuffer((ulong)(data.LongLength), BufferUsageFlags.TransferDstBit);
                            var stagingMemoryRegion = VkMemory.malloc("Texture2D staging", stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                            var img = new Image(imageHandle);
                            var original = imageLayout;
                            VkEngine.TransitionImageLayout(img, ImageAspectFlags.ColorBit, original, ImageLayout.TransferSrcOptimal, null);
                            VkEngine.StaticCopyImageToBuffer(this, stagingBuffer);
                            VkEngine.TransitionImageLayout(img, ImageAspectFlags.ColorBit, ImageLayout.TransferSrcOptimal, original, null);

                            byte* stagingData = stagingMemoryRegion.Bind<byte>();
                            new Span<byte>(stagingData, data.Length).CopyTo(data);
                            stagingMemoryRegion.Unbind();

                            VkEngine.DestroyResourceBuffer(stagingBuffer);
                            stagingMemoryRegion.Free();
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
            int size = sizeof(T);
            fixed (void* ptr = &data[0])
            {
                return new Span<T>(ptr, data.Length / size);
            }
        }
        public void Construct()
        {
            if (constructed)
            {
                throw new InvalidOperationException("Image already constructed!");
            }
            
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        Image image;

                        if (imageHandle == 0) //this is false in cases where the images are passed in already created,  EG: From a swapchain
                        {
                            ImageCreateInfo createInfo = new ImageCreateInfo();
                            createInfo.SType = StructureType.ImageCreateInfo;
                            createInfo.ImageType = ImageType.Type2D;
                            createInfo.Extent = new Extent3D(Width, Height, 1);
                            createInfo.MipLevels = 1;
                            createInfo.ArrayLayers = 1;
                            createInfo.Format = Converters.ImageFormatToVkFormat[(int)imageFormat];//Format.R8G8B8A8Unorm;
                            createInfo.Tiling = ImageTiling.Optimal;
                            //Undefined: transitioning to this image discards the pixels
                            //Preinitialized: transitioning to this image preserves the current pixels
                            createInfo.InitialLayout = ImageLayout.Undefined;
                            if (usedForRenderTarget)
                            {
                                createInfo.Usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit;
                            }
                            else
                            {
                                createInfo.Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit;
                            }
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
                            memoryRegion = VkMemory.malloc("Texture2D", image, MemoryPropertyFlags.DeviceLocalBit);

                            if (!usedForRenderTarget)
                            {
                                var stagingBuffer = VkEngine.CreateResourceBuffer((ulong)(data.LongLength), BufferUsageFlags.TransferSrcBit);
                                var stagingMemoryRegion = VkMemory.malloc("Texture2D staging", stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

                                byte* stagingData = stagingMemoryRegion.Bind<byte>();
                                //stagingMemoryRegion.Bind(&stagingData);
                                data.AsSpan().CopyTo(new Span<byte>(stagingData, data.Length));
                                stagingMemoryRegion.Unbind();

                                VkEngine.TransitionImageLayout(image, ImageAspectFlags.ColorBit, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, null);
                                VkEngine.StaticCopyBufferToImage(stagingBuffer, this);
                                VkEngine.TransitionImageLayout(image, ImageAspectFlags.ColorBit, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, null);

                                imageLayout = ImageLayout.ShaderReadOnlyOptimal;

                                VkEngine.DestroyResourceBuffer(stagingBuffer);
                                //VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, stagingBuffer, null);
                                stagingMemoryRegion.Free();
                            }
                            /*else
                            {
                                VkEngine.TransitionImageLayout(new Image(imageHandle), ImageAspectFlags.ColorBit, ImageLayout.Undefined, ImageLayout.ShaderReadOnlyOptimal, new CommandBuffer(null));
                            }*/
                        }
                        else
                        {
                            VkEngine.TransitionImageLayout(image, ImageAspectFlags.ColorBit, ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal, null);

                            imageLayout = ImageLayout.ColorAttachmentOptimal;
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
#endif
#if WGPU
                case Backends.WebGPU:
                    unsafe
                    {
                        if (imageHandle == 0)
                        {
                            TextureFormat viewFormat = Converters.ImageFormatToWGPUFormat[(int)imageFormat];

                            var textureDescriptor = new TextureDescriptor()
                            {
                                Size = new Extent3D(Width, Height, 1),
                                Usage = TextureUsage.CopyDst | TextureUsage.TextureBinding,
                                MipLevelCount = 1,
                                SampleCount = 1,
                                Dimension = TextureDimension.TextureDimension2D,
                                Format = viewFormat,
                                ViewFormatCount = 1,
                                ViewFormats = &viewFormat
                            };

                            imageHandle = (ulong)WGPUEngine.wgpu.DeviceCreateTexture(WGPUEngine.device, &textureDescriptor);
                        }
                        Texture* image = (Texture*)imageHandle;

                        TextureViewDescriptor viewDescriptor = new TextureViewDescriptor()
                        {
                            Format = Converters.ImageFormatToWGPUFormat[(int)imageFormat],
                            Dimension = TextureViewDimension.TextureViewDimension2D,
                            Aspect = TextureAspect.All,
                            MipLevelCount = 1,
                            ArrayLayerCount = 1,
                            BaseArrayLayer = 0,
                            BaseMipLevel = 0
                        };

                        imageViewHandle = (ulong)WGPUEngine.wgpu.TextureCreateView(image, &viewDescriptor);

                        if (data != null && data.Length > 0 && imageBelongsToMe)
                        {
                            var commandEncoderDescriptor = new CommandEncoderDescriptor();

                            var commandEncoder = WGPUEngine.wgpu.DeviceCreateCommandEncoder(WGPUEngine.device, &commandEncoderDescriptor);

                            ImageCopyTexture imgCopyTexture = new ImageCopyTexture()
                            {
                                Texture = image,
                                Aspect = TextureAspect.All,
                                MipLevel = 0,
                                Origin = new Origin3D(0, 0, 0)
                            };
                            TextureDataLayout layout = new TextureDataLayout()
                            {
                                BytesPerRow = Converters.ImageFormatToBytes[(int)imageFormat] * Width,
                                RowsPerImage = Height
                            };
                            Extent3D extents = new Extent3D(Width, Height, 1);

                            fixed (void* ptr = data)
                            {
                                WGPUEngine.wgpu.QueueWriteTexture(WGPUEngine.queue, &imgCopyTexture, ptr, (nuint)data.Length, &layout, &extents);
                            }

                            var commandBuffer = WGPUEngine.wgpu.CommandEncoderFinish(commandEncoder, new CommandBufferDescriptor());
                            WGPUEngine.wgpu.QueueSubmit(WGPUEngine.queue, 1, &commandBuffer);
                        }
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
            constructed = true;
            if (!storeData)
                data = null;
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                data = null;
                switch (application.runningBackend)
                {
#if WGPU
                    case Backends.WebGPU:
                        unsafe
                        {
                            WGPUEngine.crab.TextureViewDrop((TextureView*)imageViewHandle);
                            WGPUEngine.crab.TextureDrop((Texture*)imageHandle);
                        }
                        break;
#endif
#if VULKAN
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
#endif
                    default:
                        throw new NotImplementedException();
                }
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        #region static functions
        /// <summary>
        /// Loads a texture from the following formats: .png, .jpg, .bmp, .tga
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Texture2D FromStream(Application application, Stream stream, SamplerState samplerState, ImageFormat format = ImageFormat.R8G8B8A8Unorm, bool storeData = false)
        {
            ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            return new Texture2D(application, result.Data, (uint)result.Width, (uint)result.Height, samplerState, format, false, storeData);
        }
        /// <summary>
        /// Loads a texture from the following formats: .png, .jpg, .bmp, .tga
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Texture2D FromFile(Application application, string fileName, SamplerState samplerState, ImageFormat format = ImageFormat.R8G8B8A8Unorm, bool storeData = false)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                return FromStream(application, fs, samplerState, format, storeData);
            }
        }
#endregion
    }
}
