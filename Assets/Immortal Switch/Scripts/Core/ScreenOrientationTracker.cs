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
            Landscape
        }

        [Header("Debug")]
        [SerializeField] private bool logChanged = true;

        public ScreenViewMode CurrentMode { get; private set; }

        public event Action<ScreenViewMode> OnOrientationChanged;

        private Vector2Int lastScreenSize;
        private ScreenViewMode lastMode;

        protected override void Awake()
        {
            base.Awake();
            ForceRefresh();
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
            return lastScreenSize.x != Screen.width ||
                   lastScreenSize.y != Screen.height;
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
            return Screen.height >= Screen.width
                ? ScreenViewMode.Portrait
                : ScreenViewMode.Landscape;
        }

        public override UniTask InitializeAsync()
        {
            throw new NotImplementedException();
        }
    }
}