#if VULKAN
using Silk.NET.Vulkan;
#endif
using System;

namespace Somnium.Framework;

public static partial class Converters
{
    public static bool DepthFormatHasStencil(DepthFormat depthFormat)
    {
        switch (depthFormat)
        {
            case DepthFormat.Depth24Stencil8:
            case DepthFormat.Depth16Stencil8:
                return true;
            default:
                return false;
        }
    }
}