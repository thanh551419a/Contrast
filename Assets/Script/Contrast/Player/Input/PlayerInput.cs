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
            LogicalColor requestedColor = LogicalColor.Black;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                if (logInput)
                    Debug.Log("[INPUT] Keyboard.current = null");

                return new PlayerInputIntent(moveIntent, false, false, requestedColor);
            }

            float inputX =
                ((kb.rightArrowKey.isPressed || kb.dKey.isPressed) ? 1f : 0f) -
                ((kb.leftArrowKey.isPressed || kb.aKey.isPressed) ? 1f : 0f);

            moveIntent = Vector2.right * inputX * moveSpeed * deltaTime;

            if (kb.spaceKey.wasPressedThisFrame && isGrounded)
                jumpRequested = true;

            if (kb.digit1Key.wasPressedThisFrame)
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Black;
            }
            else if (kb.digit2Key.wasPressedThisFrame)
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Gray;
            }
            else if (kb.digit3Key.wasPressedThisFrame)
            {
                hasColorChange = true;
                requestedColor = LogicalColor.White;
            }
#else
            float inputX = Input.GetAxisRaw("Horizontal");
            moveIntent = Vector2.right * inputX * moveSpeed * deltaTime;

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                jumpRequested = true;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Black;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                hasColorChange = true;
                requestedColor = LogicalColor.Gray;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                hasColorChange = true;
                requestedColor = LogicalColor.White;
            }
#endif

            if (logInput)
            {
                Debug.Log(
                    $"[INPUT] inputX={moveIntent.x / Mathf.Max(0.0001f, moveSpeed * deltaTime):F2} " +
                    $"moveIntent={moveIntent} " +
                    $"jumpRequested={jumpRequested} " +
                    $"colorChange={hasColorChange} " +
                    $"requestedColor={requestedColor}");
            }

            return new PlayerInputIntent(
                moveIntent,
                jumpRequested,
                hasColorChange,
                requestedColor);
        }
    }

    public readonly struct PlayerInputIntent
    {
        public readonly Vector2 Move;
        public readonly bool JumpRequested;
        public readonly bool HasColorChange;
        public readonly LogicalColor RequestedColor;

        public PlayerInputIntent(
            Vector2 move,
            bool jumpRequested,
            bool hasColorChange,
            LogicalColor requestedColor)
        {
            Move = move;
            JumpRequested = jumpRequested;
            HasColorChange = hasColorChange;
            RequestedColor = requestedColor;
        }
    }
}
