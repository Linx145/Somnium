#if VULKAN
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Somnium.Framework.Vulkan
{
    public readonly struct VkRenderPassHash
    {
        public readonly Format imageFormat;
        public readonly DepthFormat depthFormat;
        public readonly ImageLayout finalLayout;
        public readonly bool mustClear;

        public VkRenderPassHash(Format imageFormat, DepthFormat depthFormat, ImageLayout finalLayout, bool mustClear)
        {
            this.imageFormat = imageFormat;
            this.depthFormat = depthFormat;
            this.finalLayout = finalLayout;
            this.mustClear = mustClear;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (int)imageFormat;
                hash = hash * 23 + (int)depthFormat;
                hash = hash * 23 + (int)finalLayout;
                hash *= mustClear ? -1 : 1;
                return hash;
            }
        }
    }
    public unsafe class VkRenderPass : IDisposable
    {
        public static Dictionary<VkRenderPassHash, VkRenderPass> renderPassCache = new Dictionary<VkRenderPassHash, VkRenderPass>();
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }

        public bool hasDepthStencil;
        public VkRenderPassHash hash;
        public ClearValue[] clearValues;
        RenderPass handle;
        RenderBuffer renderingToBuffer;
        private VkRenderPass(VkRenderPassHash hash)
        {
            this.hash = hash;
        }
        public static implicit operator RenderPass(VkRenderPass pass)
        {
            return pass.handle;
        }
        public bool begun { get; private set; }

        public static VkRenderPass GetOrCreate(Format imageFormat, ImageLayout finalLayout, DepthFormat depthFormat, bool mustClear)
        {
            VkRenderPassHash hash = new VkRenderPassHash(imageFormat, depthFormat, finalLayout, mustClear);
            if (renderPassCache.TryGetValue(hash, out var pass))
            {
                return pass;
            }
            pass = Create(imageFormat, finalLayout, depthFormat, mustClear);
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
        public static VkRenderPass Create(Format imageFormat, ImageLayout finalLayout, DepthFormat depthFormat = DepthFormat.Depth32, bool mustClear = false)
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
            colorAttachment.LoadOp = mustClear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load;
            colorAttachment.StoreOp = AttachmentStoreOp.Store;

            colorAttachment.InitialLayout = ImageLayout.ColorAttachmentOptimal;
            colorAttachment.FinalLayout = finalLayout;
        
            attachments[0] = colorAttachment;

            description.ColorAttachmentCount = 1;
            description.PColorAttachments = &colorAttachmentReference;
            #endregion

            #region color subpass
            SubpassDependency colorDependency = new SubpassDependency();
            colorDependency.SrcSubpass = Vk.SubpassExternal;
            colorDependency.DstSubpass = 0;

            colorDependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
            colorDependency.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;

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
                depthAttachment.LoadOp = mustClear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load;
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
                depthDependency.SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;

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

            VkRenderPass result = new VkRenderPass(new VkRenderPassHash(imageFormat, depthFormat, finalLayout, mustClear));
            result.handle = renderPass;
            result.hasDepthStencil = depthFormat != DepthFormat.None;

            return result;
        }
        public static void DisposeAll()
        {
            foreach (var value in renderPassCache.Values)
            {
                value.Dispose();
            }
            renderPassCache.Clear();
        }
        public const float OneOver255 = 1f / 255f;
        /// <summary>
        /// Begins the renderpass with data specified in arguments
        /// </summary>
        /// <param name="cmdBuffer"></param>
        /// <param name="swapchain"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Begin(CommandCollection cmdBuffer, SwapChain swapchain, RenderBuffer renderTarget = null, Color? clearColor = null)
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

            //Debugger.Log((renderTarget == null ? "Transitioning backbuffer " : "Transitioning rendertarget ") + imageToTransition.imageLayout.ToString());
            VkEngine.frames[VkEngine.window.frameNumber].commandBufferLock.EnterWriteLock();
            VkEngine.TransitionImageLayout(imageToTransition, ImageAspectFlags.ColorBit, ImageLayout.ColorAttachmentOptimal, VkEngine.commandBuffer);
            VkEngine.frames[VkEngine.window.frameNumber].commandBufferLock.ExitWriteLock();

            renderingToBuffer = renderTarget;
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
            if (clearColor == null)
            {
                beginInfo.ClearValueCount = 0;
                beginInfo.PClearValues = null;
            }
            else
            {
                if (clearValues == null)
                {
                    clearValues = new ClearValue[hasDepthStencil ? 2 : 1];
                }
                clearValues[0] = new ClearValue(new ClearColorValue(clearColor.Value.R * OneOver255, clearColor.Value.G * OneOver255, clearColor.Value.B * OneOver255, clearColor.Value.A * OneOver255));
                if (hasDepthStencil)
                {
                    clearValues[1] = new ClearValue(null, new ClearDepthStencilValue(1f, 0));
                }

                beginInfo.ClearValueCount = (uint)clearValues.Length;
                fixed (ClearValue* ptr = clearValues)
                {
                    beginInfo.PClearValues = ptr;
                }
            }
            //use inline for primary command buffers
            vk.CmdBeginRenderPass(new CommandBuffer(cmdBuffer.handle), in beginInfo, SubpassContents.Inline);
            imageToTransition.imageLayout = hash.finalLayout;
            begun = true;
        }
        public void End(CommandCollection cmdBuffer)
        {
            if (!begun)
            {
                throw new InvalidOperationException("Render pass not yet begun!");
            }
            var vkCommandBuffer = new CommandBuffer(cmdBuffer.handle);
            vk.CmdEndRenderPass(vkCommandBuffer);

            if (renderingToBuffer != null)
            {
                /*ImageMemoryBarrier barrier = new ImageMemoryBarrier();
                barrier.SType = StructureType.ImageMemoryBarrier;
                barrier.PNext = null;
                var srcStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                var dstStageMask = PipelineStageFlags.FragmentShaderBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                barrier.OldLayout = renderingToBuffer.backendTexture.imageLayout;
                barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;

                barrier.Image = new Image(renderingToBuffer.backendTexture.imageHandle);
                barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;// ImageAspectFlags.ColorBit;
                barrier.SubresourceRange.BaseMipLevel = 0;
                barrier.SubresourceRange.LevelCount = 1;
                barrier.SubresourceRange.BaseArrayLayer = 0;
                barrier.SubresourceRange.LayerCount = 1;

                VkEngine.vk.CmdPipelineBarrier(new CommandBuffer(cmdBuffer.handle), srcStageMask, dstStageMask, DependencyFlags.None, 0, null, 0, null, 1, &barrier);//.CmdPipelineBarrier(new CommandBuffer(VkEngine.commandBuffer.handle), PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.BottomOfPipeBit, DependencyFlags.None, null, null, span);

                renderingToBuffer.backendTexture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
                */

                /*ImageMemoryBarrier2 barrier = new ImageMemoryBarrier2();
                barrier.SType = StructureType.ImageMemoryBarrier2;
                barrier.SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit;
                barrier.SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit;
                barrier.DstStageMask = PipelineStageFlags2.FragmentShaderBit;
                barrier.DstAccessMask = AccessFlags2.ShaderReadBit;
                barrier.OldLayout = ImageLayout.ColorAttachmentOptimal;
                barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;

                barrier.Image = new Image(renderingToBuffer.backendTexture.imageHandle);
                barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;// ImageAspectFlags.ColorBit;
                barrier.SubresourceRange.BaseMipLevel = 0;
                barrier.SubresourceRange.LevelCount = 1;
                barrier.SubresourceRange.BaseArrayLayer = 0;
                barrier.SubresourceRange.LayerCount = 1;

                DependencyInfo dependencyInfo = new DependencyInfo();
                dependencyInfo.SType = StructureType.DependencyInfo;
                dependencyInfo.PNext = null;
                dependencyInfo.PBufferMemoryBarriers = null;
                dependencyInfo.PMemoryBarriers = null;
                dependencyInfo.PImageMemoryBarriers = &barrier;
                dependencyInfo.ImageMemoryBarrierCount = 1;

                VkEngine.vk.CmdPipelineBarrier2(new CommandBuffer(cmdBuffer.handle), in dependencyInfo);//.CmdPipelineBarrier(new CommandBuffer(VkEngine.commandBuffer.handle), PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.BottomOfPipeBit, DependencyFlags.None, null, null, span);
                */
                renderingToBuffer = null;
            }

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
#endif