#if WGPU
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using Silk.NET.Core.Native;

namespace Somnium.Framework.WGPU
{
    public static unsafe class WGPUEngine
    {
        public static WebGPU wgpu = null;
        public static Wgpu crab;
        internal static Instance* instance;
        internal static Surface* surface;
        internal static Adapter* adapter;
        internal static Device* device;
        internal static Queue* queue;
        internal static SwapChain* swapChain;
        internal static TextureFormat swapChainFormat;

        public static CommandEncoder* commandEncoder;
        public static TextureView* backbufferTextureView;
        //private static TextureFormat _SwapChainFormat;

        private static GenerationalArray<WGPUGraphicsPipeline> pipelines = new GenerationalArray<WGPUGraphicsPipeline>(null);

        public static bool initialized { get; private set; }

        private static unsafe void Initialize(Window forWindow, string AppName, bool enableValidationLayers = true)
        {
            wgpu = WebGPU.GetApi();
            wgpu.TryGetDeviceExtension(null, out crab);

            InstanceDescriptor instanceDescriptor = new InstanceDescriptor();
            instance = wgpu.CreateInstance(&instanceDescriptor);

            surface = WebGPUSurface.CreateWebGPUSurface(forWindow, wgpu, instance);//wgpu.InstanceCreateSurface(instance, &surfaceDescriptor);

            GetAdapter();
            GetDevice();
            GetQueue();

            if (enableValidationLayers)
            {
                wgpu.DeviceSetUncapturedErrorCallback(device, new PfnErrorCallback(UncapturedError), null);
                wgpu.DeviceSetDeviceLostCallback(device, new PfnDeviceLostCallback(DeviceLost), null);
            }

            CreateSwapchain(forWindow);
        }

        private static void GetAdapter()
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
        private static void GetDevice()
        {
            wgpu.AdapterRequestDevice
            (
                adapter,
                null,
                new PfnRequestDeviceCallback((_, device1, _, _) => device = device1),
                null
            );
        }
        private static void GetQueue()
        {
            queue = wgpu.DeviceGetQueue(device);
        }
        private static void CreateSwapchain(Window window)
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
            Debugger.Log($"Validation layer: Error caught of type {arg0}, message: {SilkMarshal.PtrToString((nint)arg1)}");
        }

        public static void BeginDraw(Window window)
        {
            CommandEncoderDescriptor cmdCollectionDescriptor = new CommandEncoderDescriptor();
            commandEncoder = wgpu.DeviceCreateCommandEncoder(device, &cmdCollectionDescriptor);

            backbufferTextureView = null;

            for (var attempt = 0; attempt < 2; attempt++)
            {
                backbufferTextureView = wgpu.SwapChainGetCurrentTextureView(swapChain);

                if (attempt == 0 && backbufferTextureView == null)
                {
                    Debugger.Log("wgpu.SwapChainGetCurrentTextureView() failed; trying to create a new swap chain...\n");
                    CreateSwapchain(window);
                    continue;
                }

                break;
            }
            if (backbufferTextureView == null)
            {
                throw new ExecutionException("Failed to create new swapchain");
            }
        }

        #region pipelines
        public static GenerationalIndex AddPipeline(WGPUGraphicsPipeline pipeline) => pipelines.Add(pipeline);
        public static void DestroyPipeline(GenerationalIndex index)
        {
            pipelines[index].Dispose();
            pipelines.Remove(index);
        }
        public static WGPUGraphicsPipeline GetPipeline(GenerationalIndex index) => pipelines.Get(index);
        #endregion

        public static void Shutdown()
        {
            crab.SwapChainDrop(swapChain);
            crab.DeviceDrop(device);
            crab.AdapterDrop(adapter);
            crab.SurfaceDrop(surface);
            crab.InstanceDrop(instance);
            wgpu.Dispose();
        }
    }
}
#endif