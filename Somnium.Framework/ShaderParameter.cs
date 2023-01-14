using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class ShaderParameterCollection : IDisposable
    {
        private Dictionary<string, int> map;
        private UnorderedList<ShaderParameter> parameters;
        private readonly Shader shader;
        private readonly Application application;
        private readonly ShaderType shaderType;
        public bool constructed { get; private set; } = false;

        public ulong handle;

        #region Vulkan
        /// <summary>
        /// The vulkan descriptor set. Only set after the shader has been bound to a pipeline state and the pipeline created.
        /// </summary>
        public DescriptorSet descriptorSet;
        #endregion

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
            ShaderParameter param = new ShaderParameter(application, name, index, type, size, arrayLength);
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
        public ReadOnlySpan<ShaderParameter> GetParameters() => parameters.AsReadonlySpan();
        public void Construct()
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
                            binding.Binding = value.index;
                            binding.DescriptorType = ShaderParameter.UniformTypeToVkDescriptorType[(int)value.type];
                            //If the binding points to a variable in the shader that is an array, this would be that array's length
                            binding.DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                            binding.StageFlags = VkGraphicsPipeline.ShaderTypeToFlags[(int)shaderType];

                            *(bindings + value.index) = binding;
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
        }
        public void Set<T>(string paramName, T value) where T: unmanaged
        {
            Set(map[paramName], value);
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
                            descriptorWrite.DstSet = shader.shader1Params.descriptorSet;
                            descriptorWrite.DstBinding = (uint)paramIndex;
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
                        VkEngine.vk.DestroyDescriptorSetLayout(VkEngine.vkDevice, new DescriptorSetLayout(handle), null);
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
        public readonly uint index;
        public readonly UniformType type;
        public readonly uint arrayLength;
        public readonly uint size;
        public uint offset;
        public UniformBuffer uniformBuffer;

        public ShaderParameter(Application application, string name, uint index, UniformType type, uint size, uint arrayLength = 0)
        {
            this.type = type;
            this.name = name;
            this.index = index;
            this.arrayLength = arrayLength;
            this.size = size;
            uniformBuffer = new UniformBuffer(application, size);
        }
        public static ShaderParameter Create<T>(Application application, string name, uint index, UniformType type, uint arrayLength = 0) where T : unmanaged
        {
            int size;
            unsafe
            {
                size = sizeof(T);
            }
            return new ShaderParameter(application, name, index, type, (uint)size, arrayLength);
        }

        public static readonly DescriptorType[] UniformTypeToVkDescriptorType = new DescriptorType[]
        {
            DescriptorType.UniformBuffer,
            DescriptorType.SampledImage,
            DescriptorType.StorageImage,
            DescriptorType.Sampler
        };

        public void Dispose()
        {
            uniformBuffer?.Dispose();
        }
    }
}
