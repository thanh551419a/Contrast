using Contrast.Color;
using Contrast.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Contrast.Input
{
    /// <summary>
    /// Desktop platform adapter. Translates Windows physical keyboard input into standard
    /// gameplay commands conforming to <see cref="IPlatformInputAdapter"/>.
    /// </summary>
    public sealed class StandaloneKeyboardInputAdapter : IPlatformInputAdapter
    {
        private bool prevWPressed;
        private bool prevSpacePressed;

        public float GetHorizontalAxis()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return 0f;

            float axis = 0f;
            if (kb.dKey.isPressed)
                axis += 1f;
            if (kb.aKey.isPressed)
                axis -= 1f;

            return Mathf.Clamp(axis, -1f, 1f);
#else
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.D))
                axis += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A))
                axis -= 1f;

            return Mathf.Clamp(axis, -1f, 1f);
#endif
        }

        public bool GetJumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return false;

            bool wEdge = kb.wKey.wasPressedThisFrame || (kb.wKey.isPressed && !prevWPressed);
            bool spaceEdge = kb.spaceKey.wasPressedThisFrame || (kb.spaceKey.isPressed && !prevSpacePressed);
            return wEdge || spaceEdge;
#else
            bool wEdge = UnityEngine.Input.GetKeyDown(KeyCode.W) || (UnityEngine.Input.GetKey(KeyCode.W) && !prevWPressed);
            bool spaceEdge = UnityEngine.Input.GetKeyDown(KeyCode.Space) || (UnityEngine.Input.GetKey(KeyCode.Space) && !prevSpacePressed);
            return wEdge || spaceEdge;
#endif
        }

        public bool GetFastFall()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return false;

            return kb.sKey.isPressed;
#else
            return UnityEngine.Input.GetKey(KeyCode.S);
#endif
        }

        public float GetPhaseTuneAxis()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return 0f;

            float axis = 0f;
            if (kb.rightArrowKey.isPressed)
                axis += 1f;
            if (kb.leftArrowKey.isPressed)
                axis -= 1f;

            return Mathf.Clamp(axis, -1f, 1f);
#else
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow))
                axis += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                axis -= 1f;

            return Mathf.Clamp(axis, -1f, 1f);
#endif
        }

        public bool GetNewtonMarkDown()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return false;

            return kb.xKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.X);
#endif
        }

        public int GetColorChunkStep()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return 0;

            if (kb.upArrowKey.wasPressedThisFrame)
                return 1;
            if (kb.downArrowKey.wasPressedThisFrame)
                return -1;

            return 0;
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                return 1;
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                return -1;

            return 0;
#endif
        }

        public bool TryGetDirectColor(out LogicalColor color)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    color = LogicalColor.White;
                    return true;
                }
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    color = LogicalColor.Gray;
                    return true;
                }
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                {
                    color = LogicalColor.Black;
                    return true;
                }
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1))
            {
                color = LogicalColor.White;
                return true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2))
            {
                color = LogicalColor.Gray;
                return true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad3))
            {
                color = LogicalColor.Black;
                return true;
            }
#endif
            color = LogicalColor.White;
            return false;
        }

        public void ResetFrameState()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                prevWPressed = kb.wKey.isPressed;
                prevSpacePressed = kb.spaceKey.isPressed;
            }
            else
            {
                prevWPressed = false;
                prevSpacePressed = false;
            }
#else
            prevWPressed = UnityEngine.Input.GetKey(KeyCode.W);
            prevSpacePressed = UnityEngine.Input.GetKey(KeyCode.Space);
#endif
        }
    }
}
