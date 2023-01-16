using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;

namespace Somnium.Framework
{
    public class RenderTarget2D : IDisposable
    {
        private readonly Application application;
        public readonly Texture2D backendTexture;
        public readonly uint width;
        public readonly uint height;

        public ulong framebufferHandle;

        public bool isDisposed { get; private set; } = false;
        public bool constructed { get; private set; } = false;
        public RenderTarget2D(Application application, Texture2D backendTexture)
        {
            this.application = application;
            this.backendTexture = backendTexture;
            this.width = backendTexture.Width;
            this.height = backendTexture.Height;

            Construct();
        }
        public RenderTarget2D(Application application, uint width, uint height, ImageFormat imageFormat)
        {
            this.application = application;
            this.width = width;
            this.height = height;

            backendTexture = new Texture2D(application, width, height, imageFormat);

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
                        createInfo.AttachmentCount = 1;
                        ImageView* imageView = stackalloc ImageView[] { new ImageView(backendTexture.imageViewHandle) };
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
                if (backendTexture != null && !backendTexture.isDisposed)
                {
                    backendTexture.Dispose();
                }
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