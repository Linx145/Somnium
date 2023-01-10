using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System.Runtime.InteropServices;

namespace Somnium.Framework.Vulkan
{
    public static unsafe class VulkanDebug
    {
        public enum Mode
        {
            /// <summary>
            /// Specifies that the output of the Debug should be written to Console via Console.WriteLine
            /// </summary>
            Console, 
            /// <summary>
            /// Specifies that the output of the Debug should be written to the Debug Output in VS via System.Diagnostics.WriteLine
            /// </summary>
            Output
        }

        public static DebugUtilsMessageSeverityFlagsEXT MessageSeverity =
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;

        public static DebugUtilsMessageTypeFlagsEXT MessageTypes = 
            DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
            DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;

        public static Mode WriteMode;

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
            if (WriteMode == Mode.Output)
            {
                System.Diagnostics.Debug.WriteLine("Validation Layer: " + str);
            }
            else
            {
                Console.WriteLine("Validation Layer: " + str);
            }

            return Vk.False;
        }
        internal static void InitializeDebugMessenger()
        {
            if (!VulkanEngine.vk.TryGetInstanceExtension(VulkanEngine.vkInstance, out debugUtils)) throw new InitializationException("Failed to initialize ExtDebugUtils!");

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            if (debugUtils!.CreateDebugUtilsMessenger(VulkanEngine.vkInstance, in createInfo, null, out messenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
            }
            Console.WriteLine("Debug Messenger setup");
        }
        internal static void DestroyDebugMessenger()
        {
            debugUtils?.DestroyDebugUtilsMessenger(VulkanEngine.vkInstance, messenger, null);
            debugUtils?.Dispose();
            Console.WriteLine("Debug messenger destroyed");
        }
    }
}
