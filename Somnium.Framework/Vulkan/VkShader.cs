using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    public unsafe class VkShader : IDisposable
    {
        private static Vk vk
        {
            get
            {
                return VkEngine.vk;
            }
        }
        private static Device device
        {
            get
            {
                return VkEngine.vkDevice;
            }
        }
        public ShaderModule vertexShader;
        public ShaderModule fragmentShader;

        public const string main = "main";
        private static byte* mainPtr;

        public static byte* Main()
        {
            if (mainPtr == (byte*)0)
            {
                mainPtr = (byte*)SilkMarshal.StringToPtr(main);
            }
            return mainPtr;
        }
        public static void FreeMainStringPointer()
        {
            if (mainPtr != (byte*)0)
            {
                SilkMarshal.Free((nint)mainPtr);
                mainPtr = (byte*)0;
            }
        }
        public static VkShader Create(string vertexShaderPath, string fragmentShaderPath)
        {
            return Create(File.ReadAllBytes(vertexShaderPath), File.ReadAllBytes(fragmentShaderPath));
        }
        public static VkShader Create(byte[] vertexShader, byte[] fragmentShader)
        {
            VkShader result = new VkShader();

            ShaderModule vertexShaderModule;
            ShaderModule fragmentShaderModule;

            ShaderModuleCreateInfo vCreateInfo = new ShaderModuleCreateInfo();
            vCreateInfo.SType = StructureType.ShaderModuleCreateInfo;
            vCreateInfo.CodeSize = (nuint)vertexShader.Length;
            fixed (byte* ptr = vertexShader)
            {
                vCreateInfo.PCode = (uint*)ptr;
                Result creationResult = vk.CreateShaderModule(device, in vCreateInfo, null, out vertexShaderModule);
                if (creationResult != Result.Success)
                {
                    throw new AssetCreationException("Failed to create vertex shader!");
                }
            }

            ShaderModuleCreateInfo fCreateInfo = new ShaderModuleCreateInfo();
            fCreateInfo.SType = StructureType.ShaderModuleCreateInfo;
            fCreateInfo.CodeSize = (nuint)fragmentShader.Length;
            fixed (byte* ptr = fragmentShader)
            {
                fCreateInfo.PCode = (uint*)ptr;
                Result creationResult = vk.CreateShaderModule(device, in fCreateInfo, null, out fragmentShaderModule);
                if (creationResult != Result.Success)
                {
                    throw new AssetCreationException("Failed to create fragment shader!");
                }
            }

            result.vertexShader = vertexShaderModule;
            result.fragmentShader = fragmentShaderModule;

            return result;
        }
        public void Dispose()
        {
            vk.DestroyShaderModule(device, fragmentShader, null);
            vk.DestroyShaderModule(device, vertexShader, null);
        }
    }
}
