using System;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Somnium.Framework.Windowing;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VulkanEngine
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
        private static Device internalVkDevice;
        
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
                    VulkanDebug.InitializeDebugMessenger();
                }
                CreateLogicalDevice();
            }
        }
        #region instance creation
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
            requiredExtensions = GetRequiredExtensions(window);
            
            vkInstanceCreateInfo.EnabledExtensionCount = (uint)requiredExtensions.Length;
            vkInstanceCreateInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(requiredExtensions);

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
                VulkanDebug.PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
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
        private static string[] requiredExtensions;
        private static string[] GetRequiredExtensions(Window window)
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

        public static VkExtensionProperties[] SupportedExtensions()
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
        private static void CreateLogicalDevice()
        {
            VulkanGPUInfo GPU = VulkanGPUs.SelectGPU();

            DeviceQueueCreateInfo queueCreateInfo = new DeviceQueueCreateInfo();
            queueCreateInfo.SType = StructureType.DeviceQueueCreateInfo;
            queueCreateInfo.QueueFamilyIndex = GPU.queueInfo.graphicsBitIndex!.Value;
            queueCreateInfo.QueueCount = 1;
            float queuePriority = 1f;
            queueCreateInfo.PQueuePriorities = &queuePriority;

            DeviceCreateInfo deviceCreateInfo = new DeviceCreateInfo();
            deviceCreateInfo.SType = StructureType.DeviceCreateInfo;
            deviceCreateInfo.QueueCreateInfoCount = 1;
            deviceCreateInfo.PQueueCreateInfos = &queueCreateInfo;

            Result result = vk.CreateDevice(GPU.Device, in deviceCreateInfo, null, out internalVkDevice);

            if (result != Result.Success)
            {
                throw new InitializationException("Failed to create Vulkan logical device!");
            }
        }
        #endregion

        public static void Shutdown()
        {
            if (initialized)
            {
                if (ValidationLayersActive)
                {
                    VulkanDebug.DestroyDebugMessenger();
                }
                vk.DestroyDevice(vkDevice, null);
                vk.DestroyInstance(vkInstance, null);
                vk.Dispose();
                
                if (validationLayersPtr != IntPtr.Zero)
                {
                    SilkMarshal.Free((nint)validationLayersPtr);
                }
                Marshal.FreeHGlobal(appNamePtr);
                Marshal.FreeHGlobal(engineNamePtr);

                initialized = false;
            }
        }
    }
}
