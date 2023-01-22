using System;
using Silk.NET.Vulkan;
using System.IO;
using Somnium.Framework.Vulkan;
using Silk.NET.Core.Native;
using System.Collections.Generic;

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
        public readonly bool useDynamicUniformBuffer;
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
        #endregion

        public Shader(Application application, ShaderType Type, byte[] byteCode, bool useDynamicUniformBuffer = true)
        {
            this.useDynamicUniformBuffer = useDynamicUniformBuffer;
            this.application = application;
            this.byteCode = byteCode;
            this.byteCode2 = null;
            this.type = Type;
            shader1Params = new ShaderParameterCollection(application, this, Type);
            shader2Params = null;

            Construct();
        }
        public Shader(Application application, byte[] shaderCode1, byte[] shaderCode2, ShaderType type = ShaderType.VertexAndFragment, bool useDynamicUniformBuffer = false)
        {
            this.useDynamicUniformBuffer = useDynamicUniformBuffer;
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
                                if (binding.DescriptorType == DescriptorType.UniformBuffer && useDynamicUniformBuffer)
                                {
                                    binding.DescriptorType = DescriptorType.UniformBufferDynamic;
                                }
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

                            descriptorSetsPerFrame = new List<DescriptorSet>[application.Window.maxSimultaneousFrames];
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
        public unsafe void AddDescriptorSets()
        {
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

            if (VkEngine.vk.AllocateDescriptorSets(VkEngine.vkDevice, in allocInfo, &result) != Result.Success)
            {
                throw new AssetCreationException("Failed to create Vulkan descriptor sets!");
            }

            descriptorSetsPerFrame[application.Window.frameNumber].Add(result);

            shader1Params?.AddUniformBuffers();
            shader2Params?.AddUniformBuffers();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uniform"></param>
        /// <param name="shaderNumber">Which uniform buffer should the data be set into</param>
        public void SetUniform<T>(string uniformName, T uniform, SetNumber shaderNumber = SetNumber.Either) where T : unmanaged
        {
            if (!uniformHasBeenSet)
            {
                if (descriptorForThisDrawCall >= descriptorSetsPerFrame[application.Window.frameNumber].Count)
                {
                    AddDescriptorSets();
                }
                uniformHasBeenSet = true;
            }

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniform))
                {
                    if (!shader2Params.Set(uniformName, uniform))
                    {
                        throw new System.Collections.Generic.KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
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
            if (!uniformHasBeenSet)
            {
                if (descriptorForThisDrawCall >= descriptorSetsPerFrame[application.Window.frameNumber].Count)
                {
                    AddDescriptorSets();
                }
                uniformHasBeenSet = true;
            }

            if (shaderNumber == SetNumber.Either)
            {
                if (!shader1Params.Set(uniformName, uniformArray))
                {
                    if (!shader2Params.Set(uniformName, uniformArray))
                    {
                        throw new System.Collections.Generic.KeyNotFoundException("Could not find uniform of name " + uniformName + " in either shader1parameters or shader2parameters!");
                    }
                }
            }
            else if (shaderNumber == SetNumber.First)
            {
                shader1Params.Set(uniformName, uniformArray);
            }
            else shader2Params.Set(uniformName, uniformArray);
        }
        public void SetUniform(string uniformName, Texture2D uniform, SetNumber shaderNumber = SetNumber.Either)
        {
            if (!uniformHasBeenSet)
            {
                if (descriptorForThisDrawCall >= descriptorSetsPerFrame[application.Window.frameNumber].Count)
                {
                    AddDescriptorSets();
                }
                uniformHasBeenSet = true;
            }

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
