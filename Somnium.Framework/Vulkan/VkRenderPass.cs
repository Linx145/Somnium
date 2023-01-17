using Silk.NET.Vulkan;
using System;
using Somnium.Framework.Windowing;

namespace Somnium.Framework.Vulkan
{
    /// <summary>
    /// A render pass is where rendering occurs. 
    /// </summary>
    public unsafe class VkRenderPass : IDisposable
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }

        RenderPass handle;
        private VkRenderPass()
        {

        }
        public static implicit operator RenderPass(VkRenderPass pass)
        {
            return pass.handle;
        }
        public RenderPassBeginInfo beginInfo;
        public bool begun { get; private set; }
        /// <summary>
        /// Creates a new renderpass for rendering into a framebuffer
        /// </summary>
        /// <param name="imageFormat">The image format for the render pass to handle. Corresponds to the image format of the swapchain that will be feeding this renderpass</param>
        /// <param name="imageLayout">The layout to change the image into when entering and exiting the renderpass</param>
        /// <returns></returns>
        /// <exception cref="InitializationException"></exception>
        public static VkRenderPass Create(Format imageFormat, ImageLayout imageLayout, AttachmentLoadOp loadOperation = AttachmentLoadOp.Clear, AttachmentStoreOp storeOperation = AttachmentStoreOp.Store, bool hasStencil = true)
        {
            AttachmentReference colorAttachmentReference = new AttachmentReference();
            colorAttachmentReference.Attachment = 0;
            colorAttachmentReference.Layout = imageLayout;

            AttachmentDescription colorAttachment = new AttachmentDescription();
            colorAttachment.Format = imageFormat;
            colorAttachment.Samples = SampleCountFlags.Count1Bit;
            colorAttachment.LoadOp = loadOperation;
            colorAttachment.StoreOp = storeOperation;
            
            if (hasStencil)
            {
                colorAttachment.StencilLoadOp = AttachmentLoadOp.Clear;
                colorAttachment.StencilStoreOp = storeOperation;
            }
            else
            {
                colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
                colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
            }
            colorAttachment.InitialLayout = ImageLayout.Undefined;
            colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

            SubpassDependency dependency = new SubpassDependency();
            dependency.SrcSubpass = Vk.SubpassExternal;
            dependency.DstSubpass = 0;

            dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
            dependency.SrcAccessMask = 0;

            dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
            dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;

            SubpassDescription description = new SubpassDescription();
            description.PipelineBindPoint = PipelineBindPoint.Graphics;
            description.ColorAttachmentCount = 1;
            description.PColorAttachments = &colorAttachmentReference;

            RenderPassCreateInfo createInfo = new RenderPassCreateInfo();
            createInfo.SType = StructureType.RenderPassCreateInfo;
            createInfo.DependencyCount = 1;
            createInfo.PDependencies = &dependency;
            createInfo.AttachmentCount = 1;
            createInfo.PAttachments = &colorAttachment;
            createInfo.SubpassCount = 1;
            createInfo.PSubpasses = &description;

            RenderPass renderPass;

            if (VkEngine.vk.CreateRenderPass(VkEngine.vkDevice, in createInfo, null, &renderPass) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Render Pass!");
            }

            VkRenderPass result = new VkRenderPass();
            result.handle = renderPass;

            return result;
        }
        /// <summary>
        /// Begins the render pass with data in beginInfo
        /// </summary>
        /// <param name="cmdBuffer"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Begin(CommandCollection cmdBuffer)
        {
            if (begun)
            {
                throw new InvalidOperationException("Vulkan render pass already began!");
            }
            //use inline for primary command buffers
            vk.CmdBeginRenderPass(new CommandBuffer(cmdBuffer.handle), in beginInfo, SubpassContents.Inline);

            begun = true;
        }
        /// <summary>
        /// Begins the renderpass with data specified in arguments
        /// </summary>
        /// <param name="cmdBuffer"></param>
        /// <param name="swapchain"></param>
        /// <param name="clearColor"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Begin(CommandCollection cmdBuffer, SwapChain swapchain, Color clearColor)
        {
            if (begun)
            {
                throw new InvalidOperationException("Vulkan render pass already began!");
            }
            ClearValue clearValue = new ClearValue(new ClearColorValue(clearColor.R / 255f, clearColor.G / 255f, clearColor.B / 255f, 1f));

            beginInfo = new RenderPassBeginInfo();
            beginInfo.SType = StructureType.RenderPassBeginInfo;
            beginInfo.RenderPass = handle;
            beginInfo.Framebuffer = swapchain.CurrentFramebuffer;
            beginInfo.RenderArea = new Rect2D(default, swapchain.imageExtents);
            beginInfo.ClearValueCount = 1;
            beginInfo.PClearValues = &clearValue;

            //use inline for primary command buffers
            vk.CmdBeginRenderPass(new CommandBuffer(cmdBuffer.handle), in beginInfo, SubpassContents.Inline);

            begun = true;
        }
        public void End(CommandCollection cmdBuffer)
        {
            if (!begun)
            {
                throw new InvalidOperationException("Render pass not yet begun!");
            }
            vk.CmdEndRenderPass(new CommandBuffer(cmdBuffer.handle));

            begun = false;
        }
        public void Dispose()
        {
            if (handle.Handle != 0)
            {
                VkEngine.vk.DestroyRenderPass(VkEngine.vkDevice, handle, null);
            }
        }
    }
}
