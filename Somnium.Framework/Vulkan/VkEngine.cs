﻿using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Somnium.Framework.Windowing;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VkEngine
    {
        public const string EngineName = "Somnium";
        private static string appName;

        public static Vk vk;
        private static IntPtr engineNamePtr;
        private static IntPtr appNamePtr;

        internal static Instance vkInstance;
        public static Device vkDevice
        {
            get
            {
                return internalVkDevice;
            }
        }
        public static SurfaceKHR WindowSurface
        {
            get
            {
                return internalWindowSurface;
            }
        }
        public static VkGPUInfo CurrentGPU { get; private set; }
        public static VkGraphicsPipeline TrianglePipeline;


        private static Device internalVkDevice;
        internal static KhrSurface KhrSurfaceAPI;
        internal static KhrSwapchain KhrSwapchainAPI;
        private static SurfaceKHR internalWindowSurface;

        private static VkShader testShader;

        public static bool initialized { get; private set; }
        public static unsafe void Initialize(Window window, string AppName, bool enableValidationLayers = true)
        {
            if (!initialized)
            {
                initialized = true;
                appName = AppName;

                vk = Vk.GetApi();

                CreateInstance(window);
                if (ValidationLayersActive)
                {
                    VkDebug.InitializeDebugMessenger();
                }
                CreateSurface(window);
                CreateLogicalDevice();
                CreateSwapChain(window); //also creates image views
                CreateRenderPass();
                CreatePipelines(window);
                swapChain.RecreateFramebuffers(renderPass);
                CreateCommandPool();
                CreateCommandBuffer();
                CreateSynchronizers();
            }
        }
        public static void Draw(Window window)
        {
            vk.WaitForFences(vkDevice, 1, in fence, new Bool32(true), 1000000000);
            vk.ResetFences(vkDevice, 1, in fence);

            SwapChain potentialNewSwapchain = swapChain.SwapBuffers(presentSemaphore, default);
            if (potentialNewSwapchain != null)
            {
                swapChain = potentialNewSwapchain;
            }

            commandBuffer.Reset();
            commandBuffer.Begin();
            renderPass.Begin(commandBuffer, swapChain, Color.CornflowerBlue);
            vk.CmdBindPipeline(commandBuffer.handle, PipelineBindPoint.Graphics, TrianglePipeline);
            //BeginRenderPass(); //also clears the screen

            vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, TrianglePipeline);
            vk.CmdDraw(commandBuffer, 3, 1, 0, 0);

            //EndRenderPass();
            renderPass.End(commandBuffer);
            commandBuffer.End();

            SubmitToGPU(window);
        }
        public static void SubmitToGPU(Window window)
        {
            //submit what we rendered to the GPU
            SubmitInfo submitInfo = new SubmitInfo();
            submitInfo.SType = StructureType.SubmitInfo;

            PipelineStageFlags waitFlag = PipelineStageFlags.ColorAttachmentOutputBit;
            submitInfo.PWaitDstStageMask = &waitFlag;

            submitInfo.WaitSemaphoreCount = 1;
            fixed (Silk.NET.Vulkan.Semaphore* ptr = &presentSemaphore)
            {
                submitInfo.PWaitSemaphores = ptr;
            }

            submitInfo.SignalSemaphoreCount = 1;
            fixed (Silk.NET.Vulkan.Semaphore* ptr = &renderSemaphore)
            {
                submitInfo.PSignalSemaphores = ptr;
            }

            submitInfo.CommandBufferCount = 1;
            fixed (CommandBuffer* ptr = &commandBuffer.handle)
            {
                submitInfo.PCommandBuffers = ptr;
            }

            if (vk.QueueSubmit(CurrentGPU.AllPurposeQueue, 1, in submitInfo, fence) != Result.Success)
            {
                throw new ExecutionException("Error submitting Vulkan Queue!");
            }

            //and present it to the window
            PresentInfoKHR presentInfo = new PresentInfoKHR();
            presentInfo.SType = StructureType.PresentInfoKhr;
            presentInfo.SwapchainCount = 1;
            fixed (SwapchainKHR* ptr = &swapChain.handle)
            {
                presentInfo.PSwapchains = ptr;
            }
            presentInfo.WaitSemaphoreCount = 1;
            fixed (Silk.NET.Vulkan.Semaphore* ptr = &renderSemaphore)
            {
                presentInfo.PWaitSemaphores = ptr;
            }
            uint imageIndex = swapChain.currentImageIndex;
            presentInfo.PImageIndices = &imageIndex;

            Result presentResult = KhrSwapchainAPI.QueuePresent(CurrentGPU.AllPurposeQueue, &presentInfo);
            if (presentResult != Result.Success)
            {
                if (presentResult == Result.ErrorOutOfDateKhr)
                {
                    swapChain.Dispose();
                    swapChain = SwapChain.Create(window);
                    //swapChain.Recreate(SwapChain.QuerySwapChainSupport(CurrentGPU.Device));
                }
                else throw new ExecutionException("Error presenting Vulkan queue!");
            }
        }
        #region instance creation
        /// <summary>
        /// Creates the Vulkan instance to run things with
        /// </summary>
        /// <param name="window">The window that should be used</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InitializationException"></exception>
        private static void CreateInstance(Window window)
        {
            ApplicationInfo vkApplicationInfo = new ApplicationInfo();
            vkApplicationInfo.SType = StructureType.ApplicationInfo;

            vkApplicationInfo.PApplicationName = Utils.StringToBytePtr(appName, out appNamePtr);
            vkApplicationInfo.PEngineName = Utils.StringToBytePtr(EngineName, out engineNamePtr);
            vkApplicationInfo.ApplicationVersion = new Version32(1, 0, 0);
            vkApplicationInfo.EngineVersion = new Version32(0, 1, 0);
            vkApplicationInfo.ApiVersion = Vk.Version11;

            InstanceCreateInfo vkInstanceCreateInfo = new InstanceCreateInfo();
            vkInstanceCreateInfo.SType = StructureType.InstanceCreateInfo;
            vkInstanceCreateInfo.PApplicationInfo = &vkApplicationInfo;

            //get the required window extensions needed to hook the app in, plus additional stuff to get
            //optional modules like Validation Layer Debug Messengers up and running
            requiredInstanceExtensions = GetRequiredInstanceExtensions(window);
            
            vkInstanceCreateInfo.EnabledExtensionCount = (uint)requiredInstanceExtensions.Length;
            requiredInstanceExtensionsPtr = (byte**)SilkMarshal.StringArrayToPtr(requiredInstanceExtensions);
            vkInstanceCreateInfo.PpEnabledExtensionNames = requiredInstanceExtensionsPtr;

            if (ValidationLayersActive)
            {
                if (!CheckValidationLayersSupported())
                {
                    throw new InvalidOperationException("One or more of the inputted Validation Layers are not supported!");
                }

                vkInstanceCreateInfo.EnabledLayerCount = (uint)validationLayersToUse.Length;
                vkInstanceCreateInfo.PpEnabledLayerNames = Utils.StringArrayToPointer(validationLayersToUse, out validationLayersPtr);

                //enable debugging
                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT();
                VkDebug.PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                vkInstanceCreateInfo.PNext = &debugCreateInfo;
            }
            else
            {
                vkInstanceCreateInfo.EnabledLayerCount = 0;
                vkInstanceCreateInfo.PNext = null;
            }

            var creationResult = vk.CreateInstance(in vkInstanceCreateInfo, null, out vkInstance);
            if (creationResult != Result.Success)
            {
                if (creationResult == Result.ErrorExtensionNotPresent)
                {
                    throw new InitializationException("One or more extensions were not found!");
                }
                else throw new InitializationException("Vulkan failed to create instance! Check device for compatibility");
            }            
        }
        #endregion

        #region surface creation
        private static void CreateSurface(Window window)
        {
            //windowSurface = window.CreateWindowSurfaceVulkan();
            if (!vk.TryGetInstanceExtension(vkInstance, out KhrSurfaceAPI))
            {
                throw new InitializationException("KHR_surface extension missing!");
            }
            internalWindowSurface = window.CreateWindowSurfaceVulkan();
        }
        #endregion

        #region validation layers
        private static string[] validationLayersToUse = new string[] { "VK_LAYER_KHRONOS_validation" };
        private static IntPtr validationLayersPtr;
        public static bool enableValidationLayers
        {
            get
            {
                return internalEnableValidationLayers;
            }
            set
            {
                if (initialized)
                {
                    throw new InvalidOperationException("Cannot enable/disable validation layers once Vulkan is initialized!");
                }
                internalEnableValidationLayers = value;
            }
        }
        private static bool internalEnableValidationLayers = true;

        public static bool ValidationLayersActive
        {
            get
            {
                return validationLayersToUse != null && validationLayersToUse.Length > 0 && enableValidationLayers;
            }
        }

        public static void UseValidationLayers(params string[] layers)
        {
            validationLayersToUse = layers;
        }
        public static LayerProperties[] SupportedValidationLayers()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Cannot call SupportedValidationLayers() before initializing Vulkan!");
            }

            uint layerCount = 0;
            vk.EnumerateInstanceLayerProperties(&layerCount, null);

            LayerProperties[] layerProperties = new LayerProperties[layerCount];
            vk.EnumerateInstanceLayerProperties(&layerCount, layerProperties.AsSpan());

            return layerProperties;
        }
        public static bool CheckValidationLayersSupported()
        {
            var allSupported = SupportedValidationLayers();

            int totalSupported = 0;
            for (int i = 0; i < allSupported.Length; i++)
            {
                fixed (byte* supportedNamePtr = allSupported[i].LayerName)
                {
                    string? str = Marshal.PtrToStringAnsi((IntPtr)supportedNamePtr);

                    for (int j = 0; j < validationLayersToUse.Length; j++)
                    {
                        if (validationLayersToUse[j] == str)
                        {
                            totalSupported++;
                            break;
                        }
                    }
                }
            }
            return totalSupported == validationLayersToUse.Length;
        }
        #endregion

        #region extensions
        public static string[] requiredInstanceExtensions;
        private static byte** requiredInstanceExtensionsPtr;
        private static string[] GetRequiredInstanceExtensions(Window window)
        {
            var glfwExtensions = window.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

            if (ValidationLayersActive)
            {
                //VK_EXT_debug_utils
                extensions = extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
            }

            return extensions;
        }

        public static VkExtensionProperties[] SupportedInstanceExtensions()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Cannot call SupportedExtensions() before initializing Vulkan!");
            }
            uint supportedExtensionsCount = 0;
            vk.EnumerateInstanceExtensionProperties((byte*)null, &supportedExtensionsCount, null);

            ExtensionProperties[] extensionProperties = new ExtensionProperties[supportedExtensionsCount];

            vk.EnumerateInstanceExtensionProperties((byte*)null, &supportedExtensionsCount, extensionProperties.AsSpan());

            VkExtensionProperties[] result = new VkExtensionProperties[supportedExtensionsCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new VkExtensionProperties(extensionProperties[i]);
            }
            return result;
        }
        #endregion

        #region Logical Device
        public static string[] requiredDeviceExtensions = new string[] { KhrSwapchain.ExtensionName };
        private static byte** requiredDeviceExtensionsPtr;

        /// <summary>
        /// Locates a suitable GPU based on code in the module VulkanGPUs and creates a logical device to interface with it
        /// </summary>
        /// <exception cref="InitializationException"></exception>
        private static void CreateLogicalDevice()
        {
            VkGPUInfo GPU = VkGPU.SelectGPU();

            var queueCreateInfos = GPU.GetQueuesToCreate();

            DeviceCreateInfo deviceCreateInfo = new DeviceCreateInfo();
            deviceCreateInfo.SType = StructureType.DeviceCreateInfo;
            deviceCreateInfo.QueueCreateInfoCount = 1;
            fixed (DeviceQueueCreateInfo* ptr = queueCreateInfos)
            {
                deviceCreateInfo.PQueueCreateInfos = ptr;
            }
            deviceCreateInfo.EnabledExtensionCount = (uint)requiredDeviceExtensions.Length;
            requiredDeviceExtensionsPtr = (byte**)SilkMarshal.StringArrayToPtr(requiredDeviceExtensions);
            deviceCreateInfo.PpEnabledExtensionNames = requiredDeviceExtensionsPtr;

            Result result = vk.CreateDevice(GPU.Device, in deviceCreateInfo, null, out internalVkDevice);
            GPU.GetCreatedQueueIndices(internalVkDevice);

            if (result != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan logical device!");
            }
            CurrentGPU = GPU;
        }
        #endregion

        #region swap chain creation
        /// <summary>
        /// The min amount of images in the swap chain
        /// </summary>
        public static uint SwapChainImages
        {
            get
            {
                return internalSwapChainImages;
            }
            set
            {
                internalSwapChainImages = value;
            }
        }
        private static uint internalSwapChainImages = 0;
        public static PresentModeKHR PreferredPresentMode
        {
            get
            {
                return internalPreferredPresentMode;
            }
            set
            {
                if (initialized)
                {
                    throw new InitializationException("Cannot change Vulkan present mode while app has been initialized!");
                }
                internalPreferredPresentMode = value;
            }
        }
        private static SwapChain swapChain;
        private static PresentModeKHR internalPreferredPresentMode = PresentModeKHR.MailboxKhr;
        public static void CreateSwapChain(Window window)
        {
            vk.TryGetDeviceExtension(vkInstance, internalVkDevice, out KhrSwapchainAPI);
            swapChain = SwapChain.Create(window);
            /*SwapChainSupportDetails swapChainSupport = SwapChain.QuerySwapChainSupport(CurrentGPU.Device);

            SurfaceFormatKHR surfaceFormat = SwapChain.FindSurfaceWith(ColorSpaceKHR.PaceSrgbNonlinearKhr, Format.B8G8R8A8Srgb, swapChainSupport.supportedSurfaceFormats);
            
            if (internalSwapChainImages <= swapChainSupport.Capabilities.MinImageCount)
            {
                internalSwapChainImages = swapChainSupport.Capabilities.MinImageCount + 1;
            }

            swapChain = new SwapChain(
                swapChainSupport,
                internalSwapChainImages,
                surfaceFormat.Format,
                surfaceFormat.ColorSpace,
                window.GetSwapChainExtents(swapChainSupport.Capabilities),
                internalPreferredPresentMode,
                WindowSurface);
            
            swapChain.Recreate(swapChainSupport);*/

        }
        #endregion

        #region render pass
        public static VkRenderPass renderPass;

        public static void CreateRenderPass()
        {
            renderPass = VkRenderPass.Create(swapChain, ImageLayout.ColorAttachmentOptimal);
        }
        #endregion

        #region pipelines
        private static void CreatePipelines(Window window)
        {
            testShader = VkShader.Create("Content/Vertex.spv", "Content/Fragment.spv");
            TrianglePipeline = new VkGraphicsPipeline(
                new Viewport(0, 0, swapChain.imageExtents.Width, swapChain.imageExtents.Height, 0f, 1f),
                new Rect2D(default(Offset2D), swapChain.imageExtents),
                FrontFace.CounterClockwise, 
                CullModeFlags.None,
                BlendState.AlphaBlend,
                PrimitiveTopology.TriangleList,
                PolygonMode.Fill,
                renderPass);

            TrianglePipeline.shaderStages = new PipelineShaderStageCreateInfo[]
            {
                VkGraphicsPipeline.CreateShaderStage(ShaderStageFlags.VertexBit, testShader.vertexShader),
                VkGraphicsPipeline.CreateShaderStage(ShaderStageFlags.FragmentBit, testShader.fragmentShader)
            };

            TrianglePipeline.BuildPipeline();

            /*pipeline.shaderStages = new PipelineShaderStageCreateInfo[]
            {
                VkGraphicsPipeline.CreateShaderStage(ShaderStageFlags.VertexBit, testShader.vertexShader),
                VkGraphicsPipeline.CreateShaderStage(ShaderStageFlags.FragmentBit, testShader.fragmentShader)
            };

            TrianglePipelineLayout = VkPipelineLayout.Create();

            var pipelineInfo = VkGraphicsPipeline.CreateInfo(BlendState.AlphaBlend, PrimitiveTopology.TriangleList, PolygonMode.Fill, renderPass, TrianglePipelineLayout);

            fixed (Pipeline* ptr = &internalTrianglePipeline)
            {
                if (vk.CreateGraphicsPipelines(vkDevice, default, 1, pipelineInfo, null, ptr) != Result.Success)
                {
                    throw new InitializationException("Error creating Vulkan Graphics Pipeline!");
                }
            }*/
        }
        #endregion

        #region command pools(memory) and buffers
        static CommandPoolCreateInfo poolCreateInfo;
        static CommandPool commandPool;
        static VkCommandBuffer commandBuffer;
        public static void CreateCommandPool()
        {
            poolCreateInfo = new CommandPoolCreateInfo();
            poolCreateInfo.SType = StructureType.CommandPoolCreateInfo;
            //we reset our command buffers every frame individually, so use this
            poolCreateInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;
            //TODO: update to specific queues
            poolCreateInfo.QueueFamilyIndex = CurrentGPU.queueInfo.GetGeneralPurposeQueue(CurrentGPU.Device)!.Value;

            fixed (CommandPool* ptr = &commandPool)
            {
                if (vk.CreateCommandPool(vkDevice, in poolCreateInfo, null, ptr) != Result.Success)
                {
                    throw new InitializationException("Failed to create Vulkan Command Pool(Memory)!");
                }
            }
        }
        public static void CreateCommandBuffer()
        {
            commandBuffer = VkCommandBuffer.Create(commandPool, CommandBufferLevel.Primary);
        }
        #endregion

        #region synchronization
        public static Silk.NET.Vulkan.Semaphore presentSemaphore;
        public static Silk.NET.Vulkan.Semaphore renderSemaphore;
        public static Fence fence;

        private static SemaphoreCreateInfo semaphoreCreateInfo;
        private static FenceCreateInfo fenceCreateInfo;

        public static void CreateSynchronizers()
        {
            semaphoreCreateInfo = new SemaphoreCreateInfo();
            semaphoreCreateInfo.SType = StructureType.SemaphoreCreateInfo;
            fenceCreateInfo = new FenceCreateInfo();
            fenceCreateInfo.Flags = FenceCreateFlags.SignaledBit;
            fenceCreateInfo.SType = StructureType.FenceCreateInfo;

            presentSemaphore = CreateSemaphore();
            renderSemaphore = CreateSemaphore();

            fence = CreateFence();
        }
        public static Silk.NET.Vulkan.Semaphore CreateSemaphore()
        {
            Silk.NET.Vulkan.Semaphore result;
            if (vk.CreateSemaphore(vkDevice, in semaphoreCreateInfo, null, &result) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Semaphore!");
            }
            return result;
        }
        public static Fence CreateFence()
        {
            Fence result;
            if (vk.CreateFence(vkDevice, in fenceCreateInfo, null, &result) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Fence!");
            }
            return result;
        }
        #endregion

        public static void Shutdown()
        {
            if (initialized)
            {
                CurrentGPU = default;

                //we need this so vulkan doesnt attempt to destroy the engine while
                //something is still presenting
                vk.WaitForFences(vkDevice, 1, in fence, new Bool32(true), uint.MaxValue);

                renderPass.Dispose();
                vk.DestroySemaphore(vkDevice, presentSemaphore, null);
                vk.DestroySemaphore(vkDevice, renderSemaphore, null);
                vk.DestroyFence(vkDevice, fence, null);
                vk.DestroyCommandPool(vkDevice, commandPool, null);
                //vk.DestroyPipelineLayout(vkDevice, TrianglePipelineLayout, null);
                TrianglePipeline.Dispose();
                swapChain.Dispose();
                testShader.Dispose();
                vk.DestroyDevice(vkDevice, null);
                KhrSurfaceAPI.DestroySurface(vkInstance, internalWindowSurface, null);
                if (ValidationLayersActive)
                {
                    VkDebug.DestroyDebugMessenger();
                }
                vk.DestroyInstance(vkInstance, null);
                KhrSwapchainAPI.Dispose();
                KhrSurfaceAPI.Dispose();
                vk.Dispose();

                if (validationLayersPtr != IntPtr.Zero)
                {
                    SilkMarshal.Free((nint)validationLayersPtr);
                }
                if (requiredInstanceExtensionsPtr != null)
                {
                    SilkMarshal.Free((nint)requiredInstanceExtensionsPtr);
                }
                Marshal.FreeHGlobal(appNamePtr);
                Marshal.FreeHGlobal(engineNamePtr);
                SilkMarshal.Free((nint)requiredDeviceExtensionsPtr);
                VkShader.FreeMainStringPointer();

                initialized = false;
            }
        }
    }
}