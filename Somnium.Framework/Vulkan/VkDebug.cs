#if VULKAN
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System.Runtime.InteropServices;
using System;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VkDebug
    {
        static Window window;

        public static DebugUtilsMessageSeverityFlagsEXT MessageSeverity =
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;

        public static DebugUtilsMessageTypeFlagsEXT MessageTypes = 
            DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
            DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;

        public static ExtDebugUtils? debugUtils;
        private static DebugUtilsMessengerEXT messenger;

        public static void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = MessageSeverity;
            createInfo.MessageType = MessageTypes;
            createInfo.PfnUserCallback = (PfnDebugUtilsMessengerCallbackEXT)DebugCallback;
        }
        private static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            string? str = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);
            if (Application.Config.loggingMode == LoggingMode.VSDebug)
            {
                System.Diagnostics.Debug.WriteLine("Validation Layer: " + str);
            }
            else if (Application.Config.loggingMode == LoggingMode.Console)
            {
                Console.WriteLine(str);
            }

            if (Application.Config.throwValidationExceptions)
            {
                if ((messageSeverity | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt) != 0)
                {
                    throw new ExecutionException(str);
                }
            }

            return Vk.False;
        }
        internal static void InitializeDebugMessenger(Window appWindow)
        {
            window = appWindow;
            if (!VkEngine.vk.TryGetInstanceExtension(VkEngine.vkInstance, out debugUtils)) throw new InitializationException("Failed to initialize ExtDebugUtils!");

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            if (debugUtils!.CreateDebugUtilsMessenger(VkEngine.vkInstance, in createInfo, null, out messenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
            }
        }
        internal static void DestroyDebugMessenger()
        {
            debugUtils?.DestroyDebugUtilsMessenger(VkEngine.vkInstance, messenger, null);
            debugUtils?.Dispose();
        }
    }
}
#endif