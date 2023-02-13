using System.Numerics;

namespace Somnium.Framework
{
    public static class Input
    {
        public static InputState instance;

        public static bool mouseInteracted;

        /// <summary>
        /// Returns true if a key is just pressed or is being held down.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        public static bool IsKeyDown(Keys key) => instance.IsKeyDown(key);

        /// <summary>
        /// Returns true for a frame if a key has just been pressed
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        public static bool IsKeyPressed(Keys key) => instance.IsKeyPressed(key);

        /// <summary>
        /// Returns true for a frame if a key has just been released
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        public static bool IsKeyReleased(Keys key) => instance.IsKeyReleased(key);

        /// <summary>
        /// Returns true if the specified mouse button is held down.
        /// </summary>
        /// <param name="button">The mouse button to check</param>
        /// <returns></returns>
        public static bool IsMouseDown(MouseButtons button) => instance.IsMouseDown(button);

        /// <summary>
        /// Returns true for a frame if the specified mouse button has just been pressed
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static bool IsMousePressed(MouseButtons button) => instance.IsMousePressed(button);

        /// <summary>
        /// Returns true for a frame if the specified mouse button has just been released
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static bool IsMouseReleased(MouseButtons button) => instance.IsMouseRelease(button);

        public static Vector2 mousePosition => instance.mousePosition;
        public static void SetMousePosition(Window window, Vector2 position) => instance.SetMousePosition(window, position);
        public static Vector2 mouseScroll => instance.mouseScroll;
    }

    public abstract class InputState
    {
        public abstract bool IsKeyDown(Keys key);
        public abstract bool IsKeyPressed(Keys key);
        public abstract bool IsKeyReleased(Keys key);
        public abstract bool IsMouseDown(MouseButtons button);
        public abstract bool IsMousePressed(MouseButtons button);
        public abstract bool IsMouseRelease(MouseButtons button);
        public abstract Vector2 mousePosition { get;}
        public abstract unsafe void SetMousePosition(Window window, Vector2 mousePosition);
        public abstract Vector2 mouseScroll { get; }
    }
}
