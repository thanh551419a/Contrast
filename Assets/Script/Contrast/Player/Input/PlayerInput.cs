using Contrast.Color;
using Contrast.Input;
using Contrast.UI;
using UnityEngine;

namespace Contrast.Player
{
    /// <summary>
    /// Reads player input through platform adapters (<see cref="IPlatformInputAdapter"/>)
    /// and produces pure data <see cref="PlayerInputIntent"/>.
    /// Decouples game rules from specific hardware (Windows Keyboard/Mouse, Android Touchscreen).
    /// It never mutates Player transform and never executes collision logic.
    /// </summary>
    public sealed class PlayerInput : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logInput = true;

        private readonly IPlatformInputAdapter keyboardAdapter = new StandaloneKeyboardInputAdapter();
        private IPlatformInputAdapter customAdapter;

        public IPlatformInputAdapter KeyboardAdapter => keyboardAdapter;
        public IPlatformInputAdapter CustomAdapter => customAdapter;

        /// <summary>
        /// Allows unit/audit tests to inject a specific platform adapter for input equivalence verification.
        /// </summary>
        public void SetCustomAdapter(IPlatformInputAdapter adapter)
        {
            customAdapter = adapter;
        }

        /// <summary>
        /// Deprecated flag from legacy prototype. Kept for backward compatibility.
        /// Space is now strictly and exclusively a Jump input across all stages.
        /// </summary>
        public static bool IsNewtonFinaleActive { get; set; } = false;

        public static bool IsJumpInputPressed(bool wPressed, bool spacePressed)
        {
            return wPressed || spacePressed;
        }

        public PlayerInputIntent ReadInput(
            float deltaTime,
            float moveSpeed,
            bool isGrounded)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            Vector2 moveIntent = Vector2.zero;
            bool jumpRequested = false;
            bool fastFall = false;
            float phaseTuneAxis = 0f;
            bool newtonMark = false;
            bool hasColorChange = false;
            int colorChunkStep = 0;
            LogicalColor requestedColor = LogicalColor.White;

            if (customAdapter != null)
            {
                float axis = Mathf.Clamp(customAdapter.GetHorizontalAxis(), -1f, 1f);
                moveIntent = Vector2.right * axis * moveSpeed * deltaTime;

                bool customJumpPressed = customAdapter.GetJumpDown();
                if (customJumpPressed && isGrounded)
                    jumpRequested = true;

                fastFall = customAdapter.GetFastFall();
                phaseTuneAxis = Mathf.Clamp(customAdapter.GetPhaseTuneAxis(), -1f, 1f);
                newtonMark = customAdapter.GetNewtonMarkDown();

                colorChunkStep = customAdapter.GetColorChunkStep();
                hasColorChange = customAdapter.TryGetDirectColor(out requestedColor);
                customAdapter.ResetFrameState();

                float customPhaseDelta = phaseTuneAxis * 2.75f * deltaTime;

                return new PlayerInputIntent(
                    moveIntent,
                    jumpRequested,
                    fastFall,
                    customPhaseDelta,
                    newtonMark,
                    hasColorChange,
                    requestedColor,
                    colorChunkStep,
                    customJumpPressed);
            }

            // 1. Read from physical keyboard adapter (Windows Desktop)
            float kbAxis = keyboardAdapter.GetHorizontalAxis();
            bool kbJump = keyboardAdapter.GetJumpDown();
            bool kbFastFall = keyboardAdapter.GetFastFall();
            float kbPhaseAxis = keyboardAdapter.GetPhaseTuneAxis();
            bool kbNewtonMark = keyboardAdapter.GetNewtonMarkDown();
            int kbColorStep = keyboardAdapter.GetColorChunkStep();
            bool kbHasDirectColor = keyboardAdapter.TryGetDirectColor(out LogicalColor kbDirectColor);

