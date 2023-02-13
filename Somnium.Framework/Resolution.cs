using System.Numerics;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    /// <summary>
    /// A GPU memory friendly data structure that can be passed into shaders to represent Vector2s by themselves.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack =1, Size = Mathf.Vector4Size)]
    public readonly struct ShaderVector2
    {
        public readonly Vector2 Resolution;

        public ShaderVector2(float X, float Y)
        {
            this.Resolution = new Vector2(X, Y);
        }
    }
}
