using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class ScreenOrientationTracker : Singleton<ScreenOrientationTracker>
    {
        public enum ScreenViewMode
        {
            Portrait,
            Landscape,
            None = -1,
        }

        [Header("Forced lock screen")]
        [SerializeField]
        private ScreenViewMode forcedOrientation = ScreenViewMode.None;

        [Header("Debug")]
        [SerializeField]
        private bool logChanged = true;

        public ScreenViewMode CurrentMode { get; private set; }

        public bool IsOrientationLocked { get; private set; }

        public ScreenOrientation LockedOrientation { get; private set; }

        public event Action<ScreenViewMode> OnOrientationChanged;

        private Vector2Int lastScreenSize;
        private ScreenViewMode lastMode;

        protected override void Awake()
        {
            base.Awake();
            ForceRefresh();

            if (forcedOrientation != ScreenViewMode.None)
            {
                Screen.orientation = forcedOrientation switch
                {
                    ScreenViewMode.Portrait => ScreenOrientation.Portrait,
                    ScreenViewMode.Landscape => ScreenOrientation.LandscapeLeft,
                    _ => ScreenOrientation.AutoRotation,
                };

                IsOrientationLocked = true;
            }
        }

        private void Update()
        {
            if (!HasScreenSizeChanged())
                return;

            CheckOrientationChanged();
        }

        public void ForceRefresh()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            lastMode = GetCurrentMode();
            CurrentMode = lastMode;

            if (logChanged)
                Debug.Log($"[ScreenOrientationTracker] Init: {CurrentMode}");
        }

        private bool HasScreenSizeChanged()
        {
            return (lastScreenSize.x != Screen.width ||
                    lastScreenSize.y != Screen.height) &&
                   forcedOrientation == ScreenViewMode.None;
        }

        private void CheckOrientationChanged()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            ScreenViewMode newMode = GetCurrentMode();

            if (newMode == lastMode)
                return;

            lastMode = newMode;
            CurrentMode = newMode;

            if (logChanged)
                Debug.Log($"[ScreenOrientationTracker] Orientation Changed: {CurrentMode}");

            OnOrientationChanged?.Invoke(CurrentMode);
        }

        private ScreenViewMode GetCurrentMode()
        {
            if (forcedOrientation == ScreenViewMode.None)
            {
                return Screen.height >= Screen.width
                    ? ScreenViewMode.Portrait
                    : ScreenViewMode.Landscape;
            }

            return forcedOrientation;
        }

        /// <summary>
        /// Khóa màn hình theo đúng orientation hiện tại của thiết bị.
        /// </summary>
        public void LockCurrentOrientation()
        {
            if (forcedOrientation == ScreenViewMode.None)
            {
                LockedOrientation = GetCurrentScreenOrientation();
                Screen.orientation = LockedOrientation;
                IsOrientationLocked = true;
            }
        }

        /// <summary>
        /// Mở khóa orientation và cho phép thiết bị tự động xoay lại.
        /// </summary>
        public void UnlockOrientation()
        {
            if (forcedOrientation == ScreenViewMode.None)
            {
                Screen.orientation = ScreenOrientation.AutoRotation;
                IsOrientationLocked = false;
            }
        }

        private ScreenOrientation GetCurrentScreenOrientation()
        {
            if (Screen.orientation != ScreenOrientation.AutoRotation)
            {
                return Screen.orientation;
            }

            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.Portrait:
                    return ScreenOrientation.Portrait;

                case DeviceOrientation.PortraitUpsideDown:
                    return ScreenOrientation.PortraitUpsideDown;

                case DeviceOrientation.LandscapeLeft:
                    return ScreenOrientation.LandscapeLeft;

                case DeviceOrientation.LandscapeRight:
                    return ScreenOrientation.LandscapeRight;

                default:
                    return GetCurrentMode() == ScreenViewMode.Portrait
                        ? ScreenOrientation.Portrait
                        : ScreenOrientation.LandscapeLeft;
            }
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}