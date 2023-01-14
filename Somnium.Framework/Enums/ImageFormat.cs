namespace Somnium.Framework;

public enum ImageFormat
{
    /// <summary>
    /// Unsigned 32-bit ARGB pixel format for store 8 bits per channel. 
    /// </summary>
    Color,
    /// <summary>
    /// DXT1. Texture format with compression. Surface dimensions must be a multiple 4.
    /// </summary>
    Dxt1,
    /// <summary>
    /// DXT3. Texture format with compression. Surface dimensions must be a multiple 4.
    /// </summary>
    Dxt3,
    /// <summary>
    /// DXT5. Texture format with compression. Surface dimensions must be a multiple 4.
    /// </summary>
    Dxt5,
    /// <summary>
    /// Signed 16-bit bump-map format for store 8 bits for <c>u</c> and <c>v</c> data.
    /// </summary>
    NormalizedByte2,
    /// <summary>
    /// Signed 32-bit bump-map format for store 8 bits per channel.
    /// </summary>
    NormalizedByte4,
    /// <summary>
    /// Unsigned 32-bit RGBA pixel format for store 10 bits for each color and 2 bits for alpha.
    /// </summary>
    Rgba1010102,
    /// <summary>
    /// Unsigned 32-bit RG pixel format using 16 bits per channel.
    /// </summary>
    Rg32,
    /// <summary>
    /// Unsigned 64-bit RGBA pixel format using 16 bits per channel.
    /// </summary>
    Rgba64,
    /// <summary>
    /// Unsigned A 8-bit format for store 8 bits to alpha channel.
    /// </summary>
    Alpha8,
    /// <summary>
    /// IEEE 32-bit R float format for store 32 bits to red channel.
    /// </summary>
    Single,
    /// <summary>
    /// IEEE 64-bit RG float format for store 32 bits per channel.
    /// </summary>
    Vector2,
    /// <summary>
    /// IEEE 128-bit RGBA float format for store 32 bits per channel.
    /// </summary>
    Vector4,
    /// <summary>
    /// Float 16-bit R format for store 16 bits to red channel.   
    /// </summary>
    HalfSingle,
    /// <summary>
    /// Float 32-bit RG format for store 16 bits per channel. 
    /// </summary>
    HalfVector2,
    /// <summary>
    /// Float 64-bit ARGB format for store 16 bits per channel. 
    /// </summary>
    HalfVector4,
    /// <summary>
    /// Float pixel format for high dynamic range data.
    /// </summary>
    HdrBlendable,
}