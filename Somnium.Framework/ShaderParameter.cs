using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using StbImageSharp;
using System;
using System.Collections.Generic;

using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class ShaderParameterCollection : IDisposable
    {
        public int Count => parameters.Count;
        private Dictionary<string, int> map;
        private UnorderedList<ShaderParameter> parameters;
        public readonly Shader shader;
        private readonly Application application;
        public readonly ShaderType shaderType;
        public bool constructed { get; private set; } = false;

        public ShaderParameterCollection(Application application, Shader shader, ShaderType shaderType)
        {
            this.application = application;
            this.shader = shader;
            this.shaderType = shaderType;
            map = new Dictionary<string, int>();
            parameters = new UnorderedList<ShaderParameter>();
        }
        /// <summary>
        /// Adds a parameter to the shader to be constructed.
        /// </summary>
        /// <param name="param"></param>
        private void AddParameter(ShaderParameter param)
        {
            if (constructed) throw new InvalidOperationException("Attempting to add parameter to shader parameter collection that has already been built!");
            map.Add(param.name, parameters.Count);
            parameters.Add(param);
        }
        /// <summary>
        /// Adds a parameter to the shader to be constructed.
        /// </summary>
        /// <param name="param"></param>
        public void AddParameter(string name, uint binding, UniformType type, uint size, uint arrayLength = 1)
        {
            ShaderParameter param = new ShaderParameter(shader, name, binding, type, size, arrayLength);
            AddParameter(param);
        }
        /// <summary>
        /// Adds a parameter to the shader.
        /// </summary>
        public void AddParameter<T>(string name, uint binding, UniformType type = UniformType.uniformBuffer, uint arrayLength = 1) where T : unmanaged
        {
            unsafe
            {
                AddParameter(name, binding, type, (uint)sizeof(T), arrayLength);
            }
        }
        public void AddTexture2DParameter(string name, uint binding, uint arrayLength = 1)
        {
            ShaderParameter param = new ShaderParameter(shader, name, binding, UniformType.imageAndSampler, 0, arrayLength);
            AddParameter(param);
            //param.offset = maxWidth;
            //maxWidth += param.size;
        }
        public ReadOnlySpan<ShaderParameter> GetParameters() => parameters.AsReadonlySpan();
        public bool HasUniform(string paramName) => map.ContainsKey(paramName);
        public bool Set<T>(string paramName, T value) where T : unmanaged
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, value);
                return true;
            }
            else return false;
        }
        public bool Set<T>(string paramName, T[] value) where T : unmanaged
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, value);
                return true;
            }
            else return false;
        }
        public bool Set(string paramName, Texture2D texture)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, texture);
                return true;
            }
            return false;
        }
        public bool Set(string paramName, RenderBuffer renderTarget)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, renderTarget);
                return true;
            }
            return false;
        }

        public void Set(int paramIndex, RenderBuffer renderTarget)
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.imageAndSampler)
            {
                param.stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].textures[0] = renderTarget.backendTexture;

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            //we need this to ensure that the image is ready for reading by the shader instead of
                            //being in PresentSrcKhr which it would have been when it was fresh out of the render call
                            VkEngine.TransitionImageLayout(new Image(renderTarget.backendTexture.imageHandle), ImageAspectFlags.ColorBit, ImageLayout.Undefined, ImageLayout.ShaderReadOnlyOptimal, VkEngine.commandBuffer);
                            
                            /*DescriptorImageInfo imageInfo = new DescriptorImageInfo();
                            imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
                            imageInfo.ImageView = new ImageView(renderTarget.backendTexture.imageViewHandle);
                            imageInfo.Sampler = new Sampler(renderTarget.backendTexture.samplerState.handle);

                            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                            descriptorWrite.SType = StructureType.WriteDescriptorSet;
                            descriptorWrite.DstSet = shader.descriptorSet;
                            descriptorWrite.DstBinding = param.binding;
                            descriptorWrite.DstArrayElement = 0;

                            descriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
                            descriptorWrite.DescriptorCount = 1;
                            descriptorWrite.PImageInfo = &imageInfo;

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);*/
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new InvalidOperationException("Attempting to set texture into a non-texture-based shader uniform!");
        }
        public void Set(int paramIndex, Texture2D texture)
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.imageAndSampler)
            {
                param.stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].textures[0] = texture;

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {

                            /*DescriptorImageInfo imageInfo = new DescriptorImageInfo();
                            imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
                            imageInfo.ImageView = new ImageView(texture.imageViewHandle);
                            imageInfo.Sampler = new Sampler(texture.samplerState.handle);

                            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                            descriptorWrite.SType = StructureType.WriteDescriptorSet;
                            descriptorWrite.DstSet = shader.descriptorSet;
                            descriptorWrite.DstBinding = param.binding;
                            descriptorWrite.DstArrayElement = 0;

                            descriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
                            descriptorWrite.DescriptorCount = 1;
                            descriptorWrite.PImageInfo = &imageInfo;

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);*/
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new InvalidOperationException("Attempting to set texture into a non-texture-based shader uniform!");
        }
        //TODO: public void Set(int paramIndex, Texture2D[] texture)
        public void Set<T>(int paramIndex, T value) where T : unmanaged
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.uniformBuffer)
            {
                shader.uniformHasBeenSet = true;

                param.GetUniformBuffer().SetData(value, 0);

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {

                            /*DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();

                            bufferInfo.Buffer = new Buffer(param.GetUniformBuffer().handle);//uniformBuffers[i];
                            bufferInfo.Range = param.width;
                            bufferInfo.Offset = 0;

                            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                            descriptorWrite.SType = StructureType.WriteDescriptorSet;
                            descriptorWrite.DstSet = shader.descriptorSet;
                            descriptorWrite.DstBinding = param.binding;
                            descriptorWrite.DstArrayElement = 0;
                            descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
                            descriptorWrite.DescriptorCount = 1;
                            descriptorWrite.PBufferInfo = &bufferInfo;

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);*/
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new InvalidOperationException("Attempting to set uniform data of type " + typeof(T).Name + " into a non-buffer-based shader uniform!");
        }
        public void Set<T>(int paramIndex, T[] value) where T : unmanaged
        {
            var param = parameters[paramIndex];
            shader.uniformHasBeenSet = true;
            if (param.type == UniformType.uniformBuffer)
            {
                param.GetUniformBuffer().SetData(value, 0);

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            /*DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                            bufferInfo.Buffer = new Buffer(param.GetUniformBuffer().handle);//uniformBuffers[i];
                            bufferInfo.Range = param.width;

                            WriteDescriptorSet* descriptorWrites = stackalloc WriteDescriptorSet[value.Length];

                            for (int i = 0; i < value.Length; i++)
                            {
                                WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                descriptorWrite.DstSet = shader.descriptorSet;
                                descriptorWrite.DstBinding = param.binding;
                                descriptorWrite.DstArrayElement = (uint)i;
                                descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
                                descriptorWrite.DescriptorCount = 1;
                                descriptorWrite.PBufferInfo = &bufferInfo;

                                descriptorWrites[i] = descriptorWrite;
                            }

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, (uint)value.Length, descriptorWrites, 0, null);*/
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new NotImplementedException();
        }
        public void AddStagingDatas()
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];

                if (param.type == UniformType.uniformBuffer)
                {
                    var buffer = new UniformBuffer(application, param.width);
                    param.stagingData[application.Window.frameNumber].Add(new StagingMutableState(buffer));
                    if (Application.Config.logUniformBufferAllocations)
                    {
                        Debugger.Log("Created new uniform buffer for shader param " + param.name + " for index " + shader.descriptorForThisDrawCall);
                    }
                }
                else if (param.type == UniformType.imageBuffer || param.type == UniformType.imageAndSampler)
                {
                    param.stagingData[application.Window.frameNumber].Add(new StagingMutableState((int)Math.Max(1, param.arrayLength)));
                }
            }
        }
        public void Dispose()
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                parameters[i].Dispose();
            }
        }
    }
    /// <summary>
    /// Whenever SetUniform is called in Shader.cs, the mutable state is updated. The changes are
    /// normally only pushed to the GPU right before the draw call.
    /// </summary>
    internal struct StagingMutableState
    {
        //does not need to be an array as a single buffer can fit multiple datas
        public UniformBuffer uniformBuffer;
        //needs to be an array for texture arrays
        public Texture2D[] textures;

        public bool mutated;


        #region Vulkan
        public DescriptorBufferInfo vkBufferInfo;
        public DescriptorImageInfo vkImageInfo;
        #endregion

        public StagingMutableState(UniformBuffer uniformBuffer)
        {
            vkBufferInfo = default;
            vkImageInfo = default;
            mutated = false;
            this.uniformBuffer = uniformBuffer;
            textures = null;
        }
        public StagingMutableState(int textureArrayLength)
        {
            vkBufferInfo = default;
            vkImageInfo = default;
            mutated = false;
            uniformBuffer = null;
            textures = new Texture2D[textureArrayLength];
        }
    }
    public class ShaderParameter : IDisposable
    {
        public readonly Shader shader;

        public readonly Application application;
        /// <summary>
        /// The name of the parameter
        /// </summary>
        public readonly string name;
        /// <summary>
        /// The binding within the shader itself
        /// </summary>
        public readonly uint binding;
        /// <summary>
        /// The type of variable this parameter handles
        /// </summary>
        public readonly UniformType type;
        /// <summary>
        /// The length of the array that this parameter handles where applicable
        /// </summary>
        public readonly uint arrayLength;
        /// <summary>
        /// The size of the type of variable that this parameter represents
        /// </summary>
        public readonly uint size;
        /// <summary>
        /// The total width occupied by the variable that this parameter represents, equals to size * arrayLength for arrays
        /// </summary>
        public readonly ulong width;

        internal readonly UnorderedList<StagingMutableState>[] stagingData;

        internal UniformBuffer GetUniformBuffer()
        {
            return stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].uniformBuffer;
        }

        public ShaderParameter(Shader shader, string name, uint index, UniformType type, uint size, uint arrayLength)
        {
            this.shader = shader;
            this.application = shader.application;
            this.type = type;
            this.name = name;
            this.binding = index;
            this.arrayLength = arrayLength;
            this.size = size;
            this.width = size * Math.Max(1, arrayLength);

            stagingData = new UnorderedList<StagingMutableState>[Application.Config.maxSimultaneousFrames];
            for (int i = 0; i < stagingData.Length; i++)
            {
                stagingData[i] = new UnorderedList<StagingMutableState>();
            }
            /*if (type == UniformType.uniformBuffer)
            {
                stagingBuffersPerFrame = new List<UniformBuffer>[Application.Config.maxSimultaneousFrames];
                for (int i = 0;i  < stagingBuffersPerFrame.Length; i++)
                {
                    stagingBuffersPerFrame[i] = new List<UniformBuffer>();
                }
            }
            else if (type == UniformType.imageAndSampler || type == UniformType.imageBuffer)
            {
                stagingImagesPerFrame = new List<Texture2D>[Application.Config.maxSimultaneousFrames];
                for (int i = 0; i < stagingImagesPerFrame.Length; i++)
                {
                    stagingImagesPerFrame[i] = new List<Texture2D>();
                }
            }*/
        }

        public void Dispose()
        {
            if (type == UniformType.uniformBuffer)
            {
                if (stagingData != null)
                {
                    for (int i = 0; i < stagingData.Length; i++)
                    {
                        for (int j = 0; j < stagingData[i].Count; j++)
                        {
                            stagingData[i][j].uniformBuffer?.Dispose();
                        }
                    }
                }
            }
        }
    }
    public readonly struct ShaderParamUniformData
    {
        public readonly string name;
        public readonly uint set;
        public readonly uint binding;
        public readonly uint stride;
        public readonly uint arrayLength;

        public ShaderParamUniformData(string name, uint set, uint binding, uint stride, uint arrayLength)
        {
            this.name = name;
            this.set = set;
            this.binding = binding;
            this.stride = stride;
            this.arrayLength = arrayLength;
        }
    }
    public readonly struct ShaderParamImageSamplerData
    {
        public readonly string name;
        public readonly uint set;
        public readonly uint binding;
        public readonly uint arrayLength;

        public ShaderParamImageSamplerData(string name, uint set, uint binding, uint arrayLength)
        {
            this.name = name;
            this.set = set;
            this.binding = binding;
            this.arrayLength = arrayLength;
        }
    }
}