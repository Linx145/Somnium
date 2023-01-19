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
            ShaderParameter param = new ShaderParameter(application.Window, this, name, index, type, size, arrayLength);
            param.offset = shader.bufferSize;
            shader.bufferSize += param.size;
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
            ShaderParameter param = new ShaderParameter(application.Window, this, name, index, UniformType.imageAndSampler, 0, arrayLength);
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
        /*public void Dispose()
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
        }*/
    }
    public class ShaderParameter// : IDisposable
    {
        readonly Window window;
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
        /*public UniformBuffer[] uniformBuffersPerFrame;
        public UniformBuffer uniformBuffer
        {
            get
            {
                if (uniformBuffersPerFrame == null)
                {
                    return null;
                }
                return uniformBuffersPerFrame[window.frameNumber];
            }
        }*/

        public ShaderParameter(Window window, ShaderParameterCollection collection, string name, uint index, UniformType type, uint size, uint arrayLength = 0)
        {
            this.window = window;
            this.type = type;
            this.name = name;
            this.binding = index;
            this.arrayLength = arrayLength;
            this.size = size;
            /*if (size != 0)
            {
                uniformBuffersPerFrame = new UniformBuffer[window.maxSimultaneousFrames];
                for (int i = 0; i < window.maxSimultaneousFrames; i++)
                {
                    uniformBuffersPerFrame[i] = new UniformBuffer(window.application, size);
                }
                //uniformBuffer = new UniformBuffer(collection.application, size);
            }
            else uniformBuffersPerFrame = null;*/
        }
        public static ShaderParameter Create<T>(Window window, ShaderParameterCollection collection, string name, uint index, UniformType type, uint arrayLength = 0) where T : unmanaged
        {
            int size;
            unsafe
            {
                size = sizeof(T);
            }
            return new ShaderParameter(window, collection, name, index, type, (uint)size, arrayLength);
        }

        /*public void Dispose()
        {
            if (uniformBuffersPerFrame != null)
            {
                for (int i = 0; i < window.maxSimultaneousFrames; i++)
                {
                    uniformBuffersPerFrame[i].Dispose();
                }
            }
        }*/
    }
}