using Silk.NET.Vulkan;
using System;

namespace Somnium.Framework.Vulkan
{
    public class VkFramebuffer : IDisposable
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }

        public Framebuffer handle;
        public FramebufferCreateInfo createInfo;

        private VkFramebuffer()
        {

        }
        public static unsafe VkFramebuffer Create(ImageData image, RenderPass renderPass, uint width, uint height)
        {
            VkFramebuffer buffer = new VkFramebuffer();
            FramebufferCreateInfo createInfo = new FramebufferCreateInfo();
            createInfo.SType = StructureType.FramebufferCreateInfo;
            createInfo.RenderPass = renderPass;
            createInfo.AttachmentCount = 1;
            fixed (ImageView* ptr = &image.handle)
            {
                createInfo.PAttachments = ptr;
            }
            createInfo.Width = width;
            createInfo.Height = height;
            createInfo.Layers = 1;

            Framebuffer frameBuffer;
            if (vk.CreateFramebuffer(VkEngine.vkDevice, in createInfo, null, &frameBuffer) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Framebuffer!");
            }

            buffer.createInfo = createInfo;
            buffer.handle = frameBuffer;
            return buffer;
        }
        
        public static implicit operator Framebuffer(VkFramebuffer me)
        {
            return me.handle;
        }
        public unsafe void Dispose()
        {
            if (handle.Handle != 0)
            {
                vk.DestroyFramebuffer(VkEngine.vkDevice, handle, null);
                handle = default;
            }
        }
    }
}
