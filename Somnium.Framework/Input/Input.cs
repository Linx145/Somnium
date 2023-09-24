using System.Numerics;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    public static class Input
    {
        public static Window processingWindow;
        public static bool mouseInteracted;

        /// <summary>
        /// Returns true if a key is just pressed or is being held down.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyDown(Keys key) => processingWindow.inputState.IsKeyDown(key);

        /// <summary>
        /// Returns true for a frame if a key has just been pressed
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyPressed(Keys key) => processingWindow.inputState.IsKeyPressed(key);

        /// <summary>
        /// Returns true for a frame if a key has just been released
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyReleased(Keys key) => processingWindow.inputState.IsKeyReleased(key);

        /// <summary>
        /// Returns true if the specified mouse button is held down.
        /// </summary>
        /// <param name="button">The mouse button to check</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMouseDown(MouseButtons button) => processingWindow.inputState.IsMouseDown(button);

        /// <summary>
        /// Returns true for a frame if the specified mouse button has just been pressed
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMousePressed(MouseButtons button) => processingWindow.inputState.IsMousePressed(button);

        /// <summary>
        /// Returns true for a frame if the specified mouse button has just been released
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMouseReleased(MouseButtons button) => processingWindow.inputState.IsMouseRelease(button);
        /// <summary>
        /// Returns a character whenever the user types an input/holds down the button
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char? AwaitInputChar() => processingWindow.inputState.AwaitInputChar();

        public static Vector2 mousePosition => processingWindow.inputState.mousePosition;
        public static Vector2 worldMousePosition => processingWindow.inputState.worldMousePosition;

        /// <summary>
        /// Simulates the user pressing the mouse, thus IsMouseDown will become 
        /// true and IsMousePressed as well, for that frame. 
        /// <br>Up to the user to call SimulateMouseRelease to release the mouse.</br>
        /// </summary>
        /// <param name="button"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SimulateMousePress(MouseButtons button)
        {
            processingWindow.inputState.SimulateMousePress(button);
        }
        /// <summary>
        /// Simulates the user releasing the mouse, thus IsMouseDown will become 
        /// false and IsMouseReleased as well, for that frame. 
        /// <br>Up to the user to call SimulateMouseRelease to release the mouse.</br>
        /// </summary>
        /// <param name="button"></param>
        public static void SimulateMouseRelease(MouseButtons button)
        {
            processingWindow.inputState.SimulateMouseRelease(button);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMousePosition(Window window, Vector2 position) => processingWindow.inputState.SetMousePosition(window, position);
        public static Vector2 mouseScroll => processingWindow.inputState.mouseScroll;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsConnected(int controllerID) => processingWindow.inputState.IsControllerConnected(controllerID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ControllerGetLeftStickAxis(int controllerID) => processingWindow.inputState.controllerStates[controllerID].leftStickAxis;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ControllerGetRightStickAxis(int controllerID) => processingWindow.inputState.controllerStates[controllerID].rightStickAxis;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ControllerGetL2DownAmount(int controllerID) => processingWindow.inputState.controllerStates[controllerID].L2DownAmount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ControllerGetR2DownAmount(int controllerID) => processingWindow.inputState.controllerStates[controllerID].R2DownAmount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsL2Down(int controllerID) => processingWindow.inputState.controllerStates[controllerID].L2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsR2Down(int controllerID) => processingWindow.inputState.controllerStates[controllerID].R2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsL2Pressed(int controllerID) => processingWindow.inputState.controllerStates[controllerID].L2DownAmount > -1f && processingWindow.inputState.oldControllerStates[controllerID].L2DownAmount == -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsR2Pressed(int controllerID) => processingWindow.inputState.controllerStates[controllerID].R2DownAmount > -1f && processingWindow.inputState.oldControllerStates[controllerID].R2DownAmount == -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsL2Released(int controllerID) => processingWindow.inputState.controllerStates[controllerID].L2DownAmount == -1f && processingWindow.inputState.oldControllerStates[controllerID].L2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsR2Released(int controllerID) => processingWindow.inputState.controllerStates[controllerID].R2DownAmount == -1f && processingWindow.inputState.oldControllerStates[controllerID].R2DownAmount > -1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsButtonDown(int controllerID, ControllerButtons button)
        {
            return processingWindow.inputState.controllerStates[controllerID].buttonStates[(int)button];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsButtonPressed(int controllerID, ControllerButtons button)
        {
            return processingWindow.inputState.controllerStates[controllerID].buttonStates[(int)button] && !processingWindow.inputState.oldControllerStates[controllerID].buttonStates[(int)button];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ControllerIsButtonReleased(int controllerID, ControllerButtons button)
        {
            return !processingWindow.inputState.controllerStates[controllerID].buttonStates[(int)button] && processingWindow.inputState.oldControllerStates[controllerID].buttonStates[(int)button];
        }
    }

    public abstract class InputState
    {
        public ControllerState[] oldControllerStates = new ControllerState[4];
        public ControllerState[] controllerStates = new ControllerState[4];
        public Vector2 internalMousePosition;
        public Vector2 internalWorldMousePosition;
        public Vector2 scroll;

        public SparseArray<KeyState> perFrameKeyStates = new SparseArray<KeyState>(KeyState.None);
        public SparseArray<bool> keysDown = new SparseArray<bool>(false);

        public SparseArray<KeyState> perFrameMouseStates = new SparseArray<KeyState>(KeyState.None);
        public SparseArray<bool> mouseButtonsDown = new SparseArray<bool>(false);

        public char? textInputCharacter;

        public abstract bool IsKeyDown(Keys key);
        public abstract bool IsKeyPressed(Keys key);
        public abstract bool IsKeyReleased(Keys key);
        public abstract bool IsMouseDown(MouseButtons button);
        public abstract bool IsMousePressed(MouseButtons button);
        public abstract bool IsMouseRelease(MouseButtons button);
        public abstract void SimulateMousePress(MouseButtons button);
        public abstract void SimulateMouseRelease(MouseButtons button);
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
