using System;

namespace Somnium.Framework
{
    public static class Debugger
    {
        /// <summary>
        /// Logs a message to the output as specified in Application.Config
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dateTime"></param>
        public static void Log(object message, bool dateTime = true)
        {
            if (Application.Config.loggingMode == LoggingMode.None) return;

            string messageWithDatetime = dateTime ? '[' + DateTime.Now.ToString() + "] " + message.ToString() : message.ToString();
            if (Application.Config.loggingMode == LoggingMode.Console)
            {
                Console.WriteLine(messageWithDatetime);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(messageWithDatetime);
            }
        }
        public static void LogMemoryAllocation(string source, object message)
        {
            if (Application.Config.logMemoryAllocations)
            {
                Log(source + ": " + message.ToString(), false);
            }
        }
    }
}
