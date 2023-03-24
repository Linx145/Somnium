using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Somnium.Framework.Vulkan
{
    /// <summary>
    /// A render pass is where rendering occurs. 
    /// </summary>
    public unsafe class VkRenderPass : IDisposable
    {
        public static Dictionary<uint, VkRenderPass> renderPassCache = new Dictionary<uint, VkRenderPass>();
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }

        public bool hasDepthStencil;
        public uint hash;
        RenderPass handle;
        public ImageLayout finalLayout;
        private VkRenderPass(uint hash)
        {
            this.hash = hash;
        }
        public static implicit operator RenderPass(VkRenderPass pass)
        {
            return pass.handle;
        }
        public bool begun { get; private set; }

        public static VkRenderPass GetOrCreate(Format imageFormat, ImageLayout finalLayout, DepthFormat depthFormat)
        {
            uint hash = GetKey(imageFormat, depthFormat, finalLayout);
            if (renderPassCache.TryGetValue(hash, out var pass))
            {
                return pass;
            }
            pass = Create(imageFormat, finalLayout, depthFormat);
            renderPassCache.Add(hash, pass);
            return pass;
        }
        /// <summary>
        /// Creates a new renderpass for rendering into a framebuffer
        /// </summary>
        /// <param name="imageFormat">The image format for the render pass to handle. Corresponds to the image format of the swapchain that will be feeding this renderpass</param>
        /// <param name="finalLayout">The layout to change the image into when entering and exiting the renderpass</param>
        /// <returns></returns>
        /// <exception cref="InitializationException"></exception>
        public static VkRenderPass Create(Format imageFormat, ImageLayout finalLayout, DepthFormat depthFormat = DepthFormat.Depth32)
        {
            uint attachmentCount = 1;
            if (depthFormat != DepthFormat.None)
            {
                attachmentCount++;
            }
            var attachments = stackalloc AttachmentDescription[(int)attachmentCount];
            var subpasses = stackalloc SubpassDependency[(int)attachmentCount];

            //subpass
            SubpassDescription description = new SubpassDescription();
            description.PipelineBindPoint = PipelineBindPoint.Graphics;

            #region color attachment description and reference
            AttachmentReference colorAttachmentReference = new AttachmentReference();
            colorAttachmentReference.Attachment = 0;
            colorAttachmentReference.Layout = ImageLayout.ColorAttachmentOptimal;

            AttachmentDescription colorAttachment = new AttachmentDescription();
            colorAttachment.Format = imageFormat;
            colorAttachment.Samples = SampleCountFlags.Count1Bit;
            colorAttachment.LoadOp = AttachmentLoadOp.Load;
            colorAttachment.StoreOp = AttachmentStoreOp.Store;

            //expects an image to be in layout ColorAttachmentOptimal on entering the render pass.
            //However, our images are in ShaderReadOnlyOptimal so they can be read by the shader.
            //why are our images entering the render pass?
            //The only image entering our render pass should be the swapchain image right?

            //however, after rendering once, the swapchain image would have been in PresentSrcKhr
            //so we will need to transition the image back
            //into color attachment optimal since we are not (and cannot be) using Undefined
            colorAttachment.InitialLayout = ImageLayout.ColorAttachmentOptimal;
            colorAttachment.FinalLayout = finalLayout;//ImageLayout.ColorAttachmentOptimal;
        
            attachments[0] = colorAttachment;

            description.ColorAttachmentCount = 1;
            description.PColorAttachments = &colorAttachmentReference;
            #endregion

            #region color subpass
            SubpassDependency colorDependency = new SubpassDependency();
            colorDependency.SrcSubpass = Vk.SubpassExternal;
            colorDependency.DstSubpass = 0;

            colorDependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
            colorDependency.SrcAccessMask = 0;

            colorDependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
            colorDependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;

            subpasses[0] = colorDependency;
            #endregion

            if (depthFormat != DepthFormat.None)
            {
                #region depth attachment description and reference
                AttachmentDescription depthAttachment;
                AttachmentReference depthAttachmentReference;

                depthAttachmentReference = new AttachmentReference();
                depthAttachmentReference.Attachment = 1;
                depthAttachmentReference.Layout = ImageLayout.DepthStencilAttachmentOptimal;

                depthAttachment = new AttachmentDescription();
                depthAttachment.Format = Converters.DepthFormatToVkFormat[(int)depthFormat];
                depthAttachment.Flags = AttachmentDescriptionFlags.None;
                depthAttachment.Samples = SampleCountFlags.Count1Bit;
                depthAttachment.LoadOp = AttachmentLoadOp.Load;
                depthAttachment.StoreOp = AttachmentStoreOp.Store;
                depthAttachment.StencilLoadOp = AttachmentLoadOp.Clear;
                if (Converters.DepthFormatHasStencil(depthFormat))
                {
                    depthAttachment.StencilStoreOp = AttachmentStoreOp.Store;
                }
                else depthAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
                //if (loadOperation == AttachmentLoadOp.Load)
                //{
                    depthAttachment.InitialLayout = ImageLayout.DepthStencilAttachmentOptimal;
                //}
                //else depthAttachment.InitialLayout = ImageLayout.Undefined;
                depthAttachment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

                description.PDepthStencilAttachment = &depthAttachmentReference;

                attachments[1] = depthAttachment;
                #endregion

                #region depth subpass
                //ensure that the frames are not writing to the depth buffer simultaneously using
                //an additional subpass
                SubpassDependency depthDependency = new SubpassDependency();
                depthDependency.SrcSubpass = Vk.SubpassExternal;
                depthDependency.DstSubpass = 0;

                depthDependency.SrcStageMask = PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;
                depthDependency.SrcAccessMask = 0;

                depthDependency.DstStageMask = PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;
                depthDependency.DstAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;

                subpasses[1] = depthDependency;
                #endregion
            }

            RenderPassCreateInfo createInfo = new RenderPassCreateInfo();
            createInfo.SType = StructureType.RenderPassCreateInfo;
            createInfo.DependencyCount = attachmentCount;
            createInfo.PDependencies = subpasses;
            createInfo.AttachmentCount = attachmentCount;
            createInfo.PAttachments = attachments;
            createInfo.SubpassCount = 1;
            createInfo.PSubpasses = &description;

            RenderPass renderPass;

            if (VkEngine.vk.CreateRenderPass(VkEngine.vkDevice, in createInfo, null, &renderPass) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Render Pass!");
            }

            VkRenderPass result = new VkRenderPass(GetKey(imageFormat, depthFormat, finalLayout));
            result.handle = renderPass;
            result.hasDepthStencil = depthFormat != DepthFormat.None;
            result.finalLayout = finalLayout;

            return result;
        }

        public static uint GetKey(Format imageFormat, DepthFormat depthFormat, ImageLayout finalImageLayout)
        {
            unchecked
            {
                uint hash = 17;
                hash = hash * 23 + (uint)imageFormat;
                hash = hash * 23 + (uint)depthFormat;
                hash = hash * 23 + (uint)finalImageLayout;
                return hash;
            }
        }
        public static void DisposeAll()
        {
            foreach (var value in renderPassCache.Values)
            {
                value.Dispose();
            }
            renderPassCache.Clear();
        }
        /// <summary>
        /// Begins the renderpass with data specified in arguments
        /// </summary>
        /// <param name="cmdBuffer"></param>
        /// <param name="swapchain"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Begin(CommandCollection cmdBuffer, SwapChain swapchain, RenderBuffer renderTarget = null)
        {
            if (begun)
            {
                throw new InvalidOperationException("Vulkan render pass already began!");
            }

            RenderPassBeginInfo beginInfo = new RenderPassBeginInfo();
            beginInfo.SType = StructureType.RenderPassBeginInfo;
            beginInfo.RenderPass = handle;
            //we need to do this for every time we begin the command buffer as the
            //image that we output (and thus the next call's input) will be in the wrong format
            //var imageToTransition = renderTarget == null ? swapchain.images[swapchain.currentImageIndex] : new Image(renderTarget.backendTexture.imageHandle);
            Texture2D imageToTransition = renderTarget == null ? swapchain.renderTargets[swapchain.currentImageIndex].backendTexture : renderTarget.backendTexture;

            VkEngine.TransitionImageLayout(imageToTransition, ImageAspectFlags.ColorBit, ImageLayout.ColorAttachmentOptimal, VkEngine.commandBuffer);

            if (renderTarget == null)
            {
                beginInfo.Framebuffer = swapchain.CurrentFramebuffer;
                beginInfo.RenderArea = new Rect2D(default, swapchain.imageExtents);
            }
            else
            {
                beginInfo.Framebuffer = new Framebuffer(renderTarget.framebufferHandle);
                beginInfo.RenderArea = new Rect2D(default, new Extent2D(renderTarget.width, renderTarget.height));
            }
            beginInfo.ClearValueCount = 0;
            beginInfo.PClearValues = null;
            //use inline for primary command buffers
            vk.CmdBeginRenderPass(new CommandBuffer(cmdBuffer.handle), in beginInfo, SubpassContents.Inline);
            imageToTransition.imageLayout = finalLayout;
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
        public override int GetHashCode()
        {
            unchecked
            {
                return (int)hash;
            }
        }
    }
}