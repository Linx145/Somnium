using System;
using Silk.NET.Vulkan;
using System.IO;
using Somnium.Framework.Vulkan;
using Silk.NET.Core.Native;
using System.Collections.Generic;
using System.Text;
using FMOD;

namespace Somnium.Framework
{
    public sealed class Shader : IDisposable
    {
        public enum SetNumber
        {
            Either, First, Second
        }
        public const string main = "main";
        public static IntPtr mainPtr;

        internal readonly Application application;
        public readonly ShaderType type;
        public bool isDisposed { get; private set; }
        private byte[] byteCode;
        private byte[] byteCode2;

        public ulong shaderHandle;
        public ulong shaderHandle2;

        public ShaderParameterCollection shader1Params;
        public ShaderParameterCollection shader2Params;

        #region vulkan
        /// <summary>
        /// The Vulkan descriptor set layout for all this shader's parameters. One copy of the same layout per frame
        /// </summary>
        public DescriptorSetLayout descriptorSetLayout;
        public List<DescriptorSet>[] descriptorSetsPerFrame;
        private UnorderedList<WriteDescriptorSet> descriptorSetWrites = new UnorderedList<WriteDescriptorSet>();

        /// <summary>
        /// This gets increased by 1 if the parameter data is set and a draw call is made.
        /// <br>It will not be increased if it is unused during a draw call, or no draw call is made.</br>
        /// <br>It is then reset when the present queue is submitted</br>
        /// </summary>
        public int descriptorForThisDrawCall = 0;
        public bool uniformHasBeenSet = false;
        public DescriptorSet descriptorSet
        {
            get
            {
                return descriptorSetsPerFrame[application.Window.frameNumber][descriptorForThisDrawCall];
            }
        }
        /// <summary>
        /// If true, there is at least one descriptor set that has been updated for use in the upcoming draw call.
        /// </summary>
        public bool descriptorHasBeenSet
        {
            get
            {
                return descriptorSetsPerFrame[application.Window.frameNumber].Count > 0;
            }
        }
        #endregion

        public Shader(Application application, ShaderType Type, byte[] byteCode)
        {
            this.application = application;
            this.byteCode = byteCode;
            this.byteCode2 = null;
            this.type = Type;
            shader1Params = new ShaderParameterCollection(application, this, Type);
            shader2Params = null;

            Construct();
        }
        public Shader(Application application, byte[] shaderCode1, byte[] shaderCode2, ShaderType type = ShaderType.VertexAndFragment)
        {
            this.application = application;
            this.type = type;
            this.byteCode = shaderCode1; //vertex / tessellation control
            this.byteCode2 = shaderCode2; //fragment / tessellation evaluation
            switch (type)
            {
                case ShaderType.VertexAndFragment:
                    shader1Params = new ShaderParameterCollection(application, this, ShaderType.Vertex);
                    shader2Params = new ShaderParameterCollection(application, this, ShaderType.Fragment);
                    break;
                case ShaderType.Tessellation:
                    shader1Params = new ShaderParameterCollection(application, this, ShaderType.TessellationControl);
                    shader2Params = new ShaderParameterCollection(application, this, ShaderType.TessellationEvaluation);
                    break;
            }

            Construct();
        }

        /// <summary>
        /// The compiled bytecode for this shader. 
        /// <br>In VertexAndFragment shaders, will represent the Vertex Shader.</br>
        /// <br>In Tessellation shaders, will represent the Tessellation Control Shader.</br>
        /// </summary>
        public ReadOnlySpan<byte> GetCompiledCode()
        {
            return byteCode;
        }
        /// <summary>
        /// The second part of the compiled bytecode for this shader.
        /// <br>In VertexAndFragment shaders, will represent the Fragment Shader.</br>
        /// <br>In Tessellation shaders, will represent the Tessellation Evaluation Shader.</br>
        /// </summary>
        public ReadOnlySpan<byte> GetCompiledCode2()
        {
            return byteCode2;
        }

