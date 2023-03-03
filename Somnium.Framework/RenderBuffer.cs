using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;

namespace Somnium.Framework
{
    public class RenderBuffer : IDisposable
    {
        private readonly Application application;
        public readonly Texture2D backendTexture;
        public readonly DepthBuffer depthBuffer;
        public readonly uint width;
        public readonly uint height;
        public readonly bool isBackbuffer;

        /// <summary>
        /// Whether the renderbuffer has been cleared and/or drawn to before. A renderbuffer that
        /// has not been used before should not be drawn, if not an error will arise. 
        /// It is up to your engine to enforce this error, if not your backend will do it for you.
        /// </summary>
        public bool hasBeenUsedBefore { get; internal set; }

        public ulong framebufferHandle;

        public bool isDisposed { get; private set; } = false;
        public bool constructed { get; private set; } = false;
        internal RenderBuffer(Application application, Texture2D backendTexture, DepthBuffer depthBuffer, bool isBackbuffer)
        {
            this.depthBuffer = depthBuffer;
            this.application = application;
            this.backendTexture = backendTexture;
            this.width = backendTexture.Width;
            this.height = backendTexture.Height;
            this.isBackbuffer = isBackbuffer;

            Construct();
        }
        public RenderBuffer(Application application, Texture2D backendTexture, DepthBuffer depthBuffer)
        {
            this.depthBuffer = depthBuffer;
            this.application = application;
            this.backendTexture = backendTexture;
            this.width = backendTexture.Width;
            this.height = backendTexture.Height;

            Construct();
        }
        public RenderBuffer(Application application, uint width, uint height, ImageFormat imageFormat, DepthFormat depthFormat)
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

                        if (isBackbuffer)
                        {
                            createInfo.RenderPass = VkEngine.GetRenderPass(null);
                        }
                        else createInfo.RenderPass = VkEngine.GetRenderPass(this);

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