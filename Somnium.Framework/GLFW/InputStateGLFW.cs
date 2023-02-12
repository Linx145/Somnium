using Silk.NET.GLFW;
using System;


namespace Somnium.Framework.GLFW
{
    public class InputStateGLFW : InputState
    {
        internal static Vector2 internalMousePosition;
        internal static Vector2 scroll;

        internal static SparseArray<KeyState> perFrameKeyStates = new SparseArray<KeyState>(KeyState.None);
        internal static SparseArray<bool> keysDown = new SparseArray<bool>(false);

        internal static SparseArray<KeyState> perFrameMouseStates = new SparseArray<KeyState>(KeyState.None);
        internal static SparseArray<bool> mouseButtonsDown = new SparseArray<bool>(false);
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

        public override bool IsMouseDown(MouseButtons button)
        {
            return mouseButtonsDown.WithinLength((uint)button) && mouseButtonsDown[(uint)button];
        }
        public override bool IsMousePressed(MouseButtons button)
        {
            return perFrameKeyStates.WithinLength((uint)button) && perFrameKeyStates[(uint)button] == KeyState.Pressed;
        }
        public override bool IsMouseRelease(MouseButtons button)
        {
            return perFrameKeyStates.WithinLength((uint)button) && perFrameKeyStates[(uint)button] == KeyState.Released;
        }
        public override Vector2 mousePosition
        {
            get
            {
                return internalMousePosition;
            }
        }
        public override unsafe void SetMousePosition(Window window, Vector2 mousePosition)
        {
            var windowGLFW = (WindowGLFW)window;
            internalMousePosition = mousePosition;
            SomniumGLFW.API.SetCursorPos(windowGLFW.handle, mousePosition.X, mousePosition.Y);
        }

        /// <summary>
        /// Called at the end of every frame. Resets per-frame key states
        /// </summary>
        internal static void ResetPerFrameInputStates()
        {
            for (int i = 0; i < perFrameKeyStates.values.Length; i++)
            {
                perFrameKeyStates.values[i] = KeyState.None;
            }
            for (int i = 0; i < perFrameMouseStates.values.Length; i++)
            {
                perFrameMouseStates.values[i] = KeyState.None;
            }
        }
        /// <summary>
        /// Called when the window is minimized or any other reason that may cause the window to stop recording key presses
        /// </summary>
        internal static void ClearAllInputStates()
        {
            ResetPerFrameInputStates();
            for (int i = 0; i < keysDown.values.Length; i++)
            {
                keysDown.values[i] = false;
            }
            for (int i = 0; i < mouseButtonsDown.values.Length; i++)
            {
                mouseButtonsDown.values[i] = false;
            }
        }
        public override Vector2 mouseScroll => scroll;
    }
}
