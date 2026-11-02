using Contrast.Color;

namespace Contrast.Input
{
    /// <summary>
    /// Contract for platform-specific input adapters (Windows Desktop keyboard/mouse,
    /// Android touchscreen virtual controls, or custom input sources).
    /// Decouples Game Core and PlayerController from underlying platform hardware.
    /// </summary>
    public interface IPlatformInputAdapter
    {
        /// <summary>
        /// Reads the normalized horizontal movement intent in range [-1f, 1f].
        /// Windows: A (left), D (right). Arrow keys are strictly excluded.
        /// Android: Virtual left/right buttons.
        /// </summary>
        float GetHorizontalAxis();

        /// <summary>
        /// Returns true if jump intent was triggered in the current frame.
        /// Windows: W or Space. Space is strictly jump.
        /// Android: Virtual jump button.
        /// </summary>
        bool GetJumpDown();

        /// <summary>
        /// Returns true if fast-fall intent is active (holding S on keyboard or fast-fall touch button).
        /// </summary>
        bool GetFastFall();

        /// <summary>
        /// Reads continuous phase tuning intent in range [-1f, 1f].
        /// Windows: Left Arrow (-1f), Right Arrow (+1f).
        /// Android: Virtual tune left/right buttons.
        /// </summary>
        float GetPhaseTuneAxis();

        /// <summary>
        /// Returns true if Newton measurement mark was triggered this frame.
        /// Windows: X key only.
        /// Android: Virtual X (mark) button.
        /// </summary>
        bool GetNewtonMarkDown();

        /// <summary>
        /// Chunk step for color cycling: +1 (next chunk), -1 (prev chunk), 0 (none).
        /// Kept for backward compatibility.
        /// </summary>
        int GetColorChunkStep();

        /// <summary>
        /// Returns true if a direct color selection (White / Gray / Black) was requested this frame.
        /// </summary>
        bool TryGetDirectColor(out LogicalColor color);

        /// <summary>
        /// Resets single-frame transient input events (jump, one-shot color shifts, newton mark) after evaluation.
        /// </summary>
        void ResetFrameState();
    }
}
