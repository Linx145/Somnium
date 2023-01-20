using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;

namespace Somnium.Framework
{
    public class RenderTarget2D : IDisposable
    {
        private readonly Application application;
        public readonly Texture2D backendTexture;
        public readonly DepthBuffer depthBuffer;
        public readonly uint width;
        public readonly uint height;

        public ulong framebufferHandle;

        public bool isDisposed { get; private set; } = false;
        public bool constructed { get; private set; } = false;
        public RenderTarget2D(Application application, Texture2D backendTexture, DepthBuffer depthBuffer)
        {
            this.depthBuffer = depthBuffer;
            this.application = application;
            this.backendTexture = backendTexture;
            this.width = backendTexture.Width;
            this.height = backendTexture.Height;

            Construct();
        }
        public RenderTarget2D(Application application, uint width, uint height, ImageFormat imageFormat, DepthFormat depthFormat)
        {
            this.application = application;
            this.width = width;
            this.height = height;

            backendTexture = new Texture2D(application, width, height, imageFormat, true);
            if (depthFormat != DepthFormat.None)
            {
                depthBuffer = new DepthBuffer(application, width, height, depthFormat);
            }

            Construct();
        }
        private void Construct()
        {
            if (constructed)
            {
                throw new InvalidOperationException("Render Target 2D already constructed ");
            }
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        FramebufferCreateInfo createInfo = new FramebufferCreateInfo();
                        createInfo.SType = StructureType.FramebufferCreateInfo;
                        createInfo.Width = width;
                        createInfo.Height = height;
                        createInfo.RenderPass = VkEngine.renderPass;
                        createInfo.Layers = 1;

                        uint attachmentCount = 1;
                        if (depthBuffer != null)
                        {
                            attachmentCount++;
                        }
                        ImageView* imageView = stackalloc ImageView[(int)attachmentCount];

                        uint attachmentIndex = 0;
                        imageView[attachmentIndex] = new ImageView(backendTexture.imageViewHandle);
                        attachmentIndex++;

                        if (depthBuffer != null)
                        {
                            imageView[attachmentIndex] = new ImageView(depthBuffer.imageViewHandle);
                            attachmentIndex++;
                        }

                        createInfo.AttachmentCount = attachmentCount;

                        createInfo.PAttachments = imageView;

                        Framebuffer frameBuffer;
                        if (VkEngine.vk.CreateFramebuffer(VkEngine.vkDevice, in createInfo, null, &frameBuffer) != Result.Success)
                        {
                            throw new InitializationException("Failed to create Vulkan Framebuffer!");
                        }
                        framebufferHandle = frameBuffer.Handle;
                    }
                    constructed = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            if (!isDisposed && constructed)
            {
                backendTexture?.Dispose();
                depthBuffer?.Dispose();
                if (framebufferHandle != 0)
                {
                    switch (application.runningBackend)
                    {
                        case Backends.Vulkan:
                            unsafe
                            {
                                VkEngine.vk.DestroyFramebuffer(VkEngine.vkDevice, new Framebuffer(framebufferHandle), null);
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                isDisposed = true;
            }
        }
    }
}