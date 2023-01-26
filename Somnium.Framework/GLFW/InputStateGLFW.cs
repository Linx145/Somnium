using Silk.NET.GLFW;
using System;

namespace Somnium.Framework.GLFW
{
    public class InputStateGLFW : InputState
    {
        internal static SparseArray<KeyState> perFrameKeyStates = new SparseArray<KeyState>(KeyState.None);
        internal static SparseArray<bool> keysDown = new SparseArray<bool>(false);
        public InputStateGLFW()
        {
        }
        public override bool IsKeyDown(Keys key)
        {
            return keysDown.WithinLength((uint)key) && keysDown[(uint)key];
        }
        public override bool IsKeyPressed(Keys key)
        {
            return perFrameKeyStates.WithinLength((uint)key) && perFrameKeyStates[(uint)key] == KeyState.Pressed;
        }
        public override bool IsKeyReleased(Keys key)
        {
            return perFrameKeyStates.WithinLength((uint)key) && perFrameKeyStates[(uint)key] == KeyState.Released;
        }

        /// <summary>
        /// Called at the end of every frame. Resets per-frame key states
        /// </summary>
        internal static void ResetPerFrameKeyStates()
        {
            for (int i = 0; i < perFrameKeyStates.values.Length; i++)
            {
                perFrameKeyStates.values[i] = KeyState.None;
            }
        }
        /// <summary>
        /// Called when the window is minimized or any other reason that may cause the window to stop recording key presses
        /// </summary>
        internal static void ClearAllKeyStates()
        {
            ResetPerFrameKeyStates();
            for (int i = 0; i < keysDown.values.Length; i++)
            {
                keysDown.values[i] = false;
            }
        }
    }
}
