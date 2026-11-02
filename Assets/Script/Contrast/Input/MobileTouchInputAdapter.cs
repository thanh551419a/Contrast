using Contrast.Color;
using UnityEngine;

namespace Contrast.Input
{
    /// <summary>
    /// Mobile touchscreen platform adapter. Translates virtual on-screen touch controls
    /// into standard gameplay commands conforming to <see cref="IPlatformInputAdapter"/>.
    /// Supports multi-touch interactions (e.g. holding Move while pressing Jump).
    /// </summary>
    public sealed class MobileTouchInputAdapter : IPlatformInputAdapter
    {
        private bool moveLeftHeld;
        private bool moveRightHeld;
        private float virtualAxisOverride;
        private bool hasAxisOverride;

        private bool jumpTriggered;
        private bool fastFallHeld;
        private bool phaseTuneLeftHeld;
        private bool phaseTuneRightHeld;
        private float phaseTuneAxisOverride;
        private bool hasPhaseTuneOverride;
        private bool newtonMarkTriggered;

        private int colorChunkStep;
        private bool hasDirectColor;
        private LogicalColor directColor;

        // ── Touch Control Event Handlers ─────────────────────────────

        public void SetMoveLeft(bool held)
        {
            moveLeftHeld = held;
        }

        public void SetMoveRight(bool held)
        {
            moveRightHeld = held;
        }

        public void SetAxisOverride(float axis)
        {
            virtualAxisOverride = Mathf.Clamp(axis, -1f, 1f);
            hasAxisOverride = true;
        }

        public void ClearAxisOverride()
        {
            hasAxisOverride = false;
            virtualAxisOverride = 0f;
        }

        public void TriggerJump()
        {
            jumpTriggered = true;
        }

        public void SetFastFall(bool held)
        {
            fastFallHeld = held;
        }

        public void SetPhaseTuneLeft(bool held)
        {
            phaseTuneLeftHeld = held;
        }

        public void SetPhaseTuneRight(bool held)
        {
            phaseTuneRightHeld = held;
        }

        public void SetPhaseTuneAxisOverride(float axis)
        {
            phaseTuneAxisOverride = Mathf.Clamp(axis, -1f, 1f);
            hasPhaseTuneOverride = true;
        }

        public void ClearPhaseTuneAxisOverride()
        {
            hasPhaseTuneOverride = false;
            phaseTuneAxisOverride = 0f;
        }

        public void TriggerNewtonMark()
        {
            newtonMarkTriggered = true;
        }

        public void TriggerColorChunkStep(int step)
        {
            colorChunkStep = step;
        }

        public void TriggerDirectColor(LogicalColor color)
        {
            hasDirectColor = true;
            directColor = color;
        }

        // ── IPlatformInputAdapter Implementation ──────────────────────

        public float GetHorizontalAxis()
        {
            if (hasAxisOverride)
                return virtualAxisOverride;

            float axis = 0f;
            if (moveRightHeld)
                axis += 1f;
            if (moveLeftHeld)
                axis -= 1f;

            return Mathf.Clamp(axis, -1f, 1f);
        }

        public bool GetJumpDown()
        {
            return jumpTriggered;
        }

        public bool GetFastFall()
        {
            return fastFallHeld;
        }

        public float GetPhaseTuneAxis()
        {
            if (hasPhaseTuneOverride)
                return phaseTuneAxisOverride;

            float axis = 0f;
            if (phaseTuneRightHeld)
                axis += 1f;
            if (phaseTuneLeftHeld)
                axis -= 1f;

            return Mathf.Clamp(axis, -1f, 1f);
        }

        public bool GetNewtonMarkDown()
        {
            return newtonMarkTriggered;
        }

        public int GetColorChunkStep()
        {
            return colorChunkStep;
        }

        public bool TryGetDirectColor(out LogicalColor color)
        {
            color = directColor;
            return hasDirectColor;
        }

        public void ResetFrameState()
        {
            jumpTriggered = false;
            newtonMarkTriggered = false;
            colorChunkStep = 0;
            hasDirectColor = false;
        }
    }
}
