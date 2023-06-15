#if WGPU
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using Silk.NET.Core.Native;
using System.Numerics;
using Silk.NET.GLFW;
using System;

namespace Somnium.Framework.WGPU
{
    public static unsafe class WGPUEngine
    {
        public static WebGPU wgpu = null;
        internal static Instance* instance;
        internal static Surface* surface;
        internal static Adapter* adapter;
        internal static Device* device;
        internal static Queue* queue;
        internal static SwapChain* swapChain;
        internal static TextureFormat swapChainFormat;
        //private static TextureFormat _SwapChainFormat;

        public static bool initialized { get; private set; }

        public static unsafe void Initialize(Window forWindow, string AppName, bool enableValidationLayers = true)
        {
            wgpu = WebGPU.GetApi();

            InstanceDescriptor instanceDescriptor = new InstanceDescriptor();
            instance = wgpu.CreateInstance(&instanceDescriptor);

            surface = WebGPUSurface.CreateWebGPUSurface(forWindow, wgpu, instance);//wgpu.InstanceCreateSurface(instance, &surfaceDescriptor);

            GetAdapter();
            GetDevice();
            GetQueue();

            wgpu.DeviceSetUncapturedErrorCallback(device, new PfnErrorCallback(UncapturedError), null);
            wgpu.DeviceSetDeviceLostCallback(device, new PfnDeviceLostCallback(DeviceLost), null);
        }

        public static void GetAdapter()
        {
            RequestAdapterOptions requestAdapterOptions = new RequestAdapterOptions
            {
                CompatibleSurface = surface
            };

            wgpu.InstanceRequestAdapter
            (
                instance,
                requestAdapterOptions,
                new PfnRequestAdapterCallback((_, adapter1, _, _) => adapter = adapter1),
                null
            );
        }
        public static void GetDevice()
        {
            wgpu.AdapterRequestDevice
            (
                adapter,
                null,
                new PfnRequestDeviceCallback((_, device1, _, _) => device = device1),
                null
            );
        }
        public static void GetQueue()
        {
            queue = wgpu.DeviceGetQueue(device);
        }
        public static void CreateSwapchain(Window window)
        {
            swapChainFormat = wgpu.SurfaceGetPreferredFormat(surface, adapter);

            var swapChainDescriptor = new SwapChainDescriptor
            {
                Usage = TextureUsage.RenderAttachment,
                Format = swapChainFormat,
                Width = (uint)window.GetFramebufferExtents().X,
                Height = (uint)window.GetFramebufferExtents().Y,
                PresentMode = PresentMode.Fifo
            };

            swapChain = wgpu.DeviceCreateSwapChain(device, surface, swapChainDescriptor);
        }

        private static void DeviceLost(DeviceLostReason arg0, byte* arg1, void* arg2)
        {
            Debugger.Log($"Device lost! Reason: {arg0} Message: {SilkMarshal.PtrToString((nint)arg1)}");
        }

        private static void UncapturedError(ErrorType arg0, byte* arg1, void* arg2)
        {
            Debugger.Log($"{arg0}: {SilkMarshal.PtrToString((nint)arg1)}");
        }
    }
}
#endif