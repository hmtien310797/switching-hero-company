using UnityEngine;
using UnityEngine.InputSystem;

namespace Immortal_Switch.Scripts.Fx
{
    /// <summary>
    /// Optional convenience component: listens for raw pointer presses (mouse left button or the
    /// first touch) using the Input System package and spawns a <see cref="ClickFxManager"/> effect
    /// at the pointer position.
    ///
    /// It reads raw device state directly — it does NOT add any raycast blocker, so clicks still
    /// reach the UI below. Add it to a persistent GameObject to enable tap feedback, or drop it on a
    /// <see cref="UIView"/> to limit it to a specific screen (set <c>ClickFxManager.listenForClicks
    /// = false</c> when you want scoped control instead of the global listener).
    /// </summary>
    public sealed class ScreenClickFxListener : MonoBehaviour
    {
        /// <summary>Number of active listener instances (used by ClickFxManager to avoid double-spawn).</summary>
        public static int ActiveCount { get; private set; }

        private void OnEnable() => ActiveCount++;

        private void OnDisable() => ActiveCount--;

        private void Update()
        {
            if (TryGetPointerPress(out var screenPos) && ClickFxManager.Instance != null)
                ClickFxManager.Instance.Play(screenPos);
        }

        /// <summary>
        /// Read whether a pointer went down this frame (Input System raw device state).
        /// Returns the press position in screen space. Uses the first touch, else the mouse.
        /// </summary>
        public static bool TryGetPointerPress(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            // Touch takes priority (on mobile, Mouse.current is often a simulated fallback).
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}