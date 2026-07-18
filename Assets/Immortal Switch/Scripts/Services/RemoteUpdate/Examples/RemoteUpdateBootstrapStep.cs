using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.RemoteUpdate.Examples
{
    /// <summary>
    /// Example adapter that plugs the Addressable remote update check into
    /// <see cref="GameBootstrap.RunAsync"/> as step 0 (before Nakama auth).
    ///
    /// Usage inside GameBootstrap:
    /// <code>
    /// // Step 0: Download required Addressable content.
    /// await RemoteUpdateBootstrapStep.RunAsync(progress, cancellationToken);
    /// </code>
    ///
    /// This adapter converts <see cref="RemoteContentUpdateProgress"/> into
    /// <see cref="BootstrapProgress"/> calls so the existing loading slider
    /// reflects download progress.
    /// </summary>
    public static class RemoteUpdateBootstrapStep
    {
        /// <summary>
        /// Runs the required-content download pipeline and reports progress
        /// via the provided callback. Call this before any Addressable-dependent
        /// manager initialisation.
        /// </summary>
        /// <param name="onProgress">
        /// Bootstrap progress callback. Receives (0→1, status message).
        /// The first 10 % of the bootstrap bar is allocated to the update check.
        /// </param>
        /// <param name="cancellationToken">
        /// Passed through from the bootstrap pipeline.
        /// </param>
        /// <returns>
        /// True if the game may proceed; false if the update failed and the
        /// caller should show an error / retry dialog.
        /// </returns>
        public static async UniTask<bool> RunAsync(
            Action<float, string> onProgress,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var service = AddressableRemoteUpdateService.Instance;

            // Wrap the Action<float, string> callback in our progress handler.
            var handler = new BootstrapProgressHandler(onProgress);
            var result = await service.DownloadRequiredContentAsync(handler, cancellationToken);

            handler.OnComplete(result);

            // Offline but cached? Proceed.
            if (result.Status == RemoteContentUpdateStatus.Offline)
            {
                Debug.Log("[RemoteUpdate] Running offline with cached content.");
                onProgress?.Invoke(0.1f, "Running offline…");
                return true;
            }

            // No update needed? Proceed.
            if (result.Status == RemoteContentUpdateStatus.NoUpdateNeeded)
            {
                Debug.Log("[RemoteUpdate] Content is up to date.");
                onProgress?.Invoke(0.1f, "Content up to date.");
                return true;
            }

            // Success? Proceed.
            if (result.Status == RemoteContentUpdateStatus.Complete)
            {
                Debug.Log($"[RemoteUpdate] Downloaded {FormatBytes(result.RequiredDownloadedBytes)} in " +
                          $"{result.ElapsedTime.TotalSeconds:F1}s.");
                onProgress?.Invoke(0.1f, "Update complete.");
                return true;
            }

            // Timeout or failed — log and return false so the caller can decide.
            Debug.LogError(
                $"[RemoteUpdate] Update failed: {result.Status}. " +
                $"Errors: {string.Join("; ", result.Errors)}");

            return false;
        }

        // ── Progress handler adapter ──────────────────────────────────────

        /// <summary>
        /// Maps <see cref="IRemoteUpdateProgressHandler"/> callbacks into the
        /// <c>(float percent, string message)</c> format used by BootstrapProgress.
        /// </summary>
        private sealed class BootstrapProgressHandler : IRemoteUpdateProgressHandler
        {
            private readonly Action<float, string> _onProgress;
            private RemoteContentUpdateProgress _lastProgress;

            public BootstrapProgressHandler(Action<float, string> onProgress)
            {
                _onProgress = onProgress;
            }

            public void OnProgress(RemoteContentUpdateProgress progress)
            {
                _lastProgress = progress;

                // Map the overall pipeline to the first 10 % of the bootstrap bar.
                float bootstrapPercent = 0.1f * progress.Percent;

                // For non-downloading phases, report status with a nominal fraction.
                if (progress.Status != RemoteContentUpdateStatus.Downloading &&
                    progress.Status != RemoteContentUpdateStatus.Complete)
                {
                    bootstrapPercent = 0.01f; // at least some movement
                }

                _onProgress?.Invoke(bootstrapPercent, progress.CurrentLabel);
            }

            public void OnComplete(RemoteContentUpdateResult result)
            {
                if (result.IsSuccess)
                {
                    _onProgress?.Invoke(0.1f, "Content ready.");
                }
                else
                {
                    var msg = result.Status == RemoteContentUpdateStatus.Timeout
                        ? "Update timed out."
                        : $"Update failed: {string.Join(", ", result.Errors)}";
                    _onProgress?.Invoke(0f, msg);
                }
            }
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
