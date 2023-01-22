using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class Graphics
    {
        public PipelineState currentPipeline { get; private set; }
        public readonly Application application;
        ulong[] noOffset = new ulong[1] { 0 };

        public Graphics(Application application)
        {
            this.application = application;
        }

        /// <summary>
        /// Alternate way to call <paramref name="pipelineState"/>.Begin().
        /// </summary>
        /// <param name="pipelineState"></param>
        /// <param name="clearColor"></param>
        /// <param name="renderTarget"></param>
        public void SetPipeline(PipelineState pipelineState, Color? clearColor, RenderBuffer? renderTarget = null)
        {
            pipelineState.Begin(clearColor, renderTarget);
            currentPipeline = pipelineState;
        }
        /// <summary>
        /// Syncs the local state of the uniform buffers with the shader
        /// </summary>
        public void ForceUpdateUniforms()
        {
            currentPipeline.ForceUpdateUniforms(RenderStage.Graphics);
        }
        public void SetInstanceBuffer(InstanceBuffer buffer, uint bindingPoint)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handles[application.Window.frameNumber]);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), bindingPoint, 1, &vkBuffer, noOffset.AsSpan());
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void SetVertexBuffer(VertexBuffer buffer, uint bindingPoint = 0)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindVertexBuffers(new CommandBuffer(VkEngine.commandBuffer.handle), bindingPoint, 1, &vkBuffer, noOffset.AsSpan());
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void SetIndexBuffer(IndexBuffer buffer)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        Buffer vkBuffer = new Buffer(buffer.handle);
                        fixed (ulong* ptr = noOffset)
                        {
                            VkEngine.vk.CmdBindIndexBuffer(new CommandBuffer(VkEngine.commandBuffer.handle), vkBuffer, 0, IndexType.Uint16);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void DrawPrimitives(uint vertexCount, uint instanceCount, uint firstVertex = 0, uint firstInstance = 0)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        ResetPipelineShaders();
                        VkEngine.vk.CmdDraw(new CommandBuffer(VkEngine.commandBuffer.handle), vertexCount, instanceCount, firstVertex, firstInstance);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void DrawIndexedPrimitives(uint indexCount, uint instanceCount, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        {
            unsafe
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        ResetPipelineShaders();
                        VkEngine.vk.CmdDrawIndexed(new CommandBuffer(VkEngine.commandBuffer.handle), indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void ResetPipelineShaders()
        {
            bool updateUniforms = false;
            for (int i = 0; i < currentPipeline.shaders.Length; i++)
            {
                if (currentPipeline.shaders[i].uniformHasBeenSet)
                {
                    updateUniforms = true;
                }
                currentPipeline.shaders[i].uniformHasBeenSet = false;
            }
            if (updateUniforms) ForceUpdateUniforms();

            //need to update using the old descriptorForThisDrawCall state
            for (int i = 0; i < currentPipeline.shaders.Length; i++)
            {
                currentPipeline.shaders[i].descriptorForThisDrawCall++;
            }
        }
    }
}