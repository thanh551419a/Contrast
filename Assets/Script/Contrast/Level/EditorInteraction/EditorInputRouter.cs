using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Contrast.Level.EditorInteraction
{
    /// <summary>
    /// Routes raw Unity input, manages pointer capture / interaction ownership,
    /// guards viewport operations against UI collision, and isolates keyboard shortcuts
    /// from focused text input fields.
    /// </summary>
    public sealed class EditorInputRouter
    {
        private object activeInteractionOwner;

        /// <summary>
        /// True if an interaction currently owns pointer capture (e.g. handle dragging,
        /// platform moving, camera panning).
        /// </summary>
        public bool HasPointerCapture => activeInteractionOwner != null;

        /// <summary>
        /// Current owner of pointer capture.
        /// </summary>
        public object InteractionOwner => activeInteractionOwner;

        /// <summary>
        /// Attempt to acquire pointer capture for an ongoing interaction.
        /// </summary>
        public bool CapturePointer(object owner)
        {
            if (owner == null)
                return false;

            if (activeInteractionOwner != null && activeInteractionOwner != owner)
                return false;

            activeInteractionOwner = owner;
            return true;
        }

        /// <summary>
        /// Release pointer capture when an operation completes or is cancelled.
        /// </summary>
        public void ReleasePointer(object owner)
        {
            if (activeInteractionOwner == owner)
            {
                activeInteractionOwner = null;
            }
        }

        /// <summary>
        /// Force-releases pointer capture regardless of owner.
        /// </summary>
        public void ForceReleasePointer()
        {
            activeInteractionOwner = null;
        }

        /// <summary>
        /// True if the specified owner currently holds pointer capture.
        /// </summary>
        public bool IsOwner(object owner)
        {
            return activeInteractionOwner != null && activeInteractionOwner == owner;
        }

        /// <summary>
        /// Returns true if the pointer is currently over a Unity UI element (Canvas Graphic).
        /// </summary>
        public bool IsPointerOverUi()
        {
            return EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Validates whether a new viewport pointer interaction may begin.
        /// Once captured, ongoing interactions remain valid even if the cursor passes over UI.
        /// </summary>
        public bool CanBeginViewportInteraction()
        {
            return !IsPointerOverUi();
        }

        /// <summary>
        /// Returns true when a UI InputField currently has keyboard focus,
        /// preventing editor shortcuts from firing while the user is typing.
        /// </summary>
        public bool IsInputFieldFocused()
        {
            if (EventSystem.current == null)
                return false;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return false;

            return selected.GetComponent<UnityEngine.UI.InputField>() != null;
        }

        /// <summary>
        /// Current screen-space pointer position.
        /// </summary>
        public Vector2 GetScreenPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>
        /// Vertical scroll delta from mouse wheel.
        /// </summary>
        public float GetScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        /// <summary>
        /// Convert a screen coordinate into world-space using Camera.main.
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
                return screenPosition;

            Vector3 screen = new Vector3(
                screenPosition.x,
                screenPosition.y,
                Mathf.Abs(cam.transform.position.z));

            Vector3 world = cam.ScreenToWorldPoint(screen);
            return new Vector2(world.x, world.y);
        }

        public Vector2 GetWorldPointerPosition()
        {
            return ScreenToWorld(GetScreenPointerPosition());
        }

        // ── Pointer Button State ─────────────────────────────────────

        public bool IsLeftPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public bool IsLeftPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        public bool IsLeftPointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        public bool IsMiddlePointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }

        public bool IsMiddlePointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.isPressed;
#else
            return Input.GetMouseButton(2);
#endif
        }

        public bool IsMiddlePointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(2);
#endif
        }

        // ── Keyboard Shortcuts ───────────────────────────────────────

        /// <summary>
        /// True if Delete or Backspace was pressed while no UI InputField has focus.
        /// </summary>
        public bool IsDeleteShortcutPressed()
        {
            if (IsInputFieldFocused())
                return false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace);
#endif
        }

        /// <summary>
        /// True if Escape was pressed while no UI InputField has focus.
        /// </summary>
        public bool IsCancelShortcutPressed()
        {
            if (IsInputFieldFocused())
                return false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>
        /// True if 'F' was pressed to Frame All / Fit Map while no UI InputField has focus.
        /// </summary>
        public bool IsFrameAllShortcutPressed()
        {
            if (IsInputFieldFocused())
                return false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }
    }
}