            // 2. Read from virtual touch adapter (Android Mobile / Touch UI)
            IPlatformInputAdapter touchAdapter = VirtualControlsUI.Instance?.TouchAdapter;
            float touchAxis = touchAdapter != null ? touchAdapter.GetHorizontalAxis() : 0f;
            bool touchJump = touchAdapter != null && touchAdapter.GetJumpDown();
            bool touchFastFall = touchAdapter != null && touchAdapter.GetFastFall();
            float touchPhaseAxis = touchAdapter != null ? touchAdapter.GetPhaseTuneAxis() : 0f;
            bool touchNewtonMark = touchAdapter != null && touchAdapter.GetNewtonMarkDown();
            int touchColorStep = touchAdapter != null ? touchAdapter.GetColorChunkStep() : 0;
            LogicalColor touchDirectColor = LogicalColor.White;
            bool touchHasDirectColor = touchAdapter != null && touchAdapter.TryGetDirectColor(out touchDirectColor);

            // 3. Compose combined platform input (both channels produce identical commands)
            float combinedAxis = Mathf.Clamp(kbAxis + touchAxis, -1f, 1f);
            moveIntent = Vector2.right * combinedAxis * moveSpeed * deltaTime;

            bool jumpPressed = kbJump || touchJump;
            if (jumpPressed && isGrounded)
            {
                jumpRequested = true;
            }

            fastFall = kbFastFall || touchFastFall;
            phaseTuneAxis = Mathf.Clamp(kbPhaseAxis + touchPhaseAxis, -1f, 1f);
            newtonMark = kbNewtonMark || touchNewtonMark;

            float phaseTuneDelta = phaseTuneAxis * 2.75f * deltaTime;

            if (kbColorStep != 0)
                colorChunkStep = kbColorStep;
            else if (touchColorStep != 0)
                colorChunkStep = touchColorStep;

            if (kbHasDirectColor)
            {
                hasColorChange = true;
                requestedColor = kbDirectColor;
            }
            else if (touchHasDirectColor)
            {
                hasColorChange = true;
                requestedColor = touchDirectColor;
            }

            // 4. Reset transient frame states
            keyboardAdapter.ResetFrameState();
            touchAdapter?.ResetFrameState();

            if (logInput && (combinedAxis != 0f || jumpRequested || fastFall || phaseTuneAxis != 0f || newtonMark || hasColorChange || colorChunkStep != 0))
            {
                Debug.Log(
                    $"[INPUT] axis={combinedAxis:F2} " +
                    $"moveIntent={moveIntent} " +
                    $"jumpRequested={jumpRequested} " +
                    $"fastFall={fastFall} " +
                    $"phaseDelta={phaseTuneDelta:F3} " +
                    $"newtonMark={newtonMark} " +
                    $"colorStep={colorChunkStep} " +
                    $"colorChange={hasColorChange} " +
                    $"requestedColor={requestedColor}");
            }

            return new PlayerInputIntent(
                moveIntent,
                jumpRequested,
                fastFall,
                phaseTuneDelta,
                newtonMark,
                hasColorChange,
                requestedColor,
                colorChunkStep,
                jumpPressed);
        }
    }

    public readonly struct PlayerInputIntent
    {
        public readonly Vector2 Move;
        public readonly bool JumpRequested;
        public readonly bool JumpPressed;
        public readonly bool FastFall;
        public readonly float PhaseTuneDelta;
        public readonly bool NewtonMarkRequested;
        public readonly bool HasColorChange;
        public readonly LogicalColor RequestedColor;
        public readonly int ColorChunkStep;

        public PlayerInputIntent(
            Vector2 move,
            bool jumpRequested,
            bool fastFall,
            float phaseTuneDelta,
            bool newtonMarkRequested,
            bool hasColorChange,
            LogicalColor requestedColor,
            int colorChunkStep = 0,
            bool jumpPressed = false)
        {
            Move = move;
            JumpRequested = jumpRequested;
            JumpPressed = jumpPressed || jumpRequested;
            FastFall = fastFall;
            PhaseTuneDelta = phaseTuneDelta;
            NewtonMarkRequested = newtonMarkRequested;
            HasColorChange = hasColorChange;
            RequestedColor = requestedColor;
            ColorChunkStep = colorChunkStep;
        }

        // Backward compatible constructor
        public PlayerInputIntent(
            Vector2 move,
            bool jumpRequested,
            bool hasColorChange,
            LogicalColor requestedColor,
            int colorChunkStep = 0)
            : this(move, jumpRequested, false, 0f, false, hasColorChange, requestedColor, colorChunkStep, jumpRequested)
        {
        }
    }
}
