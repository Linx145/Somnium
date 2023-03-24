namespace Somnium.Framework;

//TODO: EXPAND
public enum ImageFormat
{
    /// <summary>
    /// Standard 1 byte per channel red-green-blue-alpha with each value being unsigned (>= o) and normalized (0-1)
    /// </summary>
    R8G8B8A8Unorm,
    R8G8B8A8SNorm,
    R8G8B8A8Srgb,
    B8G8R8A8Unorm,
    B8G8R8A8SNorm,
    B8G8R8A8Srgb,
    HalfVector4,
    Vector4,
    R32Uint
}