        private void Construct()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    if (type == ShaderType.VertexAndFragment || type == ShaderType.Tessellation)
                    {
                        unsafe
                        {
                            ShaderModuleCreateInfo vCreateInfo = new ShaderModuleCreateInfo();
                            vCreateInfo.SType = StructureType.ShaderModuleCreateInfo;
                            vCreateInfo.CodeSize = (nuint)byteCode.Length;

                            fixed (byte* ptr = byteCode)
                            {
                                vCreateInfo.PCode = (uint*)ptr;
                                ShaderModule vertexShaderModule;

                                Result creationResult = VkEngine.vk.CreateShaderModule(VkEngine.vkDevice, in vCreateInfo, null, out vertexShaderModule);
                                if (creationResult != Result.Success)
                                {
                                    throw new AssetCreationException("Failed to create Vulkan shader!");
                                }

                                shaderHandle = vertexShaderModule.Handle;
                            }

                            ShaderModuleCreateInfo fCreateInfo = new ShaderModuleCreateInfo();
                            fCreateInfo.SType = StructureType.ShaderModuleCreateInfo;
                            fCreateInfo.CodeSize = (nuint)byteCode2.Length;

                            fixed (byte* ptr = byteCode2)
                            {
                                fCreateInfo.PCode = (uint*)ptr;
                                ShaderModule fragmentShaderModule;

                                Result creationResult = VkEngine.vk.CreateShaderModule(VkEngine.vkDevice, in fCreateInfo, null, out fragmentShaderModule);
                                if (creationResult != Result.Success)
                                {
                                    throw new AssetCreationException("Failed to create Vulkan shader!");
                                }

                                shaderHandle2 = fragmentShaderModule.Handle;
                            }
                        }
                    }
                    else
                    {
                        unsafe
                        {
                            ShaderModuleCreateInfo createInfo = new ShaderModuleCreateInfo();
                            createInfo.SType = StructureType.ShaderModuleCreateInfo;
                            createInfo.CodeSize = (nuint)byteCode.Length;

                            fixed (byte* ptr = byteCode)
                            {
                                createInfo.PCode = (uint*)ptr;
                                ShaderModule shaderModule;

                                Result creationResult = VkEngine.vk.CreateShaderModule(VkEngine.vkDevice, in createInfo, null, out shaderModule);
                                if (creationResult != Result.Success)
                                {
                                    throw new AssetCreationException("Failed to create Vulkan shader!");
                                }

                                shaderHandle = shaderModule.Handle;
                            }
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void ConstructParams()
        {
            int? maxCount = (shader1Params?.Count) + (shader2Params?.Count);
            if (maxCount != null && maxCount != 0)
            {
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            #region create descriptor set layout
                            DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[maxCount.Value];

                            foreach (var value in shader1Params!.GetParameters())
                            {
                                DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding();
                                binding.Binding = value.binding;
                                binding.DescriptorType = Converters.UniformTypeToVkDescriptorType[(int)value.type];
                                //If the binding points to a variable in the shader that is an array, this would be that array's length
                                binding.DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                                binding.StageFlags = Converters.ShaderTypeToFlags[(int)shader1Params!.shaderType];

                                *(bindings + value.binding) = binding;
                            }

                            if (shader2Params != null)
                            {
                                foreach (var value in shader2Params!.GetParameters())
                                {
                                    DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding();
                                    binding.Binding = value.binding;
                                    binding.DescriptorType = Converters.UniformTypeToVkDescriptorType[(int)value.type];
                                    //If the binding points to a variable in the shader that is an array, this would be that array's length
                                    binding.DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                                    binding.StageFlags = Converters.ShaderTypeToFlags[(int)shader2Params!.shaderType];

                                    *(bindings + value.binding) = binding;
                                }
                            }

                            DescriptorSetLayoutCreateInfo createInfo = new DescriptorSetLayoutCreateInfo();
                            createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
                            createInfo.BindingCount = (uint)maxCount;
                            createInfo.PBindings = bindings;

                            DescriptorSetLayout descriptorSetLayout;
                            if (VkEngine.vk.CreateDescriptorSetLayout(VkEngine.vkDevice, in createInfo, null, &descriptorSetLayout) != Result.Success)
                            {
                                throw new AssetCreationException("Failed to create new Shader Parameter Collection!");
                            }
                            this.descriptorSetLayout = descriptorSetLayout;
                            #endregion

                            # region create descriptor sets
                            //we need this so we can fill in the allocate info

                            descriptorSetsPerFrame = new List<DescriptorSet>[Application.Config.maxSimultaneousFrames];
                            for (int i = 0; i < descriptorSetsPerFrame.Length; i++)
                            {
                                descriptorSetsPerFrame[i] = new List<DescriptorSet>();
                            }
                            #endregion
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private unsafe void CheckUniformSet()
        {
            if (!uniformHasBeenSet)
            {
                if (descriptorForThisDrawCall >= descriptorSetsPerFrame[application.Window.frameNumber].Count)
                {
                    //allocate a new descriptor and buffers to go along
                    DescriptorPool relatedPool = VkEngine.GetOrCreateDescriptorPool();

                    DescriptorSet result;

                    DescriptorSetAllocateInfo allocInfo = new DescriptorSetAllocateInfo();
                    allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
                    allocInfo.DescriptorPool = relatedPool;
                    allocInfo.DescriptorSetCount = 1;
                    fixed (DescriptorSetLayout* copy = &descriptorSetLayout)
                    {
                        allocInfo.PSetLayouts = copy;
                    }

                    var allocResult = VkEngine.vk.AllocateDescriptorSets(VkEngine.vkDevice, in allocInfo, &result);
                    if (allocResult != Result.Success)
                    {
                        throw new AssetCreationException("Failed to create Vulkan descriptor sets! Error: " + allocResult.ToString());
                    }

                    descriptorSetsPerFrame[application.Window.frameNumber].Add(result);

                    shader1Params?.AddStagingDatas();
                    shader2Params?.AddStagingDatas();

                }
                uniformHasBeenSet = true;
            }
        }
        public bool HasUniform(string uniformName, SetNumber shaderNumber = SetNumber.Either)
        {
            if (shaderNumber == SetNumber.Either)
            {
                return shader1Params.HasUniform(uniformName) || shader2Params.HasUniform(uniformName);
            }
            else if (shaderNumber == SetNumber.First)
            {
                return shader1Params.HasUniform(uniformName);
            }
            else return shader2Params.HasUniform(uniformName);
        }
        public void SetUniform<T>(string uniformName, T uniform, SetNumber shaderNumber = SetNumber.Either) where T : unmanaged
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniform))
                {
                    if (!shader2Params.Set(uniformName, uniform))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniform);
            }
            else shader2Params.Set(uniformName, uniform);
        }
        public void SetUniforms<T>(string uniformName, T[] uniformArray, SetNumber shaderNumber = SetNumber.Either) where T : unmanaged
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniformArray))
                {
                    if (!shader2Params.Set(uniformName, uniformArray))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniformArray);
            }
            else shader2Params.Set(uniformName, uniformArray);
        }
        public void SetUniform(string uniformName, ReadOnlySpan<byte> bytes, SetNumber shaderNumber = SetNumber.Either)
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, bytes))
                {
                    if (!shader2Params.Set(uniformName, bytes))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, bytes);
            }
            else shader2Params.Set(uniformName, bytes);
        }
        public void SetUniform(string uniformName, Texture2D uniform, SetNumber shaderNumber = SetNumber.Either)
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniform))
                {
                    if (!shader2Params.Set(uniformName, uniform))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniform);
            }
            else shader2Params.Set(uniformName, uniform);
        }
        public void SetUniforms(string uniformName, Texture2D[] uniforms, SetNumber shaderNumber = SetNumber.Either)
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniforms))
                {
                    if (!shader2Params.Set(uniformName, uniforms))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniforms);
            }
            else shader2Params.Set(uniformName, uniforms);
        }
        public void SetUniform(string uniformName, SamplerState uniform, SetNumber shaderNumber = SetNumber.Either)
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniform))
                {
                    if (!shader2Params.Set(uniformName, uniform))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniform);
            }
            else shader2Params.Set(uniformName, uniform);
        }
        public void SetUniform(string uniformName, RenderBuffer uniform, SetNumber shaderNumber = SetNumber.Either)
        {
            CheckUniformSet();

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniform))
                {
                    if (!shader2Params.Set(uniformName, uniform))
                    {
                        throw new KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniform);
            }
            else shader2Params.Set(uniformName, uniform);
        }

        /// <summary>
        /// Called automatically right before drawing. Sends all data from local buffers into the GPU
        /// </summary>
        public void SyncUniformsWithGPU()
        {
            unsafe void UpdateForParams(ShaderParameterCollection paramsCollection)
            {
                foreach (var param in paramsCollection.GetParameters())
                {
                    ref var mutableState = ref param.stagingData[application.Window.frameNumber].internalArray[descriptorForThisDrawCall];

                    switch (param.type)
                    {
                        case UniformType.uniformBuffer:
                            {
                                mutableState.vkBufferInfo = new DescriptorBufferInfo(new Silk.NET.Vulkan.Buffer(mutableState.uniformBuffer.handle), 0, param.width);

                                int length = (int)Math.Max(1, param.arrayLength);
                                fixed (DescriptorBufferInfo* ptr = &mutableState.vkBufferInfo)
                                {
                                    for (int i = 0; i < length; i++)
                                    {
                                        WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                        descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                        descriptorWrite.DstSet = descriptorSet;
                                        descriptorWrite.DstBinding = param.binding;
                                        descriptorWrite.DstArrayElement = (uint)i;
                                        descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
                                        descriptorWrite.DescriptorCount = 1;
                                        descriptorWrite.PBufferInfo = ptr;

                                        descriptorSetWrites.Add(descriptorWrite);
                                        //descriptorWrites[i] = descriptorWrite;
                                    }
                                }
                            }
                            break;
                        case UniformType.sampler:
                            {
                                //samplers also use DescriptorImageInfo as well
                                for (int i = 0; i < mutableState.vkImageInfos.Length; i++)
                                {
                                    mutableState.vkImageInfos[i] = new DescriptorImageInfo(new Sampler(mutableState.samplers[i].handle), null, ImageLayout.ShaderReadOnlyOptimal);
                                }

                                int length = (int)Math.Max(1, param.arrayLength);
                                fixed (DescriptorImageInfo* ptr = &mutableState.vkImageInfos[0])
                                {
                                    for (int i = 0; i < length; i++)
                                    {
                                        WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                        descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                        descriptorWrite.DstSet = descriptorSet;
                                        descriptorWrite.DstBinding = param.binding;
                                        descriptorWrite.DstArrayElement = (uint)i;
                                        descriptorWrite.DescriptorType = DescriptorType.Sampler;
                                        descriptorWrite.DescriptorCount = 1;
                                        descriptorWrite.PImageInfo = ptr + i;

                                        descriptorSetWrites.Add(descriptorWrite);
                                        //descriptorWrites[i] = descriptorWrite;
                                    }
                                }
                            }
                            break;
                        case UniformType.image:
                            {
                                for (int i = 0; i < mutableState.vkImageInfos.Length; i++)
                                {
                                    mutableState.vkImageInfos[i] = new DescriptorImageInfo(null, new ImageView(mutableState.textures[i].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);
                                }
                                //= new DescriptorImageInfo(null, new ImageView(mutableState.textures[0].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);

                                int length = (int)Math.Max(1, param.arrayLength);
                                fixed (DescriptorImageInfo* ptr = &mutableState.vkImageInfos[0])
                                {
                                    for (int i = 0; i < length; i++)
                                    {
                                        WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                        descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                        descriptorWrite.DstSet = descriptorSet;
                                        descriptorWrite.DstBinding = param.binding;
                                        descriptorWrite.DstArrayElement = (uint)i;
                                        descriptorWrite.DescriptorType = DescriptorType.SampledImage;
                                        descriptorWrite.DescriptorCount = 1;
                                        descriptorWrite.PImageInfo = ptr + i;

                                        descriptorSetWrites.Add(descriptorWrite);
                                        //descriptorWrites[i] = descriptorWrite;
                                    }
                                }
                            }
                            break;
                        /*case UniformType.imageAndSampler:
                            {
                                mutableState.vkImageInfo = new DescriptorImageInfo(new Sampler(mutableState.textures[0].samplerState.handle), new ImageView(mutableState.textures[0].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);

                                fixed (DescriptorImageInfo* ptr = &mutableState.vkImageInfo)
                                {
                                    WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                    descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                    descriptorWrite.DstSet = descriptorSet;
                                    descriptorWrite.DstBinding = param.binding;
                                    descriptorWrite.DstArrayElement = 0;
                                    descriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
                                    descriptorWrite.DescriptorCount = 1;
                                    descriptorWrite.PImageInfo = ptr;

                                    descriptorSetWrites.Add(descriptorWrite);
                                }
                            }
                            break;*/
                        default:
                            throw new NotImplementedException();
                    }
                }
                VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, descriptorSetWrites.AsReadonlySpan(), 0, null);
                descriptorSetWrites.Clear();
            }

            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    {
                        if (shader1Params != null)
                        {
                            UpdateForParams(shader1Params);
                        }
                        if (shader2Params != null)
                        {
                            UpdateForParams(shader2Params);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        #region static methods
        public static unsafe byte* Main()
        {
            if (mainPtr == IntPtr.Zero)
            {
                mainPtr = (IntPtr)(byte*)SilkMarshal.StringToPtr(main);
            }
            return (byte*)mainPtr;
        }
        public static void FreeMainStringPointer()
        {
            if (mainPtr != IntPtr.Zero)
            {
                SilkMarshal.Free((nint)mainPtr);
                mainPtr = IntPtr.Zero;
            }
        }
        /// <summary>
        /// Loads a shader from a stream containing a Somnium Engine .shader file.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Shader FromStream(Application application, Stream stream)
        {
            void AddUniforms(ShaderParameterCollection collection, List<ShaderParamUniformData> uniforms)
            {
                for (int i = 0; i < uniforms.Count; i++)
                {
                    var uniform = uniforms[i];
                    collection.AddParameter(uniform.name, uniform.binding, UniformType.uniformBuffer, uniform.stride);
                }
            }
            void AddImages(ShaderParameterCollection collection, List<ShaderParamImageData> images)
            {
                for (int i = 0; i < images.Count; i++)
                {
                    var image = images[i];
                    collection.AddTexture2DParameter(image.name, image.binding, image.arrayLength);
                }
            }
            void AddSamplers(ShaderParameterCollection collection, List<ShaderParamSamplerData> samplers)
            {
                for (int i = 0; i < samplers.Count; i++)
                {
                    var sampler = samplers[i];
                    collection.AddSamplerParameter(sampler.name, sampler.binding, sampler.arrayLength);
                }
            }

            using (BinaryReader reader = new BinaryReader(stream))
            {
                uint version = reader.ReadUInt32();
                if (version == 1 || version == 2)
                {
                    List<byte[]> shaderBytecode = new List<byte[]>();

                    var uniforms1 = new List<ShaderParamUniformData>();
                    var uniforms2 = new List<ShaderParamUniformData>();

                    var images1 = new List<ShaderParamImageData>();
                    var images2 = new List<ShaderParamImageData>();

                    var samplers1 = new List<ShaderParamSamplerData>();
                    var samplers2 = new List<ShaderParamSamplerData>();

                    ulong maxShaders = reader.ReadUInt64();

                    if (maxShaders > 2)
                    {
                        throw new ArgumentOutOfRangeException("Invalid shader file! Shader file has more than 2 shader source bytecodes.");
                    }

                    ShaderTypeFlags flag1 = ShaderTypeFlags.None;
                    ShaderTypeFlags flag2 = ShaderTypeFlags.None;
                    for (ulong i = 0; i < maxShaders; i++)
                    {
                        ShaderTypeFlags type = (ShaderTypeFlags)reader.ReadUInt32();

                        uint uniformsCount = reader.ReadUInt32();
                        for (int c = 0; c < uniformsCount; c++)
                        {
                            uint stringSize = reader.ReadUInt32();
                            byte[] bytes = reader.ReadBytes((int)stringSize);
                            string name = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                            uint set = reader.ReadUInt32();
                            uint binding = reader.ReadUInt32();
                            uint stride = reader.ReadUInt32();
                            uint arrayLength = reader.ReadUInt32();

                            if (i == 0)
                            {
                                uniforms1.Add(new ShaderParamUniformData(name, set, binding, stride, arrayLength));
                            }
                            else uniforms2.Add(new ShaderParamUniformData(name, set, binding, stride, arrayLength));
                        }

                        if (version == 1)
                        {
                            uint samplerImagesCount = reader.ReadUInt32();
                            for (int c = 0; c < samplerImagesCount; c++)
                            {
                                uint stringSize = reader.ReadUInt32();
                                byte[] bytes = reader.ReadBytes((int)stringSize);
                                string name = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                                uint set = reader.ReadUInt32();
                                uint binding = reader.ReadUInt32();
                                uint arrayLength = reader.ReadUInt32();

                                //21/2/2023: Image-Sampler combinations deprecated due to lack of support across multiple graphics API
                                /*if (i == 0)
                                {
                                    imageSamplers1.Add(new ShaderParamImageSamplerData(name, set, binding, arrayLength));
                                }
                                else imageSamplers2.Add(new ShaderParamImageSamplerData(name, set, binding, arrayLength));*/
                            }
                        }
                        if (version >= 2)
                        {
                            uint samplerCount = reader.ReadUInt32();
                            for (int c = 0; c < samplerCount; c++)
                            {
                                uint stringSize = reader.ReadUInt32();
                                byte[] bytes = reader.ReadBytes((int)stringSize);
                                string name = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                                uint set = reader.ReadUInt32();
                                uint binding = reader.ReadUInt32();
                                uint arrayLength = reader.ReadUInt32();

                                if (i == 0)
                                {
                                    samplers1.Add(new ShaderParamSamplerData(name, set, binding, arrayLength));
                                }
                                else samplers2.Add(new ShaderParamSamplerData(name, set, binding, arrayLength));
                            }

                            uint imagesCount = reader.ReadUInt32();
                            for (int c = 0; c < imagesCount; c++)
                            {
                                uint stringSize = reader.ReadUInt32();
                                byte[] bytes = reader.ReadBytes((int)stringSize);
                                string name = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                                uint set = reader.ReadUInt32();
                                uint binding = reader.ReadUInt32();
                                uint arrayLength = reader.ReadUInt32();

                                if (i == 0)
                                {
                                    images1.Add(new ShaderParamImageData(name, set, binding, arrayLength));
                                }
                                else images2.Add(new ShaderParamImageData(name, set, binding, arrayLength));
                            }
                        }

                        ulong size = reader.ReadUInt64();

                        if (i == 0)
                        {
                            flag1 = type;
                        }
                        else flag2 = type;

                        //size is the count of ints
                        shaderBytecode.Add(reader.ReadBytes((int)size * sizeof(uint)));
                    }

                    Shader result;

                    //fragment code comes first, but Shader constructor accepts vertex code first, so we flip them when inputting
                    if (flag1 == ShaderTypeFlags.Vertex && flag2 == ShaderTypeFlags.Fragment)
                    {
                        result = new Shader(application, shaderBytecode[0], shaderBytecode[1], ShaderType.VertexAndFragment);
                        AddUniforms(result.shader1Params, uniforms1);
                        AddUniforms(result.shader2Params, uniforms2);

                        AddSamplers(result.shader1Params, samplers1);
                        AddSamplers(result.shader2Params, samplers2);

                        AddImages(result.shader1Params, images1);
                        AddImages(result.shader2Params, images2);
                        //AddImageSamplers(result.shader1Params, imageSamplers1);
                        //AddImageSamplers(result.shader2Params, imageSamplers2);
                    }
                    else if (flag1 == ShaderTypeFlags.Fragment && flag2 == ShaderTypeFlags.Vertex)
                    {
                        result = new Shader(application, shaderBytecode[1], shaderBytecode[0], ShaderType.VertexAndFragment);
                        AddUniforms(result.shader1Params, uniforms2);
                        AddUniforms(result.shader2Params, uniforms1);

                        AddSamplers(result.shader1Params, samplers2);
                        AddSamplers(result.shader2Params, samplers1);

                        AddImages(result.shader1Params, images2);
                        AddImages(result.shader2Params, images1);
                    }
                    else if (flag1 == ShaderTypeFlags.Vertex)
                    {
                        result = new Shader(application, ShaderType.Vertex, shaderBytecode[0]);
                        AddUniforms(result.shader1Params, uniforms1);
                        AddSamplers(result.shader1Params, samplers1);
                        AddImages(result.shader1Params, images1);
                    }
                    else if (flag1 == ShaderTypeFlags.Fragment)
                    {
                        result = new Shader(application, ShaderType.Fragment, shaderBytecode[0]);
                        AddUniforms(result.shader1Params, uniforms1);
                        AddSamplers(result.shader1Params, samplers1);
                        AddImages(result.shader1Params, images1);
                    }
                    else throw new NotSupportedException("Unsupported shader type combination: " + flag1.ToString() + " and " + flag2.ToString());

                    /*if (flag2 == ShaderTypeFlags.None)
                    {
                        AddUniforms(result.shader1Params, uniforms1);
                        AddImageSamplers(result.shader1Params, imageSamplers1);
                    }*/
                    result.ConstructParams();

                    return result;
                }
                else throw new NotSupportedException(".shader file version not supported: " + version);
            }
        }
        /// <summary>
        /// Loads a shader from a Somnium Engine .shader file, which can contain the bytecode for a single 
        /// shader of any type, or a pair of Vertex+Fragment or Tessellation Control+Evaluation shaders.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Shader FromFile(Application application, string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
                return FromStream(application, fs);
            /*byte[] bytes = File.ReadAllBytes(filePath);
            return new Shader(application, type, bytes);*/
        }
        public static Shader FromBytes(Application application, byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
                return FromStream(application, ms);
        }
        /// <summary>
        /// Creates a new shader from the specified file paths. Up to you to add and compile the shader uniforms yourself.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="vertexShaderFile"></param>
        /// <param name="fragmentShaderFile"></param>
        /// <returns></returns>
        public static Shader FromSpvFiles(Application application, string vertexShaderFile, string fragmentShaderFile)
        {
            byte[] vertex = File.ReadAllBytes(vertexShaderFile);
            byte[] fragment = File.ReadAllBytes(fragmentShaderFile);
            return new Shader(application, vertex, fragment);
        }
        #endregion

        public void Dispose()
        {
            if (!isDisposed)
            {
                shader1Params?.Dispose();
                shader2Params?.Dispose();
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            if (shaderHandle != 0)
                            {
                                ShaderModule module = new ShaderModule(shaderHandle);
                                VkEngine.vk.DestroyShaderModule(VkEngine.vkDevice, module, null);
                            }
                            if (shaderHandle2 != 0)
                            {
                                ShaderModule module = new ShaderModule(shaderHandle2);
                                VkEngine.vk.DestroyShaderModule(VkEngine.vkDevice, module, null);
                            }
                            if (descriptorSetLayout.Handle != 0)
                            {
                                //only need to destroy one as all members of the array are in fact,
                                //just copies of one another for use in multiple simultaneous frames
                                VkEngine.vk.DestroyDescriptorSetLayout(VkEngine.vkDevice, descriptorSetLayout, null);
                            }  
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }
    }
}
