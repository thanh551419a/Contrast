using Contrast.Color;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Contrast.Player
{
    /// <summary>
    /// Reads player input and creates data only.
    /// It never changes Player position and never performs collision logic.
    /// </summary>
    public sealed class PlayerInput : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logInput = true;

        public PlayerInputIntent ReadInput(
            float deltaTime,
            float moveSpeed,
            bool isGrounded)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            Vector2 moveIntent = Vector2.zero;
            bool jumpRequested = false;
            bool hasColorChange = false;
            int colorChunkStep = 0;
            LogicalColor requestedColor = LogicalColor.White;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                if (logInput)
                    Debug.Log("[INPUT] Keyboard.current = null");

                return new PlayerInputIntent(moveIntent, false, false, requestedColor, 0);
            }

            // A/D for horizontal movement
            float inputX =
                (kb.dKey.isPressed ? 1f : 0f) -
                (kb.aKey.isPressed ? 1f : 0f);

            moveIntent = Vector2.right * inputX * moveSpeed * deltaTime;

            if (kb.spaceKey.wasPressedThisFrame && isGrounded)
                jumpRequested = true;

            // Arrow keys change color by chunk of 84
            if (kb.rightArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                colorChunkStep = 1;
            }
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            {
                colorChunkStep = -1;
            }

            // Direct digit keys: 1=White (0..84), 2=Gray (85..169), 3=Black (170..254)
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                hasColorChange = true;
                requestedColor = LogicalColor.White;
            }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Gray;
            }
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Black;
            }
#else
            // Fallback for classic InputManager
            float inputX = 0f;
            if (Input.GetKey(KeyCode.D)) inputX += 1f;
            if (Input.GetKey(KeyCode.A)) inputX -= 1f;

            moveIntent = Vector2.right * inputX * moveSpeed * deltaTime;

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                jumpRequested = true;

            // Arrow keys change color by chunk of 84
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                colorChunkStep = 1;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                colorChunkStep = -1;
            }

            // Direct digit keys
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                hasColorChange = true;
                requestedColor = LogicalColor.White;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Gray;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Black;
            }
#endif

            if (logInput && (inputX != 0f || jumpRequested || hasColorChange || colorChunkStep != 0))
            {
                Debug.Log(
                    $"[INPUT] inputX={inputX:F2} " +
                    $"moveIntent={moveIntent} " +
                    $"jumpRequested={jumpRequested} " +
                    $"colorStep={colorChunkStep} " +
                    $"colorChange={hasColorChange} " +
                    $"requestedColor={requestedColor}");
            }

            return new PlayerInputIntent(
                moveIntent,
                jumpRequested,
                hasColorChange,
                requestedColor,
                colorChunkStep);
        }
    }

    public readonly struct PlayerInputIntent
    {
        public readonly Vector2 Move;
        public readonly bool JumpRequested;
        public readonly bool HasColorChange;
        public readonly LogicalColor RequestedColor;
        public readonly int ColorChunkStep;

        public PlayerInputIntent(
            Vector2 move,
            bool jumpRequested,
            bool hasColorChange,
            LogicalColor requestedColor,
            int colorChunkStep = 0)
        {
            Move = move;
            JumpRequested = jumpRequested;
            HasColorChange = hasColorChange;
            RequestedColor = requestedColor;
            ColorChunkStep = colorChunkStep;
        }
    }
}
