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
            ShaderParameter param = new ShaderParameter(shader, name, binding, UniformType.image, 0, arrayLength);
            AddParameter(param);
            //param.offset = maxWidth;
            //maxWidth += param.size;
        }
        public void AddSamplerParameter(string name, uint binding, uint arrayLength = 1)
        {
            ShaderParameter param = new ShaderParameter(shader, name, binding, UniformType.sampler, 0, arrayLength);
            AddParameter(param);
        }
        public ReadOnlySpan<ShaderParameter> GetParameters() => parameters.AsReadonlySpan();
        public bool HasUniform(string paramName) => map.ContainsKey(paramName);
        public ShaderParameter GetUniform(string paramName)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                return parameters[index];
            }
            else return null;
        }
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
        public bool Set(string paramName, ReadOnlySpan<byte> bytes)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                SetBytes(index, bytes);
                return true;
            }
            return false;
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
        public bool Set(string paramName, ReadOnlySpan<Texture2D> textures)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, textures);
                return true;
            }
            return false;
        }
        public bool Set(string paramName, ReadOnlySpan<SamplerState> samplerStates)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, samplerStates);
                return true;
            }
            return false;
        }
        public bool Set(string paramName, SamplerState samplerState)
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, samplerState);
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
            if (param.type == UniformType.image)
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
            if (param.type == UniformType.image)
            {
                param.stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].textures[0] = texture;
            }
            else throw new InvalidOperationException("Attempting to set texture into a non-texture shader uniform!");
        }
        public void Set(int paramIndex, ReadOnlySpan<Texture2D> textureArray)
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.image)
            {
                var destArray = param.stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].textures;
                if (textureArray.Length > destArray.Length) throw new InvalidOperationException("Length of input differs from uniform's texture array length!");
                if (textureArray.Length < destArray.Length)
                {
                    for (int i = textureArray.Length; i < destArray.Length; i++)
                    {
                        destArray[i] = null;
                    }
                }
                //Array.Copy(textureArray, destArray, textureArray.Length);
                textureArray.CopyTo(destArray);
            }
            else throw new InvalidOperationException("Attempting to set texture array into a non-texture shader uniform!");
        }
        public void Set(int paramIndex, SamplerState samplerState)
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.sampler)
            {
                param.stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].samplers[0] = samplerState;
            }
            else throw new InvalidOperationException("Attempting to set sampler into a non-sampler shader uniform!");
        }
        public void Set(int paramIndex, ReadOnlySpan<SamplerState> samplerStates)
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.sampler)
            {
                var destArray = param.stagingData[application.Window.frameNumber][shader.descriptorForThisDrawCall].samplers;
                if (samplerStates.Length != destArray.Length) throw new InvalidOperationException("Length of input differs from uniform's sampler array length!");
                //Array.Copy(samplerStates, destArray, samplerStates.Length);
                samplerStates.CopyTo(new Span<SamplerState>(destArray, 0, samplerStates.Length));
            }
            else throw new InvalidOperationException("Attempting to set sampler into a non-sampler shader uniform!");
        }
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
        public void SetBytes(int paramIndex, ReadOnlySpan<byte> value)
        {
            var param = parameters[paramIndex];
            shader.uniformHasBeenSet = true;
            if (param.type == UniformType.uniformBuffer)
            {
                param.GetUniformBuffer().SetDataBytes(value, 0);
            }
            else throw new InvalidOperationException();
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
                else if (param.type == UniformType.image)
                {
                    Texture2D[] textures = new Texture2D[(int)Math.Max(1, param.arrayLength)];
                    param.stagingData[application.Window.frameNumber].Add(new StagingMutableState(textures));
                }
                else if (param.type == UniformType.sampler)
                {
                    SamplerState[] samplers = new SamplerState[(int)Math.Max(1, param.arrayLength)];
                    param.stagingData[application.Window.frameNumber].Add(new StagingMutableState(samplers));
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
        public SamplerState[] samplers;

        public bool mutated;


        #region Vulkan
        public DescriptorBufferInfo vkBufferInfo;
        public DescriptorImageInfo[] vkImageInfos;
        #endregion

        public StagingMutableState(UniformBuffer uniformBuffer)
        {
            vkBufferInfo = default;
            vkImageInfos = null;
            mutated = false;
            this.uniformBuffer = uniformBuffer;
            textures = null;
            samplers = null;
        }
        public StagingMutableState(Texture2D[] textures)
        {
            vkBufferInfo = default;
            vkImageInfos = new DescriptorImageInfo[textures.Length];
            mutated = false;
            uniformBuffer = null;
            this.textures = textures;
            samplers = null;
        }
        public StagingMutableState(SamplerState[] samplers)
        {
            vkBufferInfo = default;
            vkImageInfos = new DescriptorImageInfo[samplers.Length];
            mutated = false;
            uniformBuffer = null;
            textures = null;
            this.samplers = samplers;
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
    /*public readonly struct ShaderParamImageSamplerData
    {
        public readonly string name;
        public readonly uint set;
        public readonly uint binding;

        public ShaderParamImageSamplerData(string name, uint set, uint binding)
        {
            this.name = name;
            this.set = set;
            this.binding = binding;
        }
    }*/
    public readonly struct ShaderParamImageData
    {
        public readonly string name;
        public readonly uint set;
        public readonly uint binding;
        public readonly uint arrayLength;

        public ShaderParamImageData(string name, uint set, uint binding, uint arrayLength)
        {
            this.name = name;
            this.set = set;
            this.binding = binding;
            this.arrayLength = arrayLength;
        }
    }
    public readonly struct ShaderParamSamplerData
    {
        public readonly string name;
        public readonly uint set;
        public readonly uint binding;
        public readonly uint arrayLength;

        public ShaderParamSamplerData(string name, uint set, uint binding, uint arrayLength)
        {
            this.name = name;
            this.set = set;
            this.binding = binding;
            this.arrayLength = arrayLength;
        }
    }
}