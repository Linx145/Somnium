using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    public struct ControllerState
    {
        public bool connected;

        public Vector2 leftStickAxis;
        public Vector2 rightStickAxis;
        public float L2DownAmount;
        public float R2DownAmount;

        public bool[] buttonStates;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ref ControllerState other)
        {
            other.connected = connected;
            other.leftStickAxis = leftStickAxis;
            other.rightStickAxis = rightStickAxis;
            other.L2DownAmount = L2DownAmount;
            other.R2DownAmount = R2DownAmount;
            if (buttonStates != null)
            {
                if (other.buttonStates == null)
                {
                    other.buttonStates = new bool[15];
                }
                Array.Copy(buttonStates, other.buttonStates, 15);
            }
        }

        /*public bool centralButtonDown;

        public bool DPadRight;
        public bool DPadLeft;
        public bool DPadUp;
        public bool DPadDown;

        public bool ADown;
        public bool BDown;
        public bool XDown;
        public bool YDown;

        public bool startDown;
        public bool backDown;

        public bool R1Down;
        public float R2DownAmount;
        public bool L1Down;
        public float L2DownAmount;*/
    }
}
