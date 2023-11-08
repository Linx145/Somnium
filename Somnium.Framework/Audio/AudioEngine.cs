using FMOD;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Somnium.Framework.Audio
{
    public static class AudioEngine
    {
#if FMOD
        private static SYSTEM_CALLBACK callback;
        public static FMOD.System API;
        internal static Dictionary<IntPtr, uint> ChannelGenerations = new Dictionary<IntPtr, uint>();
        public static void Initialize()
        {
            var result = FMOD.Factory.System_Create(out API);
            if (result != RESULT.OK)
            {
                throw new InitializationException("FMOD creation error: " + result);
            }
            result = API.init(512, INITFLAGS.NORMAL, IntPtr.Zero);//512, FMOD.Studio.INITFLAGS.NORMAL, INITFLAGS.NORMAL, IntPtr.Zero);
            if (result != RESULT.OK)
            {
                throw new InitializationException("FMOD initialization error: " + result);
            }
            //callback = new SYSTEM_CALLBACK(LogCallback);
            //result = API.setCallback(callback, SYSTEM_CALLBACK_TYPE.ERROR);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update()
        {
            if (API.update() != RESULT.OK)
            {
                throw new ExecutionException("Failed to update FMOD API!");
            }
        }
        public static unsafe RESULT LogCallback(IntPtr systemPtr, SYSTEM_CALLBACK_TYPE callbackType, IntPtr commandData1, IntPtr commandData2, IntPtr userData)
        {
            ERRORCALLBACK_INFO* errorCallbackInfo = (ERRORCALLBACK_INFO*)(commandData1);
            //Console.WriteLine("Caught fmod error!");
            //Console.WriteLine("function: " + (string)errorCallbackInfo->functionname);
            //Console.WriteLine("params: " + (string)errorCallbackInfo->functionparams);
            Debugger.Log("Caught fmod error, function: " + (string)errorCallbackInfo->functionname + ", RESULT: " + (errorCallbackInfo->result).ToString());
            return RESULT.ERR_INVALID_PARAM;
        }
        public static void Shutdown()
        {
            var result = API.release();
            if (result != RESULT.OK)
            {
                throw new InitializationException("FMOD release error: " + result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VECTOR ToVECTOR(this Vector3 vector)
        {
            VECTOR results = new VECTOR();
            results.x = vector.X;
            results.y = vector.Y;
            results.z = vector.Z;

            return results;
        }
#endif
    }
}