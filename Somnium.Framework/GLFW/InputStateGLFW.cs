using Silk.NET.GLFW;
using System;
using System.Numerics;

namespace Somnium.Framework.GLFW
{
    public class InputStateGLFW : InputState
    {
        internal static Vector2 internalMousePosition;
        internal static Vector2 internalWorldMousePosition;
        internal static Vector2 scroll;

        internal static SparseArray<KeyState> perFrameKeyStates = new SparseArray<KeyState>(KeyState.None);
        internal static SparseArray<bool> keysDown = new SparseArray<bool>(false);

        internal static SparseArray<KeyState> perFrameMouseStates = new SparseArray<KeyState>(KeyState.None);
        internal static SparseArray<bool> mouseButtonsDown = new SparseArray<bool>(false);

        internal static char? textInputCharacter;
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
            return perFrameMouseStates.WithinLength((uint)button) && perFrameMouseStates[(uint)button] == KeyState.Pressed;
        }
        public override bool IsMouseRelease(MouseButtons button)
        {
            return perFrameMouseStates.WithinLength((uint)button) && perFrameMouseStates[(uint)button] == KeyState.Released;
        }
        public override bool IsControllerConnected(int controllerIndex)
        {
            if (controllerIndex >= 16)
            {
                throw new ArgumentOutOfRangeException("controllerIndex");
            }
            return SomniumGLFW.API.JoystickPresent(controllerIndex);
        }
        public override unsafe void GetControllerState(int controllerIndex, ref ControllerState result)
        {
            if (SomniumGLFW.API.GetGamepadState(controllerIndex, out var glfwGamepadState))
            {
                result.connected = true;

                result.leftStickAxis = new Vector2(glfwGamepadState.Axes[0], glfwGamepadState.Axes[1]);
                result.rightStickAxis = new Vector2(glfwGamepadState.Axes[2], glfwGamepadState.Axes[3]);
                result.L2DownAmount = glfwGamepadState.Axes[4];
                result.R2DownAmount = glfwGamepadState.Axes[5];
                if (result.buttonStates == null)
                {
                    result.buttonStates = new bool[15];
                }
                for (int i = 0;i < 15; i++)
                {
                    result.buttonStates[i] = glfwGamepadState.Buttons[i] == (byte)InputAction.Press;
                }
                /*result.L2DownAmount = glfwGamepadState.Axes[4];
                result.R2DownAmount = glfwGamepadState.Axes[5];

                result.ADown = glfwGamepadState.Buttons[0] == (byte)InputAction.Press;
                result.BDown = glfwGamepadState.Buttons[1] == (byte)InputAction.Press;
                result.XDown = glfwGamepadState.Buttons[2] == (byte)InputAction.Press;
                result.YDown = glfwGamepadState.Buttons[3] == (byte)InputAction.Press;
                result.L1Down = glfwGamepadState.Buttons[4] == (byte)InputAction.Press;
                result.R1Down = glfwGamepadState.Buttons[5] == (byte)InputAction.Press;
                result.backDown = glfwGamepadState.Buttons[6] == (byte)InputAction.Press;
                result.startDown = glfwGamepadState.Buttons[7] == (byte)InputAction.Press;
                result.centralButtonDown = glfwGamepadState.Buttons[8] == (byte)InputAction.Press;
                result.leftStickPressed = glfwGamepadState.Buttons[9] == (byte)InputAction.Press;
                result.rightStickPressed = glfwGamepadState.Buttons[10] == (byte)InputAction.Press;
                result.DPadUp = glfwGamepadState.Buttons[11] == (byte)InputAction.Press;
                result.DPadRight = glfwGamepadState.Buttons[12] == (byte)InputAction.Press;
                result.DPadDown = glfwGamepadState.Buttons[13] == (byte)InputAction.Press;
                result.DPadLeft = glfwGamepadState.Buttons[14] == (byte)InputAction.Press;*/
            }
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
        public override char? AwaitInputChar()
        {
            return textInputCharacter;
        }

        /// <summary>
        /// Called at the end of every frame. Resets per-frame key states
        /// </summary>
        internal static void ResetPerFrameInputStates()
        {
            textInputCharacter = null;
            scroll = default;
            for (int i = 0; i < perFrameKeyStates.values.Length; i++)
            {
                perFrameKeyStates.values[i] = KeyState.None;
            }
            for (int i = 0; i < perFrameMouseStates.values.Length; i++)
            {
                perFrameMouseStates.values[i] = KeyState.None;
            }
            //if (Input.ConnectedControllers > 0)
            //{
                for (int i = 0; i < controllerStates.Length; i++)
                {
                    //oldControllerStates[i] = controllerStates[i];
                    controllerStates[i].CopyTo(ref oldControllerStates[i]);
                    ((InputStateGLFW)Input.instance).GetControllerState(i, ref controllerStates[i]);
                }
            //}
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
