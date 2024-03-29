﻿using System;
#if VULKAN
using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
#endif
#if WGPU
using Silk.NET.WebGPU;
using Somnium.Framework.WGPU;
#endif
using System.IO;
using Silk.NET.Core.Native;
using System.Collections.Generic;
using System.Text.Json;
using System.Runtime.CompilerServices;

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
        public bool uniformHasBeenSet = false;

        /// <summary>
        /// This gets increased by 1 if the parameter data is set and a draw call is made.
        /// <br>It will not be increased if it is unused during a draw call, or no draw call is made.</br>
        /// <br>It is then reset when the present queue is submitted</br>
        /// </summary>
        public int descriptorForThisDrawCall = 0;

#if WGPU
        public ulong descriptorSetLayout;
        public List<ulong> bindGroups;
        public ulong bindGroup
        {
            get
            {
                return bindGroups[descriptorForThisDrawCall];
            }
        }
#endif

        #region vulkan
#if VULKAN
        /// <summary>
        /// The Vulkan descriptor set layout for all this shader's parameters. One copy of the same layout per frame
        /// </summary>
        public DescriptorSetLayout descriptorSetLayout;
        public List<DescriptorSet>[] descriptorSetsPerFrame;
        private UnorderedList<WriteDescriptorSet> descriptorSetWrites = new UnorderedList<WriteDescriptorSet>();

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
#endif
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
#if WGPU
                case Backends.WebGPU:
                    if (type == ShaderType.VertexAndFragment)
                    {
                        unsafe
                        {
                            //vs
                            ShaderModuleSPIRVDescriptor vsDescriptor = new ShaderModuleSPIRVDescriptor();
                            vsDescriptor.Chain = new ChainedStruct(null, SType.ShaderModuleSpirvdescriptor);
                            //spirvDescriptor.Code = 
                            vsDescriptor.CodeSize = (uint)(byteCode.Length / 4);
                            fixed (byte* ptr = byteCode)
                            {
                                vsDescriptor.Code = (uint*)ptr;
                            }

                            ShaderModuleDescriptor vsModuleDescriptor = new ShaderModuleDescriptor((ChainedStruct*)&vsDescriptor);

                            ShaderModule* vs = WGPUEngine.wgpu.DeviceCreateShaderModule(WGPUEngine.device, &vsModuleDescriptor);
                            shaderHandle = (ulong)(long)(IntPtr)vs;

                            //fs
                            ShaderModuleSPIRVDescriptor fsDescriptor = new ShaderModuleSPIRVDescriptor();
                            fsDescriptor.Chain = new ChainedStruct(null, SType.ShaderModuleSpirvdescriptor);
                            //spirvDescriptor.Code = 
                            fsDescriptor.CodeSize = (uint)(byteCode2.Length / 4);
                            fixed (byte* ptr = byteCode2)
                            {
                                fsDescriptor.Code = (uint*)ptr;
                            }

                            ShaderModuleDescriptor fsModuleDescriptor = new ShaderModuleDescriptor((ChainedStruct*)&fsDescriptor);

                            ShaderModule* fs = WGPUEngine.wgpu.DeviceCreateShaderModule(WGPUEngine.device, &fsModuleDescriptor);
                            shaderHandle2 = (ulong)(long)(IntPtr)fs;
                        }
                    }
                    break;
#endif
#if VULKAN
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
#endif
                default:
                    throw new NotImplementedException();
            }
        }
#if WGPU
        private unsafe void ParamsFromCollection(ReadOnlySpan<ShaderParameter> collection, BindGroupLayoutEntry* entries, ShaderStage shaderStage)
        {
            foreach (var value in collection)
            {
                if (value.type == UniformType.uniformBuffer)
                {
                    BindGroupLayoutEntry entry = new BindGroupLayoutEntry();
                    entry.Binding = value.binding;
                    entry.Buffer = new BufferBindingLayout()
                    {
                        MinBindingSize = value.width,
                        Type = BufferBindingType.Uniform
                    };
                    entry.Visibility = shaderStage;
                    *(entries + value.binding) = entry;
                }
                else if (value.type == UniformType.sampler)
                {
                    BindGroupLayoutEntry entry = new BindGroupLayoutEntry();
                    entry.Binding = value.binding;
                    entry.Sampler = new SamplerBindingLayout()
                    {
                        Type = SamplerBindingType.Filtering
                    };
                    entry.Visibility = shaderStage;
                    *(entries + value.binding) = entry;
                }
                else if (value.type == UniformType.image)
                {
                    //todo: change to be dynamic
                    TextureSampleType sampleType = TextureSampleType.Float;

                    BindGroupLayoutEntry entry = new BindGroupLayoutEntry();
                    entry.Binding = value.binding;
                    entry.Texture = new TextureBindingLayout()
                    {
                        Multisampled = false,
                        SampleType = sampleType
                    };
                    entry.Visibility = shaderStage;
                    *(entries + value.binding) = entry;
                }
                else throw new NotImplementedException();
            }
        }
