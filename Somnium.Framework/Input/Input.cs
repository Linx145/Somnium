namespace Somnium.Framework
{
    public static class Input
    {
        public static InputState instance;

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
    }

    public abstract class InputState
    {
        public abstract bool IsKeyDown(Keys key);
        public abstract bool IsKeyPressed(Keys key);
        public abstract bool IsKeyReleased(Keys key);
    }
}
