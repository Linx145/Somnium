using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;
using Somnium.Framework.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class ShaderParameterCollection// : IDisposable
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
        public void AddParameter(string name, uint index, UniformType type, uint size, uint arrayLength = 0)
        {
            ShaderParameter param = new ShaderParameter(name, index, type, size, arrayLength);
            param.offset = shader.bufferSize;
            shader.bufferSize += param.width;
            AddParameter(param);
        }
        /// <summary>
        /// Adds a parameter to the shader to be constructed.
        /// </summary>
        /// <param name="param"></param>
        public void AddParameter<T>(string name, uint index, UniformType type = UniformType.uniformBuffer, uint arrayLength = 0) where T : unmanaged
        {
            unsafe
            {
                AddParameter(name, index, type, (uint)sizeof(T), arrayLength);
            }
        }
        public void AddTexture2DParameter(string name, uint index, uint arrayLength = 1)
        {
            ShaderParameter param = new ShaderParameter(name, index, UniformType.imageAndSampler, 0, arrayLength);
            AddParameter(param);
            //param.offset = maxWidth;
            //maxWidth += param.size;
        }
        public ReadOnlySpan<ShaderParameter> GetParameters() => parameters.AsReadonlySpan();
        public void HasUniform(string paramName) => map.ContainsKey(paramName);
        public bool Set<T>(string paramName, T value) where T : unmanaged
        {
            if (map.TryGetValue(paramName, out var index))
            {
                Set(index, value);
                return true;
            }
            else return false;
        }
        public bool Set<T>(string paramName, ReadOnlySpan<T> value) where T : unmanaged
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
        public void Set(int paramIndex, Texture2D texture)
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.imageAndSampler)
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            DescriptorImageInfo imageInfo = new DescriptorImageInfo();
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

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public void Set<T>(int paramIndex, T value) where T : unmanaged
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.uniformBuffer)
            {
                shader.uniformBuffer.SetData(value, param.offset);

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                            bufferInfo.Buffer = new Buffer(shader.uniformBuffer.handle);//uniformBuffers[i];
                            bufferInfo.Offset = param.offset;
                            bufferInfo.Range = param.width;

                            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                            descriptorWrite.SType = StructureType.WriteDescriptorSet;
                            descriptorWrite.DstSet = shader.descriptorSet;
                            descriptorWrite.DstBinding = param.binding;
                            descriptorWrite.DstArrayElement = 0;

                            descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
                            descriptorWrite.DescriptorCount = 1;
                            descriptorWrite.PBufferInfo = &bufferInfo;

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new NotImplementedException();
        }
        public void Set<T>(int paramIndex, ReadOnlySpan<T> value) where T : unmanaged
        {
            var param = parameters[paramIndex];
            if (param.type == UniformType.uniformBuffer)
            {
                shader.uniformBuffer.SetData(value, param.offset);

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                            bufferInfo.Buffer = new Buffer(shader.uniformBuffer.handle);//uniformBuffers[i];
                            bufferInfo.Offset = param.offset;
                            bufferInfo.Range = param.width;

                            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                            descriptorWrite.SType = StructureType.WriteDescriptorSet;
                            descriptorWrite.DstSet = shader.descriptorSet;
                            descriptorWrite.DstBinding = param.binding;
                            descriptorWrite.DstArrayElement = 0;
                            descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
                            descriptorWrite.DescriptorCount = 1;// (uint)value.Length;
                            descriptorWrite.PBufferInfo = &bufferInfo;

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new NotImplementedException();
        }
    }
    public class ShaderParameter// : IDisposable
    {
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
        /// <summary>
        /// Where in the buffer the variable's memory space begins at
        /// </summary>
        public ulong offset;

        public ShaderParameter(string name, uint index, UniformType type, uint size, uint arrayLength = 0)
        {
            this.type = type;
            this.name = name;
            this.binding = index;
            this.arrayLength = arrayLength;
            this.size = size;
            this.width = size * Math.Max(1, arrayLength);
        }
    }
}