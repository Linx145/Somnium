using System.Numerics;
using System.Runtime.CompilerServices;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyDown(Keys key) => instance.IsKeyDown(key);

        /// <summary>
        /// Returns true for a frame if a key has just been pressed
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyPressed(Keys key) => instance.IsKeyPressed(key);

        /// <summary>
        /// Returns true for a frame if a key has just been released
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyReleased(Keys key) => instance.IsKeyReleased(key);

        /// <summary>
        /// Returns true if the specified mouse button is held down.
        /// </summary>
        /// <param name="button">The mouse button to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMouseDown(MouseButtons button) => instance.IsMouseDown(button);

        /// <summary>
        /// Returns true for a frame if the specified mouse button has just been pressed
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMousePressed(MouseButtons button) => instance.IsMousePressed(button);

        /// <summary>
        /// Returns true for a frame if the specified mouse button has just been released
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMouseReleased(MouseButtons button) => instance.IsMouseRelease(button);
        /// <summary>
        /// Returns a character whenever the user types an input/holds down the button
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char? AwaitInputChar() => instance.AwaitInputChar();

        public static Vector2 mousePosition => instance.mousePosition;
        public static Vector2 worldMousePosition => instance.worldMousePosition;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMousePosition(Window window, Vector2 position) => instance.SetMousePosition(window, position);
        public static Vector2 mouseScroll => instance.mouseScroll;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsConnected(int controllerID) => instance.IsControllerConnected(controllerID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ControllerGetLeftStickAxis(int controllerID) => InputState.controllerStates[controllerID].leftStickAxis;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ControllerGetRightStickAxis(int controllerID) => InputState.controllerStates[controllerID].rightStickAxis;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ControllerGetL2DownAmount(int controllerID) => InputState.controllerStates[controllerID].L2DownAmount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ControllerGetR2DownAmount(int controllerID) => InputState.controllerStates[controllerID].R2DownAmount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsL2Down(int controllerID) => InputState.controllerStates[controllerID].L2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsR2Down(int controllerID) => InputState.controllerStates[controllerID].R2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsL2Pressed(int controllerID) => InputState.controllerStates[controllerID].L2DownAmount > -1f && InputState.oldControllerStates[controllerID].L2DownAmount == -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsR2Pressed(int controllerID) => InputState.controllerStates[controllerID].R2DownAmount > -1f && InputState.oldControllerStates[controllerID].R2DownAmount == -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsL2Released(int controllerID) => InputState.controllerStates[controllerID].L2DownAmount == -1f && InputState.oldControllerStates[controllerID].L2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsR2Released(int controllerID) => InputState.controllerStates[controllerID].R2DownAmount == -1f && InputState.oldControllerStates[controllerID].R2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsButtonDown(int controllerID, ControllerButtons button)
        {
            return InputState.controllerStates[controllerID].buttonStates[(int)button];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsButtonPressed(int controllerID, ControllerButtons button)
        {
            return InputState.controllerStates[controllerID].buttonStates[(int)button] && !InputState.oldControllerStates[controllerID].buttonStates[(int)button];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsButtonReleased(int controllerID, ControllerButtons button)
        {
            return !InputState.controllerStates[controllerID].buttonStates[(int)button] && InputState.oldControllerStates[controllerID].buttonStates[(int)button];
        }
    }

    public abstract class InputState
    {
        public static ControllerState[] oldControllerStates = new ControllerState[4];
        public static ControllerState[] controllerStates = new ControllerState[4];

        public abstract bool IsKeyDown(Keys key);
        public abstract bool IsKeyPressed(Keys key);
        public abstract bool IsKeyReleased(Keys key);
        public abstract bool IsMouseDown(MouseButtons button);
        public abstract bool IsMousePressed(MouseButtons button);
        public abstract bool IsMouseRelease(MouseButtons button);
        public abstract char? AwaitInputChar();
        public abstract Vector2 mousePosition { get;}
        /// <summary>
        /// To be set by the end user where applicable
        /// </summary>
        public Vector2 worldMousePosition;
        public abstract unsafe void SetMousePosition(Window window, Vector2 mousePosition);
        public abstract Vector2 mouseScroll { get; }

        public abstract bool IsControllerConnected(int controllerIndex);
        public abstract void GetControllerState(int controllerIndex, ref ControllerState controllerState);
    }
}
