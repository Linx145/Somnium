using Silk.NET.Core.Native;
using System;
using System.Runtime.InteropServices;

namespace Somnium.Framework
{
    public static unsafe class Utils
    {
        public static byte* StringToBytePtr(string str, out IntPtr ptr)
        {
            IntPtr intPtr = Marshal.StringToHGlobalAnsi(str);
            ptr = intPtr;
            return (byte*)intPtr;
        }
        public static byte** StringArrayToPointer(string[] strArray, out IntPtr ptr)
        {
            IntPtr intPtr = SilkMarshal.StringArrayToPtr(strArray);
            ptr = intPtr;
            return (byte**)intPtr;
        }
    }
}
