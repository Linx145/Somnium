﻿using System;
using Silk.NET.Vulkan;
using System.IO;
using Somnium.Framework.Vulkan;
using Silk.NET.Core.Native;
using System.Reflection.Metadata;

namespace Somnium.Framework
{
    public sealed class Shader : IDisposable
    {
        public const string main = "main";

        Application application;
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
        /// The Vulkan descriptor set layout for all this shader's parameters
        /// </summary>
        public DescriptorSetLayout descriptorSetLayout;
        public DescriptorSet descriptorSet;
        #endregion

        public Shader(Application application, ShaderType Type, byte[] byteCode)
        {
            this.application = application;
            this.byteCode = byteCode;
            this.byteCode2 = null;
            this.type = Type;
            shader1Params = new ShaderParameterCollection(this, Type, application);
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
                    shader1Params = new ShaderParameterCollection(this, ShaderType.Vertex, application);
                    shader2Params = new ShaderParameterCollection(this, ShaderType.Fragment, application);
                    break;
                case ShaderType.Tessellation:
                    shader1Params = new ShaderParameterCollection(this, ShaderType.TessellationControl, application);
                    shader2Params = new ShaderParameterCollection(this, ShaderType.TessellationEvaluation, application);
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
                            DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[maxCount.Value];

                            //if (shader1Params != null)
                            //{
                            foreach (var value in shader1Params!.GetParameters())
                            {
                                DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding();
                                binding.Binding = value.binding;
                                binding.DescriptorType = ShaderParameter.UniformTypeToVkDescriptorType[(int)value.type];
                                //If the binding points to a variable in the shader that is an array, this would be that array's length
                                binding.DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                                binding.StageFlags = VkGraphicsPipeline.ShaderTypeToFlags[(int)shader1Params!.shaderType];

                                *(bindings + value.binding) = binding;
                            }

                            if (shader2Params != null)
                            {
                                foreach (var value in shader2Params!.GetParameters())
                                {
                                    DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding();
                                    binding.Binding = value.binding;
                                    binding.DescriptorType = ShaderParameter.UniformTypeToVkDescriptorType[(int)value.type];
                                    //If the binding points to a variable in the shader that is an array, this would be that array's length
                                    binding.DescriptorCount = value.arrayLength == 0 ? 1 : value.arrayLength;
                                    binding.StageFlags = VkGraphicsPipeline.ShaderTypeToFlags[(int)shader2Params!.shaderType];

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
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uniform"></param>
        /// <param name="shaderNumber">Which uniform buffer should the data be set into. Should be either 1 or 2.</param>
        public void SetUniform<T>(string uniformName, T uniform, int shaderNumber = 1) where T : unmanaged
        {
            if (shaderNumber == 0 || shaderNumber == 1)
            {
                shader1Params.Set(uniformName, uniform);
            }
            else shader2Params.Set(uniformName, uniform);
        }
        public void SetUniform(string uniformName, Texture2D uniform, int shaderNumber = 1)
        {
            if (shaderNumber == 0 || shaderNumber == 1)
            {
                shader1Params.Set(uniformName, uniform);
            }
            else shader2Params.Set(uniformName, uniform);
        }

        #region static methods
        public static Shader FromFile(Application application, string filePath, ShaderType type)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            return new Shader(application, type, bytes);
        }
        public static Shader FromFiles(Application application, string vertexShaderFile, string fragmentShaderFile)
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
                switch (application.runningBackend)
                {
                    case Backends.Vulkan:
                        unsafe
                        {
                            if (shaderHandle != 0)
                            {
                                ShaderModule module = new ShaderModule(shaderHandle);
                                VkEngine.vk.DestroyShaderModule(VkEngine.vkDevice, module, null);
                                shader1Params?.Dispose();
                            }
                            if (shaderHandle2 != 0)
                            {
                                ShaderModule module = new ShaderModule(shaderHandle2);
                                VkEngine.vk.DestroyShaderModule(VkEngine.vkDevice, module, null);
                                shader2Params?.Dispose();
                            }
                            if (descriptorSetLayout.Handle != 0)
                            {
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