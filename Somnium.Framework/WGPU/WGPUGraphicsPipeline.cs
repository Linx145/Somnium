#if WGPU
using System;
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;

namespace Somnium.Framework.WGPU
{
    public sealed unsafe class WGPUGraphicsPipeline : IDisposable
    {
        public RenderPipeline* Pipeline;
        public CullMode cullMode;
        public BlendState blendState;
        public PrimitiveType primitiveType;
        public Shader shader;
        public ImageFormat outputFormat;
        public bool depthTest;
        public bool depthWrite;
        public DepthFormat depthFormat;

        public WGPUGraphicsPipeline(
    CullMode cullMode,
    BlendState blendState,
    PrimitiveType primitiveType,
    ImageFormat outputFormat,
    Shader shader,
    bool depthTest,
    bool depthWrite,
    DepthFormat depthFormat,
    params VertexDeclaration[] vertexTypes)
        {
            this.depthFormat = depthFormat;
            this.cullMode = cullMode;
            this.blendState = blendState;
            this.primitiveType = primitiveType;
            this.shader = shader;
            this.depthTest = depthTest;
            this.depthWrite = depthWrite;
            this.outputFormat = outputFormat;

            BuildPipeline();
        }

        public unsafe void BuildPipeline()
        {
            //blend state
            var wgpuBlend = new Silk.NET.WebGPU.BlendState();
            wgpuBlend.Color = new BlendComponent(
                BlendOperation.Add,
                Converters.BlendStateToFactor[(int)blendState.SourceColorBlend],
                Converters.BlendStateToFactor[(int)blendState.DestinationColorBlend]);
            wgpuBlend.Alpha = new BlendComponent(
                BlendOperation.Add,
                Converters.BlendStateToFactor[(int)blendState.SourceAlphaBlend],
                Converters.BlendStateToFactor[(int)blendState.DestinationAlphaBlend]);

            var colorTargetState = new ColorTargetState();
            colorTargetState.Format = Converters.ImageFormatToWGPUFormat[(int)outputFormat];

            var fragmentState = new FragmentState
            {
                Module = (ShaderModule*)new IntPtr((long)shader.shaderHandle2).ToPointer(),
                TargetCount = 1,
                Targets = &colorTargetState,
                EntryPoint = (byte*)Shader.mainPtr.ToPointer()
            };

            var vertexState = new VertexState()
            {
                Module = (ShaderModule*)new IntPtr((long)shader.shaderHandle).ToPointer(),
                EntryPoint = (byte*)Shader.mainPtr.ToPointer()
            };

            var primitiveState = new PrimitiveState()
            {
                Topology = Converters.PrimitiveTypeToWGPUTopology[(int)primitiveType],
                StripIndexFormat = IndexFormat.Uint16,
                CullMode = Converters.CullModeToWGPUCullMode[(int)cullMode]
            };

            var multiSamplerState = new MultisampleState()
            {
                Count = 1,
                Mask = ~0u,
                AlphaToCoverageEnabled = false
            };

            var depthStencilState = new DepthStencilState()
            {
                DepthWriteEnabled = depthWrite,
                DepthCompare = depthTest ? CompareFunction.LessEqual : CompareFunction.Never,
                Format = Converters.DepthFormatToWGPUFormat[(int)depthFormat]
            };

            var renderPipelineDescriptor = new RenderPipelineDescriptor()
            {
                Vertex = vertexState,
                Fragment = &fragmentState,
                Primitive = primitiveState,
                Multisample = multiSamplerState,
                DepthStencil = &depthStencilState
            };

            Pipeline = WGPUEngine.wgpu.DeviceCreateRenderPipeline(WGPUEngine.device, &renderPipelineDescriptor);
        }

        public void Dispose()
        {
            WGPUEngine.crab.RenderPipelineDrop(Pipeline);
        }
    }
}
#endif