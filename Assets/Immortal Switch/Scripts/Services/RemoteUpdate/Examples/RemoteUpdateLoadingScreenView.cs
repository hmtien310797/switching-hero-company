using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.RemoteUpdate.Examples
{
    /// <summary>
    /// Passive loading-screen UIView that displays Addressable remote content
    /// download progress. This view does NOT start the pipeline — it only
    /// receives progress and completion callbacks via
    /// <see cref="IRemoteUpdateProgressHandler"/>.
    ///
    /// The pipeline is owned exclusively by <see cref="Immortal_Switch.Scripts.Core.GameBootstrap"/>.
    ///
    /// Usage:
    /// <code>
    /// // In GameBootstrap or another owner:
    /// var view = GetComponent&lt;RemoteUpdateLoadingScreenView&gt;();
    /// var result = await service.DownloadRequiredContentAsync(view, token);
    /// // view now shows progress bars and status text automatically.
    /// </code>
    /// </summary>
    public class RemoteUpdateLoadingScreenView : MonoBehaviour, IRemoteUpdateProgressHandler
    {
        // ── Serialized UI references ──────────────────────────────────────

        [Header("Progress")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _sizeText;
        [SerializeField] private TMP_Text _percentText;

        // ── Internal state ────────────────────────────────────────────────

        private RemoteContentUpdateResult _lastResult;
        private bool _isComplete;

        // ── Public query ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the last known result. Poll this after completion
        /// to decide whether to proceed.
        /// </summary>
        public RemoteContentUpdateResult LastResult => _lastResult;

        /// <summary>
        /// True after OnComplete has been called.
        /// </summary>
        public bool IsComplete => _isComplete;

        // ── IRemoteUpdateProgressHandler implementation ───────────────────

        void IRemoteUpdateProgressHandler.OnProgress(RemoteContentUpdateProgress progress)
        {
            ApplyProgress(progress);
        }

        void IRemoteUpdateProgressHandler.OnComplete(RemoteContentUpdateResult result)
        {
            _lastResult = result;
            _isComplete = true;
            ApplyComplete(result);
        }

        // ── UI update methods ─────────────────────────────────────────────

        private void ApplyProgress(RemoteContentUpdateProgress progress)
        {
            if (_progressBar != null)
                _progressBar.value = progress.Percent;

            if (_statusText != null)
                _statusText.text = progress.CurrentLabel;

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
            if (result.IsSuccess)
            {
                if (_statusText != null)
                    _statusText.text = "Update complete!";
                if (_percentText != null)
                    _percentText.text = "100%";
                if (_progressBar != null)
                    _progressBar.value = 1f;
                return;
            }

            if (result.Status == RemoteContentUpdateStatus.Offline)
            {
                // Do not force the bar to 100% here.
                // GameBootstrap still needs to validate that Required content
                // is actually available in the local cache.
                if (_statusText != null)
                    _statusText.text = "Offline — checking cached content…";

                return;
            }

            // Failed / Timeout / Cancelled.
            if (_statusText != null)
            {
                _statusText.text = result.Status switch
                {
                    RemoteContentUpdateStatus.Timeout =>
                        "Update timed out. Check your connection.",
                    RemoteContentUpdateStatus.Cancelled =>
                        "Update cancelled.",
                    _ => $"Update failed: {string.Join("\n", result.Errors)}"
                };
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

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
