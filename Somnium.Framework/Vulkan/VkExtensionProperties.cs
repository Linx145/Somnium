using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Somnium.Framework.Vulkan
{
    public readonly struct VkExtensionProperties
    {
        public readonly string? ExtensionName;
        public readonly uint SpecVersion;

        public unsafe VkExtensionProperties(ExtensionProperties from)
        {
            ExtensionName = Marshal.PtrToStringAnsi((IntPtr)from.ExtensionName);
            SpecVersion = from.SpecVersion;
        }

        public override string ToString()
        {
            return string.Concat("Name: " + ExtensionName + ", Version: " + SpecVersion);
        }
    }
}
