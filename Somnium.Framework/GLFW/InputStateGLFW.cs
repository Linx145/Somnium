#if GLFW
using Silk.NET.GLFW;
using System;
using System.Numerics;

namespace Somnium.Framework.GLFW
{
    public class InputStateGLFW : InputState
    {
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
        public override void SimulateMousePress(MouseButtons button)
        {
            if (!mouseButtonsDown.WithinLength((uint)button) || !mouseButtonsDown[(uint)button])
            {
                mouseButtonsDown.Insert((uint)button, true);
                perFrameMouseStates.Insert((uint)button, KeyState.Pressed);
            }
        }
        public override void SimulateMouseRelease(MouseButtons button)
        {
            if (mouseButtonsDown.WithinLength((uint)button) && mouseButtonsDown[(uint)button])
            {
                mouseButtonsDown.Insert((uint)button, false);
                perFrameMouseStates.Insert((uint)button, KeyState.Released);
            }
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
        public void ResetPerFrameInputStates()
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
                    GetControllerState(i, ref controllerStates[i]);
                }
            //}
        }
        /// <summary>
        /// Called when the window is minimized or any other reason that may cause the window to stop recording key presses
        /// </summary>
        public void ClearAllInputStates()
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
#endif