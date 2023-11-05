using System;
#if GLFW
using Somnium.Framework.GLFW;
using Silk.NET.GLFW;
#endif

namespace Somnium.Framework
{
    public static class MonitorData
    {
        public const int primaryMonitor = 0;
        public static unsafe int GetMonitorsCount()
        {
#if GLFW
            SomniumGLFW.API.GetMonitors(out int count);
            return count;
#endif

            throw new NotImplementedException();
        }
        /// <summary>
        /// Gets the position and resolution of a monitor
        /// </summary>
        /// <param name="monitorIndex">The index of the monitor to get data from. Use 0 for the default monitor if any</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static unsafe Rectangle GetMonitorArea(int monitorIndex)
        {
#if GLFW
            Monitor** monitors = SomniumGLFW.API.GetMonitors(out int count);

            if (count == 0 || monitorIndex >= count)
            {
                return default;
            }
            SomniumGLFW.API.GetMonitorWorkarea(monitors[monitorIndex], out int x, out int y, out int width, out int height);
            return new Rectangle(x, y, width, height);
#endif
            throw new NotImplementedException();
        }
        /// <summary>
        /// Gets the refresh rate of a monitor
        /// </summary>
        /// <param name="monitorIndex"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">The index of the monitor to get data from. Use 0 for the default monitor if any</exception>
        public static unsafe int GetMonitorRefreshRate(int monitorIndex)
        {
#if GLFW
            Monitor** monitors = SomniumGLFW.API.GetMonitors(out int count);
            
            if (count == 0 || monitorIndex >= count)
            {
                return -1;
            }
            VideoMode* videoMode = SomniumGLFW.API.GetVideoMode(monitors[monitorIndex]);
            return videoMode->RefreshRate;
#endif
            throw new NotImplementedException();
        }
    }
}
