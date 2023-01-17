using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Somnium.Framework.Windowing;
using System;
using System.Linq;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Collections.Concurrent;

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
        public static VkGPU CurrentGPU { get; private set; }
        //public static VkGraphicsPipeline TrianglePipeline;

        private static Device internalVkDevice;
        internal static KhrSurface KhrSurfaceAPI;
        internal static KhrSwapchain KhrSwapchainAPI;
        private static SurfaceKHR internalWindowSurface;

        private static GenerationalArray<VkGraphicsPipeline> pipelines = new GenerationalArray<VkGraphicsPipeline>(null);

        public static bool initialized { get; private set; }
        internal static bool recreatedSwapChainThisFrame = false;
        private static bool onResized;
        public static unsafe void Initialize(Window window, string AppName, bool enableValidationLayers = true)
        {
            if (!initialized)
            {
                window.OnResized += OnResized;

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
                //VertexDeclaration.RegisterAllVertexDeclarations(Backends.Vulkan);
                //CreatePipelines(window);
                swapChain.RecreateFramebuffers(renderPass);
                CreateCommandPool(window.application);
                CreateCommandBuffer(window.application);
                CreateSynchronizers();
            }
        }
        public static void BeginDraw(Window window)
        {
            vk.WaitForFences(vkDevice, 1, in fence, new Bool32(true), 1000000000);
            vk.ResetFences(vkDevice, 1, in fence);
            recreatedSwapChainThisFrame = false;

            SwapChain potentialNewSwapchain = swapChain.SwapBuffers(presentSemaphore, default);
            if (potentialNewSwapchain != null)
            {
                onResized = false;
                recreatedSwapChainThisFrame = true;
                swapChain = potentialNewSwapchain;
                return;
            }

            commandBuffer.Reset();
            commandBuffer.Begin();
            renderPass.Begin(commandBuffer, swapChain, window.clearColor);
            //TrianglePipeline.Bind(commandBuffer);
            //BeginRenderPass(); //also clears the screen

        }
        public static void EndDraw(Window window)
        {
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
            fixed (Semaphore* ptr = &presentSemaphore)
            {
                submitInfo.PWaitSemaphores = ptr;
            }

            submitInfo.SignalSemaphoreCount = 1;
            fixed (Semaphore* ptr = &renderSemaphore)
            {
                submitInfo.PSignalSemaphores = ptr;
            }

            submitInfo.CommandBufferCount = 1;
            CommandBuffer cmdBuffer = new CommandBuffer(commandBuffer.handle);
            submitInfo.PCommandBuffers = &cmdBuffer;

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
            //we cannot do this before QueuePresent if not the semaphores might not be in a consistent state,
            //resulting in a signaled semaphore being never waited upon
            if (presentResult == Result.ErrorOutOfDateKhr || onResized)
            {
                if (!recreatedSwapChainThisFrame)
                {
                    swapChain.Dispose();
                    swapChain = SwapChain.Create(window);
                }
                if (onResized)
                {
                    onResized = false;
                }
                if (presentResult == Result.ErrorOutOfDateKhr)
                {
                    presentResult = Result.Success;
                }
                //swapChain.Recreate(SwapChain.QuerySwapChainSupport(CurrentGPU.Device));
            }
            if (presentResult != Result.Success)
            {
                throw new ExecutionException("Error presenting Vulkan queue!");
            }
        }

        //Handle this manually as well for drivers that do not support auto OutOfDateKhr callbacks
        public static void OnResized(Window window, int width, int height)
        {
            onResized = true;
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
            VkGPU GPU = VkGPU.SelectGPU();

            var queueCreateInfos = GPU.GetQueuesToCreate();

            DeviceCreateInfo deviceCreateInfo = new DeviceCreateInfo();
            deviceCreateInfo.SType = StructureType.DeviceCreateInfo;
            deviceCreateInfo.QueueCreateInfoCount = 2;
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
            if (KhrSwapchainAPI == null)
            {
                vk.TryGetDeviceExtension(vkInstance, internalVkDevice, out KhrSwapchainAPI);
            }
            swapChain = SwapChain.Create(window);
        }
        #endregion

        #region render pass
        public static VkRenderPass renderPass;

        public static void CreateRenderPass()
        {
            renderPass = VkRenderPass.Create(swapChain.imageFormat, ImageLayout.ColorAttachmentOptimal, AttachmentLoadOp.Clear, AttachmentStoreOp.Store);
        }
        #endregion

        #region pipelines
        public static GenerationalIndex AddPipeline(VkGraphicsPipeline pipeline) => pipelines.Add(pipeline);
        public static void DestroyPipeline(GenerationalIndex index)
        {
            pipelines[index].Dispose();
            pipelines.Remove(index);
        }
        public static VkGraphicsPipeline GetPipeline(GenerationalIndex index) => pipelines.Get(index);
        #endregion

        #region command pools(memory) and command buffers

        static CommandRegistrar commandPool;
        static CommandRegistrar transientTransferCommandPool;
        static CommandRegistrar transientGraphicsCommandPool;

        public static CommandCollection commandBuffer;

        /*static CommandPool commandPool;

        static CommandPool transientTransferCommandPool;

        static CommandPool transientGraphicsCommandPool;

        public static VkCommandBuffer commandBuffer;*/

        public static void CreateCommandPool(Application application)
        {
            commandPool = new CommandRegistrar(application, false, CommandQueueType.GeneralPurpose);
            transientGraphicsCommandPool = new CommandRegistrar(application, true, CommandQueueType.Graphics);
            transientTransferCommandPool = new CommandRegistrar(application, true, CommandQueueType.Transfer);
            /*var poolCreateInfo = new CommandPoolCreateInfo();
            poolCreateInfo.SType = StructureType.CommandPoolCreateInfo;
            //we reset our command buffers every frame individually, so use this
            poolCreateInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;
            //TODO: update to specific queues
            poolCreateInfo.QueueFamilyIndex = CurrentGPU.queueInfo.GetGeneralPurposeQueue(CurrentGPU.Device)!.Value;

            fixed (CommandPool* ptr = &commandPool)
            {
                if (vk.CreateCommandPool(vkDevice, in poolCreateInfo, null, ptr) != Result.Success)
                {
                    throw new InitializationException("Failed to create general Vulkan Command Pool!");
                }
            }
            var transientTransferPoolCreateInfo = new CommandPoolCreateInfo();
            transientTransferPoolCreateInfo.SType = StructureType.CommandPoolCreateInfo;
            transientTransferPoolCreateInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit | CommandPoolCreateFlags.TransientBit;
            transientTransferPoolCreateInfo.QueueFamilyIndex = CurrentGPU.queueInfo.GetTransferQueue(CurrentGPU.Device)!.Value;

            fixed (CommandPool* ptr = &transientTransferCommandPool)
            {
                if (vk.CreateCommandPool(vkDevice, in transientTransferPoolCreateInfo, null, ptr) != Result.Success)
                {
                    throw new InitializationException("Failed to create transient transfer Vulkan Command Pool!");
                }
            }

            var transientGraphicsPoolCreateInfo = new CommandPoolCreateInfo();
            transientGraphicsPoolCreateInfo.SType = StructureType.CommandPoolCreateInfo;
            transientGraphicsPoolCreateInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit | CommandPoolCreateFlags.TransientBit;
            transientGraphicsPoolCreateInfo.QueueFamilyIndex = CurrentGPU.queueInfo.GetGeneralPurposeQueue(CurrentGPU.Device, mustPresent:false)!.Value;

            fixed (CommandPool* ptr = &transientGraphicsCommandPool)
            {
                if (vk.CreateCommandPool(vkDevice, in transientGraphicsPoolCreateInfo, null, ptr) != Result.Success)
                {
                    throw new InitializationException("Failed to create transient graphics Vulkan Command Pool!");
                }
            }*/
        }
        public static void CreateCommandBuffer(Application application)
        {
            commandBuffer = new CommandCollection(application, commandPool);
            //commandBuffer = VkCommandBuffer.Create(commandPool, CommandBufferLevel.Primary);
        }
        public static CommandBuffer CreateTransientCommandBuffer(bool alsoBeginBuffer, bool forGraphics = false)
        {
            CommandBufferAllocateInfo allocateInfo = new CommandBufferAllocateInfo();
            allocateInfo.SType = StructureType.CommandBufferAllocateInfo;
            allocateInfo.Level = CommandBufferLevel.Primary;
            allocateInfo.CommandPool = forGraphics ? new CommandPool(transientGraphicsCommandPool.handle) : new CommandPool(transientTransferCommandPool.handle);
            allocateInfo.CommandBufferCount = 1;

            CommandBuffer transientBuffer;
            vk.AllocateCommandBuffers(vkDevice, in allocateInfo, &transientBuffer);

            if (alsoBeginBuffer)
            {
                BeginTransientCommandBuffer(transientBuffer);
            }
            return transientBuffer;
        }
        public static void BeginTransientCommandBuffer(CommandBuffer commandBuffer)
        {
            CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo();
            beginInfo.SType = StructureType.CommandBufferBeginInfo;
            beginInfo.Flags = CommandBufferUsageFlags.OneTimeSubmitBit;

            vk.BeginCommandBuffer(commandBuffer, in beginInfo);
        }
        /// <summary>
        /// Submits and destroys the command buffer
        /// </summary>
        /// <param name="commandBuffer"></param>
        /// <exception cref="ExecutionException"></exception>
        public static void EndTransientCommandBuffer(Queue queueToSubmitTo, CommandBuffer commandBuffer, CommandPool commandPool)
        {
            vk.EndCommandBuffer(commandBuffer);

            SubmitInfo submitInfo = new SubmitInfo();
            submitInfo.SType = StructureType.SubmitInfo;
            submitInfo.CommandBufferCount = 1;
            submitInfo.PCommandBuffers = &commandBuffer;

            if (vk.QueueSubmit(queueToSubmitTo, 1, in submitInfo, new Fence(null)) != Result.Success)
            {
                throw new ExecutionException("Error submitting transfer queue!");
            }
            vk.QueueWaitIdle(queueToSubmitTo);

            //finally, delete our transient command buffer
            vk.FreeCommandBuffers(vkDevice, commandPool, 1, in commandBuffer);
        }
        #endregion

        #region Resource Buffers
        public static unsafe Buffer CreateResourceBuffer(ulong size, BufferUsageFlags usageFlags)
        {
            BufferCreateInfo createInfo = new BufferCreateInfo();
            createInfo.SType = StructureType.BufferCreateInfo;
            createInfo.Size = size;
            createInfo.Usage = usageFlags;
            createInfo.SharingMode = SharingMode.Exclusive;

            Buffer buffer;
            if (vk.CreateBuffer(vkDevice, &createInfo, null, &buffer) != Result.Success)
            {
                throw new AssetCreationException("Error creating Vulkan Buffer!");
            }
            return buffer;
        }
        /// <summary>
        /// Copies a resource(AKA Vertex, index etc etc) buffer by creating a command buffer, sending a command, submitting it and deleting it.
        /// Thus, not to be called every frame.
        /// </summary>
        public static void StaticCopyResourceBuffer(Application application, Buffer from, Buffer to, ulong copySize)
        {
            /*CommandBufferAllocateInfo allocateInfo = new CommandBufferAllocateInfo();
            allocateInfo.SType = StructureType.CommandBufferAllocateInfo;
            allocateInfo.Level = CommandBufferLevel.Primary;
            allocateInfo.CommandPool = transientCommandPool;
            allocateInfo.CommandBufferCount = 1;

            CommandBuffer transientBuffer;
            vk.AllocateCommandBuffers(vkDevice, in allocateInfo, &transientBuffer);

            CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo();
            beginInfo.SType = StructureType.CommandBufferBeginInfo;
            beginInfo.Flags = CommandBufferUsageFlags.OneTimeSubmitBit;

            vk.BeginCommandBuffer(transientBuffer, in beginInfo);*/
            var transientBuffer = CreateTransientCommandBuffer(true);

            var bufferCopyInfo = new BufferCopy(0, 0, copySize);
            vk.CmdCopyBuffer(transientBuffer, from, to, 1, in bufferCopyInfo);

            EndTransientCommandBuffer(CurrentGPU.DedicatedTransferQueue, transientBuffer, new CommandPool(transientTransferCommandPool.handle));
        }
        #endregion

        #region synchronization
        public static Semaphore presentSemaphore;
        public static Semaphore renderSemaphore;
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

        #region descriptor sets
        public static DescriptorPool descriptorPool;

        public static DescriptorPool GetOrCreateDescriptorPool()
        {
            if (descriptorPool.Handle != 0) return descriptorPool;
            DescriptorPoolSize* poolSizes = stackalloc DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = 4
                },
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 4
                }
            };
            
            DescriptorPoolCreateInfo createInfo = new DescriptorPoolCreateInfo();
            createInfo.SType = StructureType.DescriptorPoolCreateInfo;
            createInfo.PoolSizeCount = 2; //the amount of areas in the descriptor to allocate and their respective descriptor counts
            createInfo.PPoolSizes = poolSizes;
            createInfo.MaxSets = 4; //the maximum number of descriptor sets that can be allocated from the pool.

            DescriptorPool newPool;
            //create descriptor pool(s)
            if (vk.CreateDescriptorPool(vkDevice, in createInfo, null, &newPool) != Result.Success)
            {
                throw new InitializationException("Failed to create descriptor pool!");
            }
            descriptorPool = newPool;
            return descriptorPool;
        }
        /*public static SparseArray<DescriptorPool> descriptorPools = new SparseArray<DescriptorPool>(default);
        public static DescriptorPool GetOrCreateDescriptorPool(UniformType poolType)
        {
            if (descriptorPools.WithinLength((uint)poolType) && descriptorPools[(uint)poolType].Handle != 0)
            {
                return descriptorPools[(uint)poolType];
            }
            DescriptorPoolSize poolSize = new DescriptorPoolSize();
            poolSize.Type = ShaderParameter.UniformTypeToVkDescriptorType[(int)poolType];//DescriptorType.UniformBuffer;
            poolSize.DescriptorCount = 1; //specifies the number of descriptors of a given type which can be allocated in total from a given pool (across all descriptor sets).

            DescriptorPoolCreateInfo createInfo = new DescriptorPoolCreateInfo();
            createInfo.SType = StructureType.DescriptorPoolCreateInfo;
            createInfo.PoolSizeCount = 1; //the amount of descriptor pools to allocated
            createInfo.PPoolSizes = &poolSize;
            createInfo.MaxSets = 1; //the maximum number of descriptor sets that can be allocated from the pool.

            DescriptorPool descriptorPool;
            //create descriptor pool(s)
            if (vk.CreateDescriptorPool(vkDevice, in createInfo, null, &descriptorPool) != Result.Success)
            {
                throw new InitializationException("Failed to create descriptor pool!");
            }
            descriptorPools.Insert((uint)poolType, descriptorPool);
            return descriptorPool;
        }*/
        #endregion

        #region images
        public static void TransitionImageLayout(Texture2D texture, ImageLayout oldLayout, ImageLayout newLayout)
        {
            CommandBuffer transientBuffer;
            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                transientBuffer = CreateTransientCommandBuffer(true, false);
            }
            else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                transientBuffer = CreateTransientCommandBuffer(true, true);
            }
            else
            {
                throw new ExecutionException("Unsupported Vulkan image layout transition!");
            }
            //var transientBuffer = CreateTransientCommandBuffer(true);

            ImageMemoryBarrier barrier = new ImageMemoryBarrier();
            barrier.SType = StructureType.ImageMemoryBarrier;
            barrier.OldLayout = oldLayout;
            barrier.NewLayout= newLayout;

            //we are not using the barrier to transfer queue family ownership, so we set these to ignored
            barrier.SrcQueueFamilyIndex = 0;
            barrier.DstQueueFamilyIndex = 0;

            barrier.Image = new Image(texture.imageHandle);
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
            barrier.SubresourceRange.BaseMipLevel = 0;
            barrier.SubresourceRange.LevelCount = 1;
            barrier.SubresourceRange.BaseArrayLayer = 0;
            barrier.SubresourceRange.LayerCount = 1;

            PipelineStageFlags sourceStage;
            PipelineStageFlags destinationStage;

            Queue queue;
            CommandPool commandPool;

            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                //transfer can write without needing to wait on anything
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;

                sourceStage = PipelineStageFlags.TopOfPipeBit;
                destinationStage = PipelineStageFlags.TransferBit;

                queue = CurrentGPU.DedicatedTransferQueue;
                commandPool = new CommandPool(transientTransferCommandPool.handle);
            }
            else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                //shader reads should wait on transfer writes
                //(specifically the shader reads in the fragment shader,
                //because that's where we're going to use the texture, at least until VTF)
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;

                sourceStage = PipelineStageFlags.TransferBit;
                destinationStage = PipelineStageFlags.FragmentShaderBit;

                queue = CurrentGPU.AllPurposeQueue;
                commandPool = new CommandPool(transientGraphicsCommandPool.handle);
            }
            else
            {
                throw new ExecutionException("Unsupported Vulkan image layout transition!");
            }

            ReadOnlySpan<ImageMemoryBarrier> span = stackalloc ImageMemoryBarrier[1] { barrier };

            vk.CmdPipelineBarrier(transientBuffer, sourceStage, destinationStage, DependencyFlags.None, null, null, span);

            EndTransientCommandBuffer(queue, transientBuffer, commandPool);
        }
        public static void StaticCopyBufferToImage(Buffer from, Texture2D to)
        {
            var transientBuffer = CreateTransientCommandBuffer(true);

            BufferImageCopy bufferImageCopy = new BufferImageCopy();
            //Because we are copying from a transient buffer created specifically for this, the buffer offset is 0
            bufferImageCopy.BufferOffset = 0;
            //only set values other than 0 if the image buffer is not tightly packed
            bufferImageCopy.BufferRowLength = 0;
            bufferImageCopy.BufferImageHeight = 0;

            bufferImageCopy.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
            bufferImageCopy.ImageSubresource.MipLevel = 0;
            bufferImageCopy.ImageSubresource.BaseArrayLayer = 0;
            bufferImageCopy.ImageSubresource.LayerCount = 1;

            bufferImageCopy.ImageOffset = default;
            bufferImageCopy.ImageExtent = new Extent3D((uint)to.Width, (uint)to.Height, 1);

            vk.CmdCopyBufferToImage(
                transientBuffer,
                from,
                new Image(to.imageHandle),
                ImageLayout.TransferDstOptimal,
                1, //length of things to copy
                &bufferImageCopy
             );

            EndTransientCommandBuffer(CurrentGPU.DedicatedTransferQueue, transientBuffer, new CommandPool(transientTransferCommandPool.handle));
        }
        #endregion

        public static void Shutdown()
        {
            if (initialized)
            {
                SamplerState.DisposeDefaultSamplerStates();
                CurrentGPU = default;
                if (descriptorPool.Handle != 0)
                {
                    vk.DestroyDescriptorPool(vkDevice, descriptorPool, null);
                }
                //vk.WaitForFences(vkDevice, 1, in fence, new Bool32(true), uint.MaxValue);
                VkMemory.Dispose();
                renderPass.Dispose();
                vk.DestroySemaphore(vkDevice, presentSemaphore, null);
                vk.DestroySemaphore(vkDevice, renderSemaphore, null);
                vk.DestroyFence(vkDevice, fence, null);
                commandPool.Dispose();
                transientTransferCommandPool.Dispose();
                transientGraphicsCommandPool.Dispose();
                //vk.DestroyCommandPool(vkDevice, new CommandPool(commandPool.handle), null);
                //vk.DestroyCommandPool(vkDevice, new CommandPool(transientTransferCommandPool.handle), null);
                //vk.DestroyCommandPool(vkDevice, new CommandPool(transientGraphicsCommandPool.handle), null);
                //vk.DestroyPipelineLayout(vkDevice, TrianglePipelineLayout, null);
                //TrianglePipeline.Dispose();
                swapChain.Dispose();
                //testShader.Dispose();
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
