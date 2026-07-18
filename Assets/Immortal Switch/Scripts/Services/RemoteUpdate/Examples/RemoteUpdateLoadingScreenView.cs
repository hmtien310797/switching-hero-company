using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Loading.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.RemoteUpdate.Examples
{
    /// <summary>
    /// Example loading-screen UIView that shows Addressable remote content
    /// download progress with a progress bar, status text, size label,
    /// and retry / skip-offline buttons.
    ///
    /// Attach this to a UI prefab and open it before calling
    /// <see cref="AddressableRemoteUpdateService.CheckAndDownloadUpdatesAsync"/>.
    ///
    /// This view implements <see cref="IRemoteUpdateProgressHandler"/> so it
    /// can be passed directly into the service:
    /// <code>
    /// var view = await UIManager.Instance.OpenPopupAsync&gt;RemoteUpdateLoadingScreenView&lt;();
    /// var service = AddressableRemoteUpdateService.Instance;
    /// var result = await service.DownloadRequiredContentAsync(view, token);
    /// if (result.IsSuccess) { /* proceed */ }
    /// </code>
    /// </summary>
    public class RemoteUpdateLoadingScreenView : UIView, IRemoteUpdateProgressHandler
    {
        // ── Serialized UI references ──────────────────────────────────────

        [Header("Progress")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _sizeText;
        [SerializeField] private TMP_Text _percentText;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _skipOfflineButton;
        [SerializeField] private Button _quitButton;

        [Header("Settings")]
        [SerializeField] private bool _quitOnFailure = true;

        // ── Internal state ────────────────────────────────────────────────

        private AddressableRemoteUpdateService _service;
        private System.Threading.CancellationTokenSource _cts;
        private RemoteContentUpdateResult _lastResult;
        private bool _isRunning;

        // ── UIView overrides ──────────────────────────────────────────────

        public override async UniTask PlayShowAsync(object args)
        {
            _service = AddressableRemoteUpdateService.Instance;
            _cts = new System.Threading.CancellationTokenSource();

            // Wire buttons.
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);

            if (_skipOfflineButton != null)
            {
                _skipOfflineButton.onClick.AddListener(OnSkipOfflineClicked);
                _skipOfflineButton.gameObject.SetActive(false);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
                _quitButton.gameObject.SetActive(false);
            }

            await base.PlayShowAsync(args);

            // Kick off the update check.
            RunUpdateAsync().Forget();
        }

        public override UniTask PlayHideAsync()
        {
            _cts?.Cancel();
            Cleanup();
            return base.PlayHideAsync();
        }

        private void Cleanup()
        {
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(OnRetryClicked);

            if (_skipOfflineButton != null)
                _skipOfflineButton.onClick.RemoveListener(OnSkipOfflineClicked);

            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OnQuitClicked);

            _cts?.Dispose();
            _cts = null;
        }

        // ── IRemoteUpdateProgressHandler implementation ───────────────────

        void IRemoteUpdateProgressHandler.OnProgress(RemoteContentUpdateProgress progress)
        {
            // The service invokes callbacks from the main thread via
            // UniTask.Yield(PlayerLoopTiming.Update). If your setup differs,
            // dispatch to main thread here:
            //   UniTask.PostToMainThread(() => ApplyProgress(progress));
            ApplyProgress(progress);
        }

        void IRemoteUpdateProgressHandler.OnComplete(RemoteContentUpdateResult result)
        {
            ApplyComplete(result);
        }

        // ── UI update methods ─────────────────────────────────────────────

        private void ApplyProgress(RemoteContentUpdateProgress progress)
        {
            if (_progressBar != null)
                _progressBar.value = progress.Percent;

            if (_statusText != null)
                _statusText.text = StatusToLocalisedText(progress.Status);

            if (_sizeText != null && progress.TotalBytes > 0)
            {
                _sizeText.text =
                    $"{FormatBytes(progress.DownloadedBytes)} / {FormatBytes(progress.TotalBytes)}";
            }

            if (_percentText != null)
                _percentText.text = $"{progress.Percent * 100f:F0}%";
        }

        private void ApplyComplete(RemoteContentUpdateResult result)
        {
            _lastResult = result;
            _isRunning = false;

            if (result.IsSuccess)
            {
                // Update finished — the caller should close this view.
                if (_statusText != null)
                    _statusText.text = "Update complete!";
                if (_percentText != null)
                    _percentText.text = "100%";
                if (_progressBar != null)
                    _progressBar.value = 1f;
                return;
            }

            // Failure — show appropriate buttons.
            if (_statusText != null)
            {
                _statusText.text = result.Status == RemoteContentUpdateStatus.Timeout
                    ? "Update timed out. Check your connection."
                    : $"Update failed: {string.Join("\n", result.Errors)}";
            }

            if (_retryButton != null)
                _retryButton.gameObject.SetActive(true);

            if (result.Status == RemoteContentUpdateStatus.Offline)
            {
                if (_skipOfflineButton != null)
                    _skipOfflineButton.gameObject.SetActive(true);
            }

            if (_quitButton != null && _quitOnFailure)
                _quitButton.gameObject.SetActive(true);
        }

        // ── Button handlers ───────────────────────────────────────────────

        private void OnRetryClicked()
        {
            if (_retryButton != null)
                _retryButton.gameObject.SetActive(false);
            if (_skipOfflineButton != null)
                _skipOfflineButton.gameObject.SetActive(false);
            if (_quitButton != null)
                _quitButton.gameObject.SetActive(false);

            // Reset UI.
            if (_progressBar != null) _progressBar.value = 0f;
            if (_statusText != null) _statusText.text = "Retrying…";
            if (_sizeText != null) _sizeText.text = string.Empty;
            if (_percentText != null) _percentText.text = "0%";

            // Re-create token source and retry.
            _cts?.Dispose();
            _cts = new System.Threading.CancellationTokenSource();
            _isRunning = true;

            RunUpdateAsync().Forget();
        }

        private void OnSkipOfflineClicked()
        {
            Debug.Log("[RemoteUpdate] User chose to skip update — running offline.");
            // The caller should check _lastResult.IsSuccess and decide to proceed.
            // Mark as "done" by setting a success-like state.
            _lastResult = new RemoteContentUpdateResult(
                RemoteContentUpdateStatus.Offline, _lastResult.ElapsedTime,
                false, false, 0, 0);
            _isRunning = false;
        }

        private void OnQuitClicked()
        {
            Debug.Log("[RemoteUpdate] User chose to quit.");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ── Update runner ─────────────────────────────────────────────────

        private async UniTaskVoid RunUpdateAsync()
        {
            if (_isRunning) return;
            _isRunning = true;

            try
            {
                var result = await _service.CheckAndDownloadUpdatesAsync(
                    this, _cts.Token);

                // If the result is already handled by OnComplete, we're done.
                // But if the caller is polling this view, expose the result.
                _lastResult = result;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[RemoteUpdate] Update cancelled by user.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteUpdate] Unexpected error: {ex}");
            }
        }

        // ── Public query ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the last known result. Poll this after the view closes
        /// to decide whether to proceed into the game.
        /// </summary>
        public RemoteContentUpdateResult LastResult => _lastResult;

        /// <summary>
        /// True while the update pipeline is still running.
        /// </summary>
        public bool IsRunning => _isRunning;

        // ── Helpers ───────────────────────────────────────────────────────

        private static string StatusToLocalisedText(RemoteContentUpdateStatus status)
        {
            // In production, use the Unity Localization package.
            // These strings are English fallbacks.
            return status switch
            {
                RemoteContentUpdateStatus.Initializing => "Initialising…",
                RemoteContentUpdateStatus.CheckingForUpdates => "Checking for updates…",
                RemoteContentUpdateStatus.NoUpdateNeeded => "Content is up to date.",
                RemoteContentUpdateStatus.UpdatingCatalogs => "Updating catalog…",
                RemoteContentUpdateStatus.CalculatingDownloadSize => "Calculating size…",
                RemoteContentUpdateStatus.Downloading => "Downloading…",
                RemoteContentUpdateStatus.Complete => "Update complete!",
                RemoteContentUpdateStatus.Failed => "Update failed.",
                RemoteContentUpdateStatus.Offline => "No internet connection.",
                RemoteContentUpdateStatus.Timeout => "Connection timed out.",
                RemoteContentUpdateStatus.Cancelled => "Cancelled.",
                _ => "Please wait…"
            };
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {suffixes[order]}";
        }
    }
}
