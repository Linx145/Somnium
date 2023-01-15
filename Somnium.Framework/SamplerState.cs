using Silk.NET.Vulkan;
using Somnium.Framework.Vulkan;
using System;
using System.Collections.Generic;

namespace Somnium.Framework
{
    public class SamplerState : IDisposable
    {
        public static SamplerState PointClamp;
        public static SamplerState PointWrap;
        public static SamplerState LinearClamp;
        public static SamplerState LinearWrap;
        //make these yourself
        //public static SamplerState AnisotropicClamp;
        //public static SamplerState AnisotropicWrap;

        public static List<SamplerState> allSamplerStates = new List<SamplerState>();

        public bool constructed { get; private set; }
        public ulong handle;

        private readonly Application application;
        public readonly FilterMode filterMode;
        public readonly RepeatMode repeatMode;
        public readonly bool anisotropic;
        public readonly float anisotropyLevel;

        public SamplerState(Application application, FilterMode filterMode, RepeatMode repeatMode, bool anisotropic = false, float anisotropyLevel = 1f)
        {
            this.application = application;
            this.filterMode = filterMode;
            this.repeatMode = repeatMode;
            this.anisotropic = anisotropic;
            this.anisotropyLevel = anisotropyLevel;

            Construct();
        }
        public void Construct()
        {
            if (constructed)
            {
                throw new AssetCreationException("Sampler State already constructed!");
            }
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        SamplerCreateInfo samplerInfo = new SamplerCreateInfo();
                        samplerInfo.SType = StructureType.SamplerCreateInfo;
                        samplerInfo.MagFilter = FilterModeToVkFilter[(int)filterMode];
                        samplerInfo.MinFilter = FilterModeToVkFilter[(int)filterMode];

                        var mode = RepeatModeToVkSamplerAddressMode[(int)repeatMode];

                        samplerInfo.AddressModeU = mode;
                        samplerInfo.AddressModeV = mode;
                        samplerInfo.AddressModeW = mode;


                        samplerInfo.AnisotropyEnable = new Silk.NET.Core.Bool32(anisotropic);

                        samplerInfo.MaxAnisotropy = anisotropyLevel;

                        samplerInfo.BorderColor = BorderColor.FloatTransparentBlack;
                        samplerInfo.UnnormalizedCoordinates = new Silk.NET.Core.Bool32(false);
                        samplerInfo.CompareEnable = new Silk.NET.Core.Bool32(false);
                        samplerInfo.CompareOp = CompareOp.Never;

                        samplerInfo.MipmapMode = SamplerMipmapMode.Linear;
                        samplerInfo.MipLodBias = 0.0f;
                        samplerInfo.MinLod = 0.0f;
                        samplerInfo.MaxLod = 0.0f;

                        Sampler sampler;
                        if (VkEngine.vk.CreateSampler(VkEngine.vkDevice, in samplerInfo, null, &sampler) != Result.Success)
                        {
                            throw new AssetCreationException("Failed to create Vulkan Sampler State!");
                        }
                        handle = sampler.Handle;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            switch (application.runningBackend)
            {
                case Backends.Vulkan:
                    unsafe
                    {
                        VkEngine.vk.DestroySampler(VkEngine.vkDevice, new Sampler(handle), null);
                    }
                        break;
                default:
                    throw new NotImplementedException();
            }
        }

        #region static
        public static void AddDefaultSamplerStates(Application application)
        {
            if (allSamplerStates == null)
            {
                allSamplerStates = new List<SamplerState>();
            }
            PointClamp = new SamplerState(application, FilterMode.Point, RepeatMode.Clamp);
            LinearClamp = new SamplerState(application, FilterMode.Linear, RepeatMode.Clamp);
            PointWrap = new SamplerState(application, FilterMode.Point, RepeatMode.Repeat);
            LinearWrap = new SamplerState(application, FilterMode.Linear, RepeatMode.Repeat);

            allSamplerStates.Add(PointClamp);
            allSamplerStates.Add(PointWrap);
            allSamplerStates.Add(LinearClamp);
            allSamplerStates.Add(LinearWrap);
        }
        public static void DisposeDefaultSamplerStates()
        {
            PointClamp.Dispose();
            PointWrap.Dispose();
            LinearClamp.Dispose();
            LinearWrap.Dispose();
        }
        public static readonly Filter[] FilterModeToVkFilter = new Filter[]
        {
            Filter.Nearest,
            Filter.Linear
        };
        public static readonly SamplerAddressMode[] RepeatModeToVkSamplerAddressMode = new SamplerAddressMode[]
        {
            SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.Repeat,
            SamplerAddressMode.ClampToBorder
        };
        #endregion
    }
}
