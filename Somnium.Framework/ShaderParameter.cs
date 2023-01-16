using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;
using System.Drawing;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class ShaderParameterCollection : IDisposable
    {
        public int Count => parameters.Count;
        private Dictionary<string, int> map;
        private UnorderedList<ShaderParameter> parameters;
        public readonly Shader shader;
        internal readonly Application application;
        public readonly ShaderType shaderType;
        public bool constructed { get; private set; } = false;

        private uint maxWidth;

        public ShaderParameterCollection(Shader shader, ShaderType shaderType, Application application)
        {
            this.shader = shader;
            this.shaderType = shaderType;
            this.application = application;
            map = new Dictionary<string, int>();
            parameters = new UnorderedList<ShaderParameter>();
        }
        /// <summary>
        /// Adds a parameter to the shader to be constructed.
        /// </summary>
        /// <param name="param"></param>
        public void AddParameter(ShaderParameter param)
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
            ShaderParameter param = new ShaderParameter(this, name, index, type, size, arrayLength);
            param.offset = maxWidth;
            maxWidth += param.size;
            AddParameter(param);
        }
        /// <summary>
        /// Adds a parameter to the shader to be constructed.
        /// </summary>
        /// <param name="param"></param>
        public void AddParameter<T>(string name, uint index, UniformType type, uint arrayLength = 0) where T : unmanaged
        {
            unsafe
            {
                AddParameter(name, index, type, (uint)sizeof(T), arrayLength);
            }
        }
        public void AddTexture2DParameter(string name, uint index, uint arrayLength)
        {
            ShaderParameter param = new ShaderParameter(this, name, index, UniformType.imageAndSampler, 0, arrayLength);
            AddParameter(param);
            //param.offset = maxWidth;
            //maxWidth += param.size;
        }
        public ReadOnlySpan<ShaderParameter> GetParameters() => parameters.AsReadonlySpan();
        /*public void Construct()
        {
            if (constructed)
            {
                throw new InvalidOperationException("Shader parameter collection already built!");
            }
            if (map.Count == 0)
            {
                throw new AssetCreationException("Attempting to construct a shader parameter collection without any parameters!");
            }
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[map.Count];

                        for (int i = 0; i < parameters.Count; i++)
                        {
                            var value = parameters[i];
                            DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding();
                            binding.Binding = value.binding;
                            binding.DescriptorType = ShaderParameter.UniformTypeToVkDescriptorType[(int)value.type];
                            //If the binding points to a variable in the shader that is an array, this would be that array's length
                            binding.DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                            binding.StageFlags = VkGraphicsPipeline.ShaderTypeToFlags[(int)shaderType];

                            *(bindings + i) = binding;
                        }

                        DescriptorSetLayoutCreateInfo createInfo = new DescriptorSetLayoutCreateInfo();
                        createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
                        createInfo.BindingCount = (uint)map.Count;
                        createInfo.PBindings = bindings;

                        DescriptorSetLayout descriptorSetLayout;
                        if (VkEngine.vk.CreateDescriptorSetLayout(VkEngine.vkDevice, in createInfo, null, &descriptorSetLayout) != Result.Success)
                        {
                            throw new AssetCreationException("Failed to create new Shader Parameter Collection!");
                        }
                        handle = descriptorSetLayout.Handle;
                    }
                    break;
                default:
                    break;
            }
            constructed = true;
        }*/
        public void Set<T>(string paramName, T value) where T: unmanaged
        {
            Set(map[paramName], value);
        }
        public void Set(string paramName, Texture2D texture)
        {
            Set(map[paramName], texture);
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
                param.uniformBuffer.SetData(value);

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                            bufferInfo.Buffer = new Buffer(param.uniformBuffer.handle);//uniformBuffers[i];
                            bufferInfo.Offset = param.offset;
                            bufferInfo.Range = param.size;

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
        public void Dispose()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            parameters[i].Dispose();
                        }
                        
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public class ShaderParameter : IDisposable
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        public readonly string name;
        public readonly uint binding;
        public readonly UniformType type;
        public readonly uint arrayLength;
        public readonly uint size;
        public readonly ShaderParameterCollection collection;
        public uint offset;
        public UniformBuffer uniformBuffer;

        public ShaderParameter(ShaderParameterCollection collection, string name, uint index, UniformType type, uint size, uint arrayLength = 0)
        {
            this.type = type;
            this.name = name;
            this.binding = index;
            this.arrayLength = arrayLength;
            this.size = size;
            if (size != 0)
            {
                uniformBuffer = new UniformBuffer(collection.application, size);
            }
            else uniformBuffer = null;
        }
        public static ShaderParameter Create<T>(ShaderParameterCollection collection, string name, uint index, UniformType type, uint arrayLength = 0) where T : unmanaged
        {
            int size;
            unsafe
            {
                size = sizeof(T);
            }
            return new ShaderParameter(collection, name, index, type, (uint)size, arrayLength);
        }

        public void Dispose()
        {
            uniformBuffer?.Dispose();
        }
    }
}