#endif
        public void ConstructParams()
        {
            //cannot use this as there may be shared uniforms
            //int? maxCount = (shader1Params?.Count) + (shader2Params?.Count);

            int maxCount = 0;
            if (shader1Params != null)
            {
                foreach (var value in shader1Params!.GetParameters())
                {
                    maxCount = Math.Max(maxCount, (int)value.binding + 1);
                }
            }
            if (shader2Params != null)
            {
                foreach (var value in shader2Params!.GetParameters())
                {
                    maxCount = Math.Max(maxCount, (int)value.binding + 1);
                }
            }
            if (maxCount != 0)
            {
                switch (application.runningBackend)
                {
#if WGPU
                    case Backends.WebGPU:
                        unsafe
                        {
                            ShaderStage shaderStage1 = ShaderStage.None;
                            ShaderStage shaderStage2 = ShaderStage.None;
                            if (type == ShaderType.VertexAndFragment)
                            {
                                shaderStage1 = ShaderStage.Vertex;
                                shaderStage2 = ShaderStage.Fragment;
                            }
                            else if (type == ShaderType.Fragment)
                            {
                                shaderStage1 = ShaderStage.Fragment;
                            }
                            else if (type == ShaderType.Vertex)
                            {
                                shaderStage1 = ShaderStage.Vertex;
                            }
                            else if (type == ShaderType.Compute)
                            {
                                shaderStage1 = ShaderStage.Compute;
                            }

                            BindGroupLayoutEntry* entries = stackalloc BindGroupLayoutEntry[maxCount.Value];

                            if (shader1Params != null)
                            {
                                ParamsFromCollection(shader1Params.GetParameters(), entries, shaderStage1);
                            }
                            if (shader2Params != null)
                            {
                                ParamsFromCollection(shader2Params.GetParameters(), entries, shaderStage2);
                            }

                            BindGroupLayoutDescriptor descriptor = new BindGroupLayoutDescriptor()
                            {
                                Entries = entries,
                                EntryCount = (uint)maxCount.Value
                            };
                            descriptorSetLayout = (ulong)WGPUEngine.wgpu.DeviceCreateBindGroupLayout(WGPUEngine.device, &descriptor);
                        }
                        bindGroups = new List<ulong>();
                        break;
#endif
#if VULKAN
                    case Backends.Vulkan:
                        unsafe
                        {
                            #region create descriptor set layout
                            DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[maxCount];

                            for (int i = 0; i < maxCount; i++)
                            {
                                //zero initialize just in case
                                *(bindings + i) = new DescriptorSetLayoutBinding();
                            }

                            foreach (var value in shader1Params!.GetParameters())
                            {
                                DescriptorSetLayoutBinding* binding = (bindings + value.binding);
                                binding->Binding = value.binding;
                                binding->DescriptorType = Converters.UniformTypeToVkDescriptorType[(int)value.type];
                                binding->DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                                binding->StageFlags = binding->StageFlags | Converters.ShaderTypeToVkFlags[(int)shader1Params!.shaderType];
                            }

                            if (shader2Params != null)
                            {
                                foreach (var value in shader2Params!.GetParameters())
                                {
                                    DescriptorSetLayoutBinding* binding = (bindings + value.binding);
                                    binding->Binding = value.binding;
                                    binding->DescriptorType = Converters.UniformTypeToVkDescriptorType[(int)value.type];
                                    binding->DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                                    binding->StageFlags = binding->StageFlags | Converters.ShaderTypeToVkFlags[(int)shader2Params!.shaderType];
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

                            #region create descriptor sets
                            //we need this so we can fill in the allocate info

                            descriptorSetsPerFrame = new List<DescriptorSet>[Application.Config.maxSimultaneousFrames];
                            for (int i = 0; i < descriptorSetsPerFrame.Length; i++)
                            {
                                descriptorSetsPerFrame[i] = new List<DescriptorSet>();
                            }
                            #endregion
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private unsafe void CheckUniformSet()
        {
            if (!uniformHasBeenSet)
            {
                switch (application.runningBackend)
                {
#if WGPU
                    case Backends.WebGPU:
                        if (descriptorForThisDrawCall >= bindGroups.Count)
                        {
                            //in webGPU, CheckUniforms() simply adds new mutable states if there are none for the current draw call
                            shader1Params?.AddStagingDatas();
                                shader2Params?.AddStagingDatas();
                        }
                        break;
#endif
#if VULKAN
                    case Backends.Vulkan:
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
                        break;
#endif
                    default:
                        break;
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
        public ShaderParameter GetUniform(string uniformName, SetNumber shaderNumber = SetNumber.Either)
        {
            if (shaderNumber == SetNumber.Either)
            {
                var result = shader1Params.GetUniform(uniformName);
                if (result != null)
                {
                    return result;
                }
                else return shader2Params.GetUniform(uniformName);
            }
            else if (shaderNumber == SetNumber.First)
            {
                return shader1Params.GetUniform(uniformName);
            }
            else return shader2Params.GetUniform(uniformName);
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
        public void SetUniforms(string uniformName, ReadOnlySpan<Texture2D> uniforms, SetNumber shaderNumber = SetNumber.Either)
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
        public void SetUniforms(string uniformName, ReadOnlySpan<SamplerState> uniforms, SetNumber shaderNumber = SetNumber.Either)
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
#if WGPU
            unsafe void UpdateForParamsWGPU(ShaderParameterCollection paramsCollection, BindGroupEntry* entries)
            {
                foreach (var param in paramsCollection.GetParameters())
                {
                    ref var mutableState = ref param.stagingData[application.Window.frameNumber].internalArray[descriptorForThisDrawCall];

                    switch (param.type)
                    {
                        case UniformType.uniformBuffer:
                            {
                                BindGroupEntry entry = new BindGroupEntry();
                                entry.Binding = param.binding;
                                entry.Buffer = (Silk.NET.WebGPU.Buffer*)mutableState.uniformBuffer.handle;
                                entry.Size = param.width;
                                *(entries + param.binding) = entry;
                            }
                            break;
                        case UniformType.image:
                            {
                                BindGroupEntry entry = new BindGroupEntry();
                                entry.Binding = param.binding;
                                for (int i = 0; i < mutableState.textureViews.Length; i++)
                                {
                                    mutableState.textureViews[i] = (TextureView*)mutableState.textures[i].imageViewHandle;//null, new ImageView(mutableState.textures[i].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);
                                }
                                Silk.NET.WebGPU.Extensions.WGPU
                                //fixed (TextureView* ptr = mutableState.textureViews)
                                //{
                                entry.TextureView = mutableState.textureViews[0];//ptr;

                                //}
                                *(entries + param.binding) = entry;
                            }
                            break;
                        case UniformType.sampler:
                            {
                                BindGroupEntry entry = new BindGroupEntry();
                                entry.Binding = param.binding; 
                                for (int i = 0; i < mutableState.samplers.Length; i++)
                                {
                                    mutableState.samplerViews[i] = (Sampler*)mutableState.samplers[i].handle;//null, new ImageView(mutableState.textures[i].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);
                                }
                                entry.Sampler = mutableState.samplerViews[0];
                            }
                            break;
                    }
                }
            }
#endif
#if VULKAN
            unsafe void UpdateForParamsVk(ShaderParameterCollection paramsCollection)
            {
                foreach (var param in paramsCollection.GetParameters())
                {
                    ref var mutableState = ref param.stagingData[application.Window.frameNumber].internalArray[descriptorForThisDrawCall];

                    if (!mutableState.mutated)
                    {
                        continue;
                    }
                    mutableState.mutated = false;

                    switch (param.type)
                    {
                        case UniformType.uniformBuffer:
                            {
                                mutableState.vkBufferInfo = new DescriptorBufferInfo(new Silk.NET.Vulkan.Buffer(mutableState.uniformBuffer.handle), 0, param.width);

                                //uint length = (uint)Math.Max(1, param.arrayLength);
                                fixed (DescriptorBufferInfo* ptr = &mutableState.vkBufferInfo)
                                {
                                    WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                    descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                    descriptorWrite.DstSet = descriptorSet;
                                    descriptorWrite.DstBinding = param.binding;
                                    descriptorWrite.DstArrayElement = 0;//(uint)i;
                                    descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
                                    descriptorWrite.DescriptorCount = 1;
                                    descriptorWrite.PBufferInfo = ptr;

                                    descriptorSetWrites.Add(descriptorWrite);
                                }
                            }
                            break;
                        case UniformType.sampler:
                            {
                                uint updateExtents = 0;
                                //samplers also use DescriptorImageInfo as well
                                for (int i = 0; i < mutableState.vkImageInfos.Length; i++, updateExtents++)
                                {
                                    if (mutableState.samplers[i] != null)
                                    {
                                        mutableState.vkImageInfos[i] = new DescriptorImageInfo(new Sampler(mutableState.samplers[i].handle), null, ImageLayout.ShaderReadOnlyOptimal);
                                    }
                                    else break;
                                }

                                if (updateExtents > 0)
                                {
                                    fixed (DescriptorImageInfo* ptr = &mutableState.vkImageInfos[0])
                                    {
                                        WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                        descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                        descriptorWrite.DstSet = descriptorSet;
                                        descriptorWrite.DstBinding = param.binding;
                                        descriptorWrite.DstArrayElement = 0;//(uint)i;
                                        descriptorWrite.DescriptorType = DescriptorType.Sampler;
                                        descriptorWrite.DescriptorCount = updateExtents;
                                        descriptorWrite.PImageInfo = ptr;

                                        descriptorSetWrites.Add(descriptorWrite);
                                    }
                                }
                            }
                            break;
                        case UniformType.image:
                            {
                                uint updateExtents = 0;
                                for (int i = 0; i < mutableState.vkImageInfos.Length; i++, updateExtents++)
                                {
                                    if (mutableState.textures[i] != null)
                                    {
                                        mutableState.vkImageInfos[i] = new DescriptorImageInfo(null, new ImageView(mutableState.textures[i].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);
                                    }
                                    else break;
                                }
                                //= new DescriptorImageInfo(null, new ImageView(mutableState.textures[0].imageViewHandle), ImageLayout.ShaderReadOnlyOptimal);

                                if (updateExtents > 0)
                                {
                                    fixed (DescriptorImageInfo* ptr = &mutableState.vkImageInfos[0])
                                    {
                                        //for (int i = 0; i < length; i++)
                                        //{
                                        //if (mutableState.textures[i] != null)
                                        //{
                                        WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                        descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                        descriptorWrite.DstSet = descriptorSet;
                                        descriptorWrite.DstBinding = param.binding;
                                        descriptorWrite.DstArrayElement = 0;// (uint)i;
                                        descriptorWrite.DescriptorType = DescriptorType.SampledImage;
                                        descriptorWrite.DescriptorCount = updateExtents;//1;
                                        descriptorWrite.PImageInfo = ptr;// + i;

                                        descriptorSetWrites.Add(descriptorWrite);
                                    }
                                }
                            }
                            break;
                        case UniformType.storageBuffer:
                            {
                                mutableState.vkBufferInfo = new DescriptorBufferInfo(new Silk.NET.Vulkan.Buffer(mutableState.storageBuffer.handle), 0, param.width);
                                fixed (DescriptorBufferInfo* ptr = &mutableState.vkBufferInfo)
                                {
                                    WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                                    descriptorWrite.SType = StructureType.WriteDescriptorSet;
                                    descriptorWrite.DstSet = descriptorSet;
                                    descriptorWrite.DstBinding = param.binding;
                                    descriptorWrite.DstArrayElement = 0;
                                    descriptorWrite.DescriptorType = DescriptorType.StorageBuffer;
                                    descriptorWrite.DescriptorCount = 1;
                                    descriptorWrite.PBufferInfo = ptr;

                                    descriptorSetWrites.Add(descriptorWrite);
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                VkEngine.vk.UpdateDescriptorSets(VkEngine.vkDevice, descriptorSetWrites.AsReadonlySpan(), 0, null);
                descriptorSetWrites.Clear();
            }
#endif

                switch (application.runningBackend)
            {
#if WGPU
                case Backends.WebGPU:
                    unsafe
                    {
                        int? maxCount = (shader1Params?.Count) + (shader2Params?.Count);
                        if (maxCount != null)
                        {
                            //need to create a new bind group
                            if (descriptorForThisDrawCall >= bindGroups.Count)
                            {
                                BindGroupEntry* entries = stackalloc BindGroupEntry[maxCount.Value];
                                if (shader1Params != null)
                                {
                                    UpdateForParamsWGPU(shader1Params, entries);
                                }
                                if (shader2Params != null)
                                {
                                    UpdateForParamsWGPU(shader2Params, entries);
                                }
                                var descriptor = new BindGroupDescriptor()
                                {
                                    Entries = entries,
                                    EntryCount = (uint)maxCount.Value,
                                    Layout = (BindGroupLayout*)descriptorSetLayout
                                };
                                BindGroup* bindGroup = WGPUEngine.wgpu.DeviceCreateBindGroup(WGPUEngine.device, &descriptor);
                                bindGroups.Add((ulong)bindGroup);
                            }
                            else
                            {
                                //update current bind group with data in mutable states
                            }
                            //send current bind group to GPU
                            BindGroup* currentBindGroup = (BindGroup*)bindGroup;
                        }
                    }
                    break;
#endif
#if VULKAN
                case Backends.Vulkan:
                    {
                        if (shader1Params != null)
                        {
                            UpdateForParamsVk(shader1Params);
                        }
                        if (shader2Params != null)
                        {
                            UpdateForParamsVk(shader2Params);
                        }
                        break;
                    }
#endif
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

        static void AddUniforms(ShaderParameterCollection collection, List<ShaderParamUniformData> uniforms)
        {
            for (int i = 0; i < uniforms.Count; i++)
            {
                var uniform = uniforms[i];
                collection.AddParameter(uniform.name, uniform.binding, UniformType.uniformBuffer, uniform.stride);
            }
        }
        static void AddStorageBuffers(ShaderParameterCollection collection, List<ShaderParamStorageBufferData> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                var buffer = buffers[i];
                collection.AddParameter(buffer.name, buffer.binding, UniformType.storageBuffer, buffer.maxSize, 1);
            }
        }
        static void AddImages(ShaderParameterCollection collection, List<ShaderParamImageData> images)
        {
            for (int i = 0; i < images.Count; i++)
            {
                var image = images[i];
                collection.AddTexture2DParameter(image.name, image.binding, image.arrayLength);
            }
        }
        static void AddSamplers(ShaderParameterCollection collection, List<ShaderParamSamplerData> samplers)
        {
            for (int i = 0; i < samplers.Count; i++)
            {
                var sampler = samplers[i];
                collection.AddSamplerParameter(sampler.name, sampler.binding, sampler.arrayLength);
            }
        }

        public static Shader FromStream(Application application, Stream stream)
        {
            JsonDocument doc = JsonDocument.Parse(stream, default);

            List<byte> firstShaderBytes = new List<byte>();
            List<byte> secondShaderBytes = new List<byte>();

            if (doc.RootElement.TryGetProperty("shadertype7", out var compElement))
            {
                var uniforms = new List<ShaderParamUniformData>();
                var storageBuffers = new List<ShaderParamStorageBufferData>();

                //todo: support image load store

                JsonElement elem;
                if (compElement.TryGetProperty("uniforms", out elem))
                {
                    foreach (var uniformObj in elem.EnumerateArray())
                    {
                        uniforms.Add(new ShaderParamUniformData(
                            uniformObj.GetProperty("name").GetString(),
                            uniformObj.GetProperty("set").GetUInt32(),
                            uniformObj.GetProperty("binding").GetUInt32(),
                            uniformObj.GetProperty("stride").GetUInt32(),
                            uniformObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                    if (compElement.TryGetProperty("storageBuffers", out elem))
                    {
                        foreach (var samplerObj in elem.EnumerateArray())
                        {
                            storageBuffers.Add(new ShaderParamStorageBufferData(
                                samplerObj.GetProperty("name").GetString(),
                                samplerObj.GetProperty("set").GetUInt32(),
                                samplerObj.GetProperty("binding").GetUInt32(),
                                samplerObj.GetProperty("maxSize").GetUInt64()
                                ));
                        }
                    }
                }

                if (application.runningBackend == Backends.Vulkan)
                {
                    byte[] bytes = new byte[4];
                    foreach (var obj in compElement.GetProperty("spirv").EnumerateArray())
                    {
                        Unsafe.As<byte, uint>(ref bytes[0]) = obj.GetUInt32();
                        firstShaderBytes.Add(bytes[0]);
                        firstShaderBytes.Add(bytes[1]);
                        firstShaderBytes.Add(bytes[2]);
                        firstShaderBytes.Add(bytes[3]);
                    }

                    Shader result;
                    result = new Shader(application, firstShaderBytes.ToArray(), null, ShaderType.Compute);

                    AddUniforms(result.shader1Params, uniforms);
                    AddStorageBuffers(result.shader1Params, storageBuffers);

                    result.ConstructParams();

                    return result;
                }
                else throw new NotImplementedException();
            }
            else if (doc.RootElement.TryGetProperty("shadertype1", out var vertElement)
                && doc.RootElement.TryGetProperty("shadertype2", out var fragElement)) //vertex shader
            {
                var vertexUniforms = new List<ShaderParamUniformData>();
                var fragmentUniforms = new List<ShaderParamUniformData>();

                var vertexStorageBuffers = new List<ShaderParamStorageBufferData>();
                var fragmentStorageBuffers = new List<ShaderParamStorageBufferData>();

                var vertexImages = new List<ShaderParamImageData>();
                var fragmentImages = new List<ShaderParamImageData>();

                var vertexSamplers = new List<ShaderParamSamplerData>();
                var fragmentSamplers = new List<ShaderParamSamplerData>();

                JsonElement elem;
                if (vertElement.TryGetProperty("uniforms", out elem))
                {
                    foreach (var uniformObj in elem.EnumerateArray())
                    {
                        vertexUniforms.Add(new ShaderParamUniformData(
                            uniformObj.GetProperty("name").GetString(),
                            uniformObj.GetProperty("set").GetUInt32(),
                            uniformObj.GetProperty("binding").GetUInt32(),
                            uniformObj.GetProperty("stride").GetUInt32(),
                            uniformObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                }
                if (fragElement.TryGetProperty("uniforms", out elem))
                {
                    foreach (var uniformObj in elem.EnumerateArray())
                    {
                        fragmentUniforms.Add(new ShaderParamUniformData(
                            uniformObj.GetProperty("name").GetString(),
                            uniformObj.GetProperty("set").GetUInt32(),
                            uniformObj.GetProperty("binding").GetUInt32(),
                            uniformObj.GetProperty("stride").GetUInt32(),
                            uniformObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                }

                if (vertElement.TryGetProperty("images", out elem))
                {
                    foreach (var imageObj in elem.EnumerateArray())
                    {
                        vertexImages.Add(new ShaderParamImageData(
                            imageObj.GetProperty("name").GetString(),
                            imageObj.GetProperty("set").GetUInt32(),
                            imageObj.GetProperty("binding").GetUInt32(),
                            imageObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                }
                if (fragElement.TryGetProperty("images", out elem))
                {
                    foreach (var imageObj in elem.EnumerateArray())
                    {
                        fragmentImages.Add(new ShaderParamImageData(
                            imageObj.GetProperty("name").GetString(),
                            imageObj.GetProperty("set").GetUInt32(),
                            imageObj.GetProperty("binding").GetUInt32(),
                            imageObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                }

                if (vertElement.TryGetProperty("samplers", out elem))
                {
                    foreach (var samplerObj in elem.EnumerateArray())
                    {
                        vertexSamplers.Add(new ShaderParamSamplerData(
                            samplerObj.GetProperty("name").GetString(),
                            samplerObj.GetProperty("set").GetUInt32(),
                            samplerObj.GetProperty("binding").GetUInt32(),
                            samplerObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                }
                if (fragElement.TryGetProperty("samplers", out elem))
                {
                    foreach (var samplerObj in elem.EnumerateArray())
                    {
                        fragmentSamplers.Add(new ShaderParamSamplerData(
                            samplerObj.GetProperty("name").GetString(),
                            samplerObj.GetProperty("set").GetUInt32(),
                            samplerObj.GetProperty("binding").GetUInt32(),
                            samplerObj.GetProperty("arrayLength").GetUInt32()
                            ));
                    }
                }

                if (vertElement.TryGetProperty("storageBuffers", out elem))
                {
                    foreach (var samplerObj in elem.EnumerateArray())
                    {
                        vertexStorageBuffers.Add(new ShaderParamStorageBufferData(
                            samplerObj.GetProperty("name").GetString(),
                            samplerObj.GetProperty("set").GetUInt32(),
                            samplerObj.GetProperty("binding").GetUInt32(),
                            samplerObj.GetProperty("maxSize").GetUInt64()
                            ));
                    }
                }
                if (fragElement.TryGetProperty("storageBuffers", out elem))
                {
                    foreach (var samplerObj in elem.EnumerateArray())
                    {
                        fragmentStorageBuffers.Add(new ShaderParamStorageBufferData(
                            samplerObj.GetProperty("name").GetString(),
                            samplerObj.GetProperty("set").GetUInt32(),
                            samplerObj.GetProperty("binding").GetUInt32(),
                            samplerObj.GetProperty("maxSize").GetUInt64()
                            ));
                    }
                }

                if (application.runningBackend == Backends.Vulkan)
                {
                    byte[] bytes = new byte[4];
                    foreach (var obj in vertElement.GetProperty("spirv").EnumerateArray())
                    {
                        Unsafe.As<byte, uint>(ref bytes[0]) = obj.GetUInt32();
                        firstShaderBytes.Add(bytes[0]);
                        firstShaderBytes.Add(bytes[1]);
                        firstShaderBytes.Add(bytes[2]);
                        firstShaderBytes.Add(bytes[3]);
                    }
                    foreach (var obj in fragElement.GetProperty("spirv").EnumerateArray())
                    {
                        Unsafe.As<byte, uint>(ref bytes[0]) = obj.GetUInt32();
                        secondShaderBytes.Add(bytes[0]);
                        secondShaderBytes.Add(bytes[1]);
                        secondShaderBytes.Add(bytes[2]);
                        secondShaderBytes.Add(bytes[3]);
                    }

                    Shader result;
                    result = new Shader(application, firstShaderBytes.ToArray(), secondShaderBytes.ToArray());

                    AddUniforms(result.shader1Params, vertexUniforms);
                    AddUniforms(result.shader2Params, fragmentUniforms);

                    AddStorageBuffers(result.shader1Params, vertexStorageBuffers);
                    AddStorageBuffers(result.shader2Params, fragmentStorageBuffers);

                    AddSamplers(result.shader1Params, vertexSamplers);
                    AddSamplers(result.shader2Params, fragmentSamplers);

                    AddImages(result.shader1Params, vertexImages);
                    AddImages(result.shader2Params, fragmentImages);

                    result.ConstructParams();

                    return result;
                }
                else throw new NotImplementedException();
            }
            else throw new NotSupportedException("Unknown shader combination in file!");
        }
        public static Shader FromFile(Application application, string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
                return FromStream(application, fs);
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
#if WGPU
                    case Backends.WebGPU:
                        unsafe
                        {
                            if (shaderHandle != 0)
                            {
                                ShaderModule* module = (ShaderModule*)new UIntPtr(shaderHandle).ToPointer();
                                WGPUEngine.crab.ShaderModuleDrop(module);
                            }
                            if (shaderHandle2 != 0)
                            {
                                ShaderModule* module = (ShaderModule*)new UIntPtr(shaderHandle2).ToPointer();
                                WGPUEngine.crab.ShaderModuleDrop(module);
                            }
                            WGPUEngine.crab.BindGroupLayoutDrop((BindGroupLayout*)descriptorSetLayout);
                            
                        }
                        break;
#endif
#if VULKAN
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
#endif
                    default:
                        throw new NotImplementedException();
                }
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }
    }
}
