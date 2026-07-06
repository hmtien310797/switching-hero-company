using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    public class GameLifecycleController : Singleton<GameLifecycleController>
    {
        [Header("Config")]
        [SerializeField] private bool triggerResumeOnStart = true;
        [SerializeField] private float resumeDelayOnStart = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private bool hasPaused;
        private bool isQuitting;

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void Start()
        {
            if (triggerResumeOnStart)
                TriggerResumeOnStartAsync().Forget();
        }

        private async UniTaskVoid TriggerResumeOnStartAsync()
        {
            if (resumeDelayOnStart > 0f)
                await UniTask.Delay((int)(resumeDelayOnStart * 1000f));

            if (this == null)
                return;

            if (enableDebugLog)
                Debug.Log("[GameLifecycle] App Resumed On Start");

            GameEventManager.Trigger(GameEvents.OnAppResumed);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                hasPaused = true;

                if (enableDebugLog)
                    Debug.Log("[GameLifecycle] App Paused");

                GameEventManager.Trigger(GameEvents.OnAppPaused);
            }
            else
            {
                if (!hasPaused)
                    return;

                hasPaused = false;

                if (enableDebugLog)
                    Debug.Log("[GameLifecycle] App Resumed");

                GameEventManager.Trigger(GameEvents.OnAppResumed);
            }
        }

        private void OnApplicationQuit()
        {
            if (isQuitting)
                return;

            isQuitting = true;

            if (enableDebugLog)
                Debug.Log("[GameLifecycle] App Quit");

            GameEventManager.Trigger(GameEvents.OnAppQuit);
        }
    }
}