using Silk.NET.GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework.GLFW
{
    public static class SomniumGLFW
    {
        public static Glfw API { get; private set; }
        public static bool initialized { get; private set; }
        public static bool Initialize()
        {
            API = Glfw.GetApi();
            bool result = API.Init();
            if (!result)
            {
                return false;
                //throw new InitializationException("GLFW failed to initialize!");
            }
            initialized = true;
            API.SetErrorCallback(OnError);
            return true;
        }
        public static void Shutdown()
        {
            API.Terminate();
            API.Dispose();
            initialized = false;
        }

        public static void OnError(ErrorCode errorCode, string message)
        {
            throw new Exception(message);
        }
    }
}
