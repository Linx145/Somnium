#if VULKAN
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Linq;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Collections.Generic;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VkEngine
    {
        public static int begunPipelines = 0;

        public const uint maxDescriptorSets = 1024;
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
        public static Window window;
        public static VkGPU CurrentGPU { get; private set; }
        //public static VkGraphicsPipeline TrianglePipeline;

        private static Device internalVkDevice;
        internal static KhrSurface KhrSurfaceAPI;
        internal static KhrSwapchain KhrSwapchainAPI;
        private static SurfaceKHR internalWindowSurface;

        private static GenerationalArray<VkGraphicsPipeline> pipelines = new GenerationalArray<VkGraphicsPipeline>(null);
        private static GenerationalArray<VkGraphicsPipeline> renderbufferPipelines = new GenerationalArray<VkGraphicsPipeline>(null);
        public static VkFrameData[] frames;

        public static HashSet<ulong> allResourceBuffers = new HashSet<ulong>();

        public static CommandCollection commandBuffer
        {
            get
            {
                return frames[window.frameNumber].commandBuffer;
            }
        }
        public static ref Fence fence
        {
            get
            {
                return ref frames[window.frameNumber].fence;
            }
        }
        public static ref Semaphore awaitPresentCompleteSemaphore
        {
            get
            {
                return ref frames[window.frameNumber].presentSemaphore;
            }
        }
        public static ref Semaphore awaitRenderCompleteSemaphore
        {
            get
            {
                return ref frames[window.frameNumber].renderSemaphore;
            }
        }
        public static UniformBuffer unifiedDynamicBuffer
        {
            get
            {
                return frames[window.frameNumber].unifiedDynamicBuffer;
            }
        }

        public static bool initialized { get; private set; }
        internal static bool recreatedSwapChainThisFrame = false;
        private static bool onResized;
        public static unsafe void Initialize(Window forWindow, string AppName, bool enableValidationLayers = true)
        {
            window = forWindow;
            if (!initialized)
            {
                internalEnableValidationLayers = enableValidationLayers;
                window.onResized += OnResized;
                window.onMoved += OnMoved;

                initialized = true;
                appName = AppName;

                vk = Vk.GetApi();

                CreateInstance();
                if (ValidationLayersActive)
                {
                    VkDebug.InitializeDebugMessenger(window);
                }
                CreateSurface();
                CreateLogicalDevice();
                CreateCommandPool(window.application);
                CreateSwapChain(); //also creates image views
                //CreateRenderPasses();
                //VertexDeclaration.RegisterAllVertexDeclarations(Backends.Vulkan);
                //CreatePipelines(window);
                swapChain.RecreateFramebuffers();//renderPass);
                CreateFrames(window.application);
                //CreateCommandBuffer(window.application);
                //CreateSynchronizers();
            }
        }
        public static void BeginDraw()
        {
            //wait for the previous frame to finish drawing
            vk.WaitForFences(vkDevice, 1, in fence, new Bool32(true), ulong.MaxValue);
            vk.ResetFences(vkDevice, 1, in fence);
            recreatedSwapChainThisFrame = false;
            
            //swap the swapchain backbuffers as long as the previous frame has finished presenting
            bool swapchainRecreated = swapChain.SwapBuffers(awaitPresentCompleteSemaphore, default);
            if (swapchainRecreated)
            {
                onResized = false;
                recreatedSwapChainThisFrame = true;
                return;
            }

            commandBuffer.Reset();
            commandBuffer.Begin();

            //because we presented last frame/this is the first frame, the swapchain image would
            //either be in layouts undefined or present_src_khr, so we now need to transition it back
            //into color attachment optimal for use in drawing again.
            //UPDATE: This is now handled by the renderpass setting
        }
        internal static void EndDraw(Application application)
        {
            if (application.Graphics.currentPipeline != null)
            {
                throw new ExecutionException("Draw ended while a pipeline is still active!");
            }
            if (activeRenderPass != null)
            {
                if (activeRenderPass.begun)
                {
                    activeRenderPass.End(commandBuffer);
                }
                activeRenderPass = null;
            }
            application.Graphics.currentRenderbuffer = null;
            //reset the parameters of all shaders in pipelines
            //since pipeline shader is shared identically between
            //pipelines and renderbufferPipelines, only need to clear once for pipelines (for now)
            for (int i = 0; i < pipelines.internalArray.Length; i++)
            {
                if (pipelines.internalArray[i] != null)
                {
                    var pipeline = pipelines.internalArray[i];
                    if (pipeline.shaders != null && pipeline.shaders.Length > 0)
                    {
                        for (int j = 0; j < pipeline.shaders.Length; j++)
                        {
                            pipeline.shaders[j].descriptorForThisDrawCall = 0;
                        }
                    }
                }
            }

            commandBuffer.End();
            SubmitToGPU();
        }
        public static void SubmitToGPU()
        {
            if (begunPipelines > 0)
            {
                throw new ExecutionException("Vulkan draw loop ended but a Pipeline State is still bound! Check that all Pipeline States have had End() called.");
            }

            //submit what we rendered to the GPU
            SubmitInfo submitInfo = new SubmitInfo();
            submitInfo.SType = StructureType.SubmitInfo;

            PipelineStageFlags waitFlag = PipelineStageFlags.ColorAttachmentOutputBit;
            submitInfo.PWaitDstStageMask = &waitFlag;

            //flag previous presentation to complete before we submit the queue
            submitInfo.WaitSemaphoreCount = 1;
            fixed (Semaphore* ptr = &awaitPresentCompleteSemaphore)
            {
                submitInfo.PWaitSemaphores = ptr;
            }
            //flag awaitRenderCompleteSemaphore as complete on completing our queue shenanigans
            submitInfo.SignalSemaphoreCount = 1;
            fixed (Semaphore* ptr = &awaitRenderCompleteSemaphore)
            {
                submitInfo.PSignalSemaphores = ptr;
            }

            submitInfo.CommandBufferCount = 1;
            CommandBuffer cmdBuffer = new CommandBuffer(commandBuffer.handle);
            submitInfo.PCommandBuffers = &cmdBuffer;

            //lock it until the queue is finished submitting (next frame)
            CurrentGPU.AllPurposeQueue.externalLock.EnterWriteLock();
            if (vk.QueueSubmit(CurrentGPU.AllPurposeQueue, 1, in submitInfo, fence) != Result.Success)
            {
                throw new ExecutionException("Error submitting Vulkan Queue!");
            }
            CurrentGPU.AllPurposeQueue.externalLock.ExitWriteLock();

            //and present it to the window
            PresentInfoKHR presentInfo = new PresentInfoKHR();
            presentInfo.SType = StructureType.PresentInfoKhr;
            presentInfo.SwapchainCount = 1;
            fixed (SwapchainKHR* ptr = &swapChain.handle)
            {
                presentInfo.PSwapchains = ptr;
            }
            //wait for the render to be completed before we present
            presentInfo.WaitSemaphoreCount = 1;
            fixed (Semaphore* ptr = &awaitRenderCompleteSemaphore)
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
                    SwapChain.Create(window);
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
        public static void OnMoved(Window window, int x, int y)
        {

        }
        #region instance creation
        /// <summary>
        /// Creates the Vulkan instance to run things with
        /// </summary>
        /// <param name="window">The window that should be used</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InitializationException"></exception>
        private static void CreateInstance()
        {
            ApplicationInfo vkApplicationInfo = new ApplicationInfo();
            vkApplicationInfo.SType = StructureType.ApplicationInfo;

            vkApplicationInfo.PApplicationName = Utils.StringToBytePtr(appName, out appNamePtr);
            vkApplicationInfo.PEngineName = Utils.StringToBytePtr(EngineName, out engineNamePtr);
            vkApplicationInfo.ApplicationVersion = new Version32(1, 0, 0);
            vkApplicationInfo.EngineVersion = new Version32(0, 1, 0);
            vkApplicationInfo.ApiVersion = Vk.Version13;

            InstanceCreateInfo vkInstanceCreateInfo = new InstanceCreateInfo();
            vkInstanceCreateInfo.SType = StructureType.InstanceCreateInfo;
            vkInstanceCreateInfo.PApplicationInfo = &vkApplicationInfo;

            //get the required window extensions needed to hook the app in, plus additional stuff to get
            //optional modules like Validation Layer Debug Messengers up and running
            requiredInstanceExtensions = GetRequiredInstanceExtensions();
            
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
        private static void CreateSurface()
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
                    string str = Marshal.PtrToStringAnsi((IntPtr)supportedNamePtr);

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
        private static string[] GetRequiredInstanceExtensions()
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
        public static SwapChain swapChain;
        private static PresentModeKHR internalPreferredPresentMode = PresentModeKHR.MailboxKhr;
        public static void CreateSwapChain()
        {
            if (KhrSwapchainAPI == null)
            {
                vk.TryGetDeviceExtension(vkInstance, internalVkDevice, out KhrSwapchainAPI);
            }
            SwapChain.Create(window);
        }
        #endregion

        /*public static VkRenderPass renderPass;
        public static VkRenderPass framebufferRenderPass;

        public static void CreateRenderPasses()
        {
            renderPass = VkRenderPass.Create(swapChain.imageFormat, ImageLayout.ColorAttachmentOptimal, AttachmentLoadOp.Load, AttachmentStoreOp.Store, DepthFormat.Depth32, ImageLayout.PresentSrcKhr);
            framebufferRenderPass = VkRenderPass.Create(Format.R8G8B8A8Unorm, ImageLayout.ColorAttachmentOptimal, AttachmentLoadOp.Load, AttachmentStoreOp.Store, DepthFormat.Depth32, ImageLayout.ShaderReadOnlyOptimal);
        }
        public static void SetRenderPass(VkRenderPass renderPass, RenderBuffer renderBuffer)
        {
            currentRenderPass = renderPass;
            renderPass.Begin(commandBuffer, swapChain, renderBuffer);
        }*/

        #region render passes
        public static VkRenderPass activeRenderPass = null;
        public static VkRenderPass GetRenderPass(RenderBuffer renderBuffer = null)
        {
            Format imageFormat;
            DepthFormat depthFormat;
            ImageLayout finalLayout;

            if (renderBuffer != null)
            {
                imageFormat = Converters.ImageFormatToVkFormat[(int)renderBuffer.backendTexture.imageFormat];//application.Graphics.currentRenderbuffer.backendTexture.imageFormat;
                depthFormat = renderBuffer.depthBuffer == null ? DepthFormat.None : renderBuffer.depthBuffer.depthFormat;
                finalLayout = ImageLayout.ColorAttachmentOptimal; //ImageLayout.ShaderReadOnlyOptimal;
            }
            else
            {
                imageFormat = swapChain.imageFormat;
                depthFormat = swapChain.depthFormat;
                finalLayout = ImageLayout.PresentSrcKhr;
            }

            var renderPass = VkRenderPass.GetOrCreate(imageFormat, finalLayout, depthFormat);
            return renderPass;
        }
        public static VkRenderPass GetCurrentRenderPass(Application application) => GetRenderPass(application.Graphics.currentRenderbuffer);
        
        public static void SetRenderPass(VkRenderPass renderPass, RenderBuffer renderBuffer)
        {
            activeRenderPass = renderPass;
            renderPass.Begin(commandBuffer, swapChain, renderBuffer);
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
        //public static VkGraphicsPipeline GetRenderbufferPipeline(GenerationalIndex index) => renderbufferPipelines.Get(index);
        #endregion

        #region command pools(memory)

        public static CommandRegistrar commandPool;
        public static CommandRegistrar transientTransferCommandPool;
        public static CommandRegistrar transientGraphicsCommandPool;

        //public static CommandCollection commandBuffer;

        public static void CreateCommandPool(Application application)
        {
            //commandPool = new CommandRegistrar(application, false, CommandQueueType.GeneralPurpose);
            transientGraphicsCommandPool = new CommandRegistrar(application, true, CommandQueueType.Graphics);
            transientTransferCommandPool = new CommandRegistrar(application, true, CommandQueueType.Transfer);
        }
        /*public static void CreateCommandBuffer(Application application)
        {
            commandBuffer = new CommandCollection(application, commandPool);
        }*/
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
        public static void EndTransientCommandBuffer(VkCommandQueue queueToSubmitTo, CommandBuffer commandBuffer, CommandPool commandPool)
        {
            vk.EndCommandBuffer(commandBuffer);

            SubmitInfo submitInfo = new SubmitInfo();
            submitInfo.SType = StructureType.SubmitInfo;
            submitInfo.CommandBufferCount = 1;
            submitInfo.PCommandBuffers = &commandBuffer;

            //wait for rendering to complete if there is rendering ongoing
            //this is needed if the rendering is on a different thread as potential asset(texture) loading operations
            //do not reset fence here
            //vk.ResetFences(vkDevice, 1, in fence);

            queueToSubmitTo.externalLock.EnterWriteLock();
            if (vk.QueueSubmit(queueToSubmitTo, 1, in submitInfo, new Fence(null)) != Result.Success)
            {
                throw new ExecutionException("Error submitting transfer queue!");
            }
            //wait for transient command buffer to finish doing work on queue
            vk.QueueWaitIdle(queueToSubmitTo);
            queueToSubmitTo.externalLock.ExitWriteLock();

            //finally, delete our transient command buffer
            vk.FreeCommandBuffers(vkDevice, commandPool, 1, in commandBuffer);
        }
        #endregion

        #region simultaneous frames
        public static void CreateFrames(Application application)
        {
            frames = new VkFrameData[Application.Config.maxSimultaneousFrames];
            for (int i = 0; i < Application.Config.maxSimultaneousFrames; i++)
            {
                frames[i] = new VkFrameData(application);
            }
        }
        #endregion

        #region Resource Buffers
        public static unsafe Buffer CreateResourceBuffer(ulong size, BufferUsageFlags usageFlags)
        {
            if (size == 0)
            {
                throw new ArgumentOutOfRangeException("Size of buffer created cannot be zero!");
            }
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
            allResourceBuffers.Add(buffer.Handle);
            return buffer;
        }
        public static void DestroyResourceBuffer(Buffer buffer)
        {
            if (allResourceBuffers.Contains(buffer.Handle))
            {
                allResourceBuffers.Remove(buffer.Handle);
                vk.DestroyBuffer(VkEngine.vkDevice, buffer, null);
            }
            else throw new InvalidOperationException("Error! Buffer was not created/registered via VkEngine.CreateResourceBuffer!");
        }
        /// <summary>
        /// Copies a resource(AKA Vertex, index etc etc) buffer by creating a command buffer, sending a command, submitting it and deleting it.
        /// Thus, not to be called every frame.
        /// </summary>
        public static void StaticCopyResourceBuffer(Application application, Buffer from, Buffer to, ulong copySize)
        {
            var transientBuffer = CreateTransientCommandBuffer(true);

            var bufferCopyInfo = new BufferCopy(0, 0, copySize);
            vk.CmdCopyBuffer(transientBuffer, from, to, 1, in bufferCopyInfo);

            EndTransientCommandBuffer(CurrentGPU.DedicatedTransferQueue, transientBuffer, new CommandPool(transientTransferCommandPool.handle));
        }
        /*public static ulong GetSafeUniformBufferSize(ulong originalSize)
        {
            var minUniformBufferAlignment = CurrentGPU.limits.minUniformBufferOffsetAlignment;
            var alignedSize = originalSize;
            if (minUniformBufferAlignment > 0)
            {
                alignedSize = (alignedSize + minUniformBufferAlignment - 1) & ~(minUniformBufferAlignment - 1);
            }
            return alignedSize;
        }*/
        #endregion

        #region synchronization
        private static SemaphoreCreateInfo semaphoreCreateInfo;
        private static FenceCreateInfo fenceCreateInfo;
        public static Semaphore CreateSemaphore()
        {
            if (semaphoreCreateInfo.SType == 0)
            {
                semaphoreCreateInfo = new SemaphoreCreateInfo();
                semaphoreCreateInfo.SType = StructureType.SemaphoreCreateInfo;
            }

            Semaphore result;
            if (vk.CreateSemaphore(vkDevice, in semaphoreCreateInfo, null, &result) != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan Semaphore!");
            }
            return result;
        }
        public static Fence CreateFence()
        {
            if (fenceCreateInfo.SType == 0)
            {
                fenceCreateInfo = new FenceCreateInfo();
                fenceCreateInfo.SType = StructureType.FenceCreateInfo;
                fenceCreateInfo.Flags = FenceCreateFlags.SignaledBit;
            }

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
            uint maxUniformDescriptors = VkEngine.maxDescriptorSets * Application.Config.maxSimultaneousFrames;

            if (descriptorPool.Handle != 0) return descriptorPool;
            DescriptorPoolSize* poolSizes = stackalloc DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = maxUniformDescriptors
                },
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.CombinedImageSampler,
                    DescriptorCount = maxUniformDescriptors
                }
            };
            
            DescriptorPoolCreateInfo createInfo = new DescriptorPoolCreateInfo();
            createInfo.SType = StructureType.DescriptorPoolCreateInfo;
            createInfo.PoolSizeCount = 2; //the amount of areas in the descriptor to allocate and their respective descriptor counts
            createInfo.PPoolSizes = poolSizes;
            createInfo.MaxSets = maxUniformDescriptors; //the maximum number of descriptor sets that can be allocated from the pool.

            DescriptorPool newPool;
            //create descriptor pool(s)
            if (vk.CreateDescriptorPool(vkDevice, in createInfo, null, &newPool) != Result.Success)
            {
                throw new InitializationException("Failed to create descriptor pool!");
            }
            descriptorPool = newPool;

            return descriptorPool;
        }
        #endregion

        #region images
        public static void AwaitImageFinishModifying(Texture2D texture)
        {
            ImageMemoryBarrier barrier = new ImageMemoryBarrier();
            barrier.SType = StructureType.ImageMemoryBarrier;
            barrier.PNext = null;
            var srcStageMask = PipelineStageFlags.ColorAttachmentOutputBit;
            barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
            var dstStageMask = PipelineStageFlags.FragmentShaderBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            barrier.OldLayout = texture.imageLayout;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;//renderBuffer.backendTexture.imageLayout;

            barrier.Image = new Image(texture.imageHandle);
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
            barrier.SubresourceRange.BaseMipLevel = 0;
            barrier.SubresourceRange.LevelCount = 1;
            barrier.SubresourceRange.BaseArrayLayer = 0;
            barrier.SubresourceRange.LayerCount = 1;

            vk.CmdPipelineBarrier(new CommandBuffer(commandBuffer.handle), srcStageMask, dstStageMask, DependencyFlags.None, 0, null, 0, null, 1, &barrier);

            texture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
        }
        public static bool TransitionImageLayout(Texture2D image, ImageAspectFlags aspectFlags, ImageLayout newLayout, CommandBuffer bufferToUse)
        {
            if (image.imageLayout == newLayout)
            {
                return false;
            }
            TransitionImageLayout(new Image(image.imageHandle), aspectFlags, image.imageLayout, newLayout, bufferToUse);
            return true;
        }
        public static void TransitionImageLayout(Image image, ImageAspectFlags aspectFlags, ImageLayout oldLayout, ImageLayout newLayout, CommandBuffer bufferToUse)
        {
            ImageMemoryBarrier barrier = new ImageMemoryBarrier();
            barrier.SType = StructureType.ImageMemoryBarrier;
            barrier.OldLayout = oldLayout;
            barrier.NewLayout = newLayout;

            //we are not using the barrier to transfer queue family ownership, so we set these to ignored
            barrier.SrcQueueFamilyIndex = 0;
            barrier.DstQueueFamilyIndex = 0;

            barrier.Image = image;
            barrier.SubresourceRange.AspectMask = aspectFlags;// ImageAspectFlags.ColorBit;
            barrier.SubresourceRange.BaseMipLevel = 0;
            barrier.SubresourceRange.LevelCount = 1;
            barrier.SubresourceRange.BaseArrayLayer = 0;
            barrier.SubresourceRange.LayerCount = 1;

            PipelineStageFlags sourceStage = PipelineStageFlags.TopOfPipeBit;
            PipelineStageFlags destinationStage = PipelineStageFlags.BottomOfPipeBit;

            VkCommandQueue queue;

            queue = CurrentGPU.AllPurposeQueue;

            CommandPool poolToUse = default;
            bool usingTransientBuffer = false;
            if (bufferToUse.Handle == 0)
            {
                poolToUse = new CommandPool(transientGraphicsCommandPool.handle);
                bufferToUse = CreateTransientCommandBuffer(true, true);
                usingTransientBuffer = true;
            }
            //commandPool = new CommandPool(transientGraphicsCommandPool.handle);

            PipelineStageFlags depthStageMask = PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit | 0;

            PipelineStageFlags sampledStageMask = PipelineStageFlags.VertexShaderBit | PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.ComputeShaderBit;

            //CREDIT: BGFX, because I actually have no clue how to map these
            switch (oldLayout)
            {
                case ImageLayout.Undefined:
                    break;

                case ImageLayout.General:
                    sourceStage = PipelineStageFlags.AllCommandsBit;
                    barrier.SrcAccessMask = AccessFlags.MemoryWriteBit;
                    break;

                case ImageLayout.ColorAttachmentOptimal:
                    sourceStage = PipelineStageFlags.ColorAttachmentOutputBit;
                    barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                    break;

                case ImageLayout.DepthStencilAttachmentOptimal:
                    sourceStage = depthStageMask;
                    barrier.SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
                    break;

                case ImageLayout.DepthStencilReadOnlyOptimal:
                    sourceStage = depthStageMask | sampledStageMask;
                    break;

                case ImageLayout.ShaderReadOnlyOptimal:
                    sourceStage = sampledStageMask;
                    break;

                case ImageLayout.TransferSrcOptimal:
                    sourceStage = PipelineStageFlags.TransferBit;
                    break;

                case ImageLayout.TransferDstOptimal:
                    sourceStage = PipelineStageFlags.TransferBit;
                    barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                    break;

                case ImageLayout.Preinitialized:
                    sourceStage = PipelineStageFlags.HostBit;
                    barrier.SrcAccessMask = AccessFlags.HostWriteBit;
                    break;

                case ImageLayout.PresentSrcKhr:
                    break;

                default:
                    throw new InvalidOperationException("Invalid source layout!");
            }

            switch (newLayout)
            {
                case ImageLayout.General:
                    destinationStage = PipelineStageFlags.AllCommandsBit;
                    barrier.DstAccessMask = AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit;
                    break;

                case ImageLayout.ColorAttachmentOptimal:
                    destinationStage = PipelineStageFlags.ColorAttachmentOutputBit;
                    barrier.DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit;
                    break;

                case ImageLayout.DepthStencilAttachmentOptimal:
                    destinationStage = depthStageMask;
                    barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;
                    break;

                case ImageLayout.DepthStencilReadOnlyOptimal:
                    destinationStage = depthStageMask | sampledStageMask;
                    barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.ShaderReadBit | AccessFlags.InputAttachmentReadBit;
                    break;

                case ImageLayout.ShaderReadOnlyOptimal:
                    destinationStage = PipelineStageFlags.FragmentShaderBit;
                    barrier.DstAccessMask = AccessFlags.ShaderReadBit | AccessFlags.InputAttachmentReadBit;
                    break;

                case ImageLayout.TransferSrcOptimal:
                    destinationStage = PipelineStageFlags.TransferBit;
                    barrier.DstAccessMask = AccessFlags.TransferReadBit;
                    break;

                case ImageLayout.TransferDstOptimal:
                    destinationStage = PipelineStageFlags.TransferBit;
                    barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                    break;

                case ImageLayout.PresentSrcKhr:
                    // vkQueuePresentKHR performs automatic visibility operations
                    break;

                default:
                    throw new InvalidOperationException("Invalid destination layout!");
            }

            ReadOnlySpan<ImageMemoryBarrier> span = stackalloc ImageMemoryBarrier[1] { barrier };

            vk.CmdPipelineBarrier(bufferToUse, sourceStage, destinationStage, DependencyFlags.None, null, null, span);

            if (usingTransientBuffer)
            {
                EndTransientCommandBuffer(queue, bufferToUse, poolToUse);
            }
            //EndTransientCommandBuffer(queue, transientBuffer, commandPool);
        }
        public static void StaticCopyImageToBuffer(Texture2D from, Buffer to)
        {
            var transientBuffer = CreateTransientCommandBuffer(true);

            BufferImageCopy bufferImageCopy = new BufferImageCopy();
            //we are also copying to a transient buffer
            bufferImageCopy.BufferOffset = 0;
            //only set values other than 0 if the image buffer is not tightly packed
            bufferImageCopy.BufferRowLength = 0;
            bufferImageCopy.BufferImageHeight = 0;

            bufferImageCopy.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
            bufferImageCopy.ImageSubresource.MipLevel = 0;
            bufferImageCopy.ImageSubresource.BaseArrayLayer = 0;
            bufferImageCopy.ImageSubresource.LayerCount = 1;

            bufferImageCopy.ImageOffset = default;
            bufferImageCopy.ImageExtent = new Extent3D((uint)from.Width, (uint)from.Height, 1);

            vk.CmdCopyImageToBuffer(
                transientBuffer,
                new Image(from.imageHandle),
                ImageLayout.TransferSrcOptimal,
                to,
                1,
                &bufferImageCopy
                );

            EndTransientCommandBuffer(CurrentGPU.DedicatedTransferQueue, transientBuffer, new CommandPool(transientTransferCommandPool.handle));
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
            bufferImageCopy.ImageExtent = new Extent3D(to.Width, to.Height, 1);

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
                VkRenderPass.DisposeAll();
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i].Dispose();
                }
                //commandPool.Dispose();
                transientTransferCommandPool.Dispose();
                transientGraphicsCommandPool.Dispose();
                //vk.DestroyCommandPool(vkDevice, new CommandPool(commandPool.handle), null);
                //vk.DestroyCommandPool(vkDevice, new CommandPool(transientTransferCommandPool.handle), null);
                //vk.DestroyCommandPool(vkDevice, new CommandPool(transientGraphicsCommandPool.handle), null);
                //vk.DestroyPipelineLayout(vkDevice, TrianglePipelineLayout, null);
                //TrianglePipeline.Dispose();
                swapChain.Dispose();
                //testShader.Dispose();
                foreach (ulong buffer in allResourceBuffers)
                {
                    vk.DestroyBuffer(vkDevice, new Buffer(buffer), null);
                }
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

                initialized = false;
            }
        }
    }
}
#endif