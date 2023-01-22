using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;
using Somnium.Framework.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

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
                shader.uniformHasBeenSet = true;
                if (shader.useDynamicUniformBuffer)
                {
                    //dont need to specify offset here as it will automatically set the offset to the dynamic
                    //buffer extents
                    VkEngine.unifiedDynamicBuffer.SetData(value, 0);
                }
                else param.GetUniformBuffer().SetData(value, 0);

                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            if (shader.useDynamicUniformBuffer)
                            {
                                DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                                bufferInfo.Buffer = new Buffer(VkEngine.unifiedDynamicBuffer.handle);//uniformBuffers[i];
                                bufferInfo.Range = param.width;
                                bufferInfo.Offset = 0; //dont need to specify offset here because we will do so in cmdBindDescriptorSet

                                WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                descriptorWrite.DstSet = shader.descriptorSet;
                                descriptorWrite.DstBinding = param.binding;
                                descriptorWrite.DstArrayElement = 0;

                                descriptorWrite.DescriptorType = DescriptorType.UniformBufferDynamic;
                                descriptorWrite.DescriptorCount = 1;
                                descriptorWrite.PBufferInfo = &bufferInfo;

                                VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);
                            }
                            else
                            {
                                DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                                
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

                                VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, 1, &descriptorWrite, 0, null);
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new NotImplementedException();
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
                            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
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

                            VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, (uint)value.Length, descriptorWrites, 0, null);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else throw new NotImplementedException();
        }
        public void AddUniformBuffers()
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                if (param.type == UniformType.uniformBuffer)
                {
                    Console.WriteLine("Created new uniform buffer for shader param " + param.name + " for index " + shader.descriptorForThisDrawCall);
                    param.uniformBuffersPerFrame[application.Window.frameNumber].Add(new UniformBuffer(application, param.width));
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

        public readonly List<UniformBuffer>[] uniformBuffersPerFrame;

        public UniformBuffer GetUniformBuffer()
        {
            return uniformBuffersPerFrame[application.Window.frameNumber][shader.descriptorForThisDrawCall];
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

            if (type == UniformType.uniformBuffer && !shader.useDynamicUniformBuffer)
            {
                uniformBuffersPerFrame = new List<UniformBuffer>[application.Window.maxSimultaneousFrames];
                for (int i = 0;i  < uniformBuffersPerFrame.Length; i++)
                {
                    uniformBuffersPerFrame[i] = new List<UniformBuffer>();
                }
            }
            else uniformBuffersPerFrame = null;
        }

        public void Dispose()
        {
            if (type == UniformType.uniformBuffer)
            {
                if (uniformBuffersPerFrame != null)
                {
                    for (int i = 0; i < uniformBuffersPerFrame.Length; i++)
                    {
                        for (int j = 0; j < uniformBuffersPerFrame[i].Count; j++)
                        {
                            uniformBuffersPerFrame[i][j].Dispose();
                        }
                    }
                }
            }
        }
    }
}