using FMOD;
using System;

namespace Somnium.Framework.Audio
{
    public static class AudioEngine
    {
#if FMOD
        public static FMOD.System API;
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
            result = API.setCallback(new SYSTEM_CALLBACK(LogCallback), SYSTEM_CALLBACK_TYPE.ERROR);
        }
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
            Console.WriteLine("Caught fmod error!");
            Console.WriteLine("function: " + (string)errorCallbackInfo->functionname);
            Console.WriteLine("params: " + (string)errorCallbackInfo->functionparams);
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
#endif
    }
}