using System;
using Cysharp.Threading.Tasks;

namespace Immortal_Switch.Scripts.RemoteUpdate.Examples
{
    /// <summary>
    /// Reusable bootstrap-step utility that calls
    /// <see cref="AddressableRemoteUpdateService.DownloadRequiredContentAsync"/>
    /// and maps progress into an <c>Action&lt;float, string&gt;</c> callback.
    ///
    /// Used by <see cref="Immortal_Switch.Scripts.Core.GameBootstrap"/> as step 0
    /// of the startup pipeline. Also usable as a standalone entry point for
    /// scenes or flows that need to check for remote updates independently.
    ///
    /// <b>Important:</b> <see cref="Immortal_Switch.Scripts.Core.GameBootstrap"/>
    /// is the sole owner of the Required pipeline during startup.
    /// Other callers must not invoke <see cref="RunAsync"/> while the bootstrap
    /// is in progress. Use <see cref="DownloadOptionalAsync"/> for deferred
    /// content after the game has started.
    ///
    /// Usage:
    /// <code>
    /// var result = await RemoteUpdateBootstrapStep.RunAsync(
    ///     (percent, message) => progressBar.Report(percent, message),
    ///     cancellationToken);
    /// if (result.IsSuccess) { /* proceed into game */ }
    /// else if (result.Status == RemoteContentUpdateStatus.Offline
    ///          &amp;&amp; await AddressableRemoteUpdateService.Instance.IsContentAvailableOfflineAsync())
    ///     { /* proceed offline */ }
    /// </code>
    /// </summary>
    public static class RemoteUpdateBootstrapStep
    {
        /// <summary>
        /// Runs the required-content download pipeline and reports progress
        /// via the provided callback.
        /// </summary>
        /// <param name="onProgress">
        /// Receives (0→1 normalised progress, status message).
        /// The first portion of your overall loading bar should be allocated
        /// to this phase (e.g. 0 % – 15 %).
        /// </param>
        /// <param name="cancellationToken">
        /// Propagated to the remote update service so cancellation flows
        /// through the entire pipeline.
        /// </param>
        /// <returns>
        /// The terminal result. Check <see cref="RemoteContentUpdateResult.IsSuccess"/>
        /// before proceeding into gameplay. For Offline status, validate cached
        /// content separately via <see cref="AddressableRemoteUpdateService.IsContentAvailableOfflineAsync"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the service is already running a pipeline (dual-owner guard).
        /// </exception>
        public static async UniTask<RemoteContentUpdateResult> RunAsync(
            Action<float, string> onProgress = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var service = AddressableRemoteUpdateService.Instance;

            // ── Dual-owner guard ─────────────────────────────────────────
            // GameBootstrap is the sole owner of the Required pipeline during
            // startup. If another caller tries to run concurrently, fail fast.
            if (service.IsRunning)
            {
                throw new InvalidOperationException(
                    "Addressable remote update is already running. " +
                    "Only one caller may own the Required pipeline at a time. " +
                    "Use DownloadOptionalContentAsync() for deferred content.");
            }

            // Wrap the Action<float, string> in our progress handler.
            var handler = new ProgressHandler(onProgress);
            return await service.DownloadRequiredContentAsync(handler, cancellationToken);
        }

        /// <summary>
        /// Downloads optional content only (e.g. high-res textures, voice-over).
        /// Call this from the main menu or settings screen after the game has started
        /// and the Required pipeline has completed.
        /// </summary>
        public static async UniTask<RemoteContentUpdateResult> DownloadOptionalAsync(
            Action<float, string> onProgress = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var service = AddressableRemoteUpdateService.Instance;
            var handler = new ProgressHandler(onProgress);
            return await service.DownloadOptionalContentAsync(handler, cancellationToken);
        }

        // ── Progress handler adapter ──────────────────────────────────────

        /// <summary>
        /// Maps <see cref="IRemoteUpdateProgressHandler"/> callbacks into
        /// <c>(float percent, string message)</c> callbacks for simple UIs.
        ///
        /// <b>Important:</b> <see cref="OnComplete"/> does NOT change the
        /// progress bar. It only reports the final status via the message.
        /// The last progress value from <see cref="OnProgress"/> is preserved.
        /// The caller (GameBootstrap) handles result-specific progress after
        /// cache validation.
        /// </summary>
        private sealed class ProgressHandler : IRemoteUpdateProgressHandler
        {
            private readonly Action<float, string> _onProgress;
            private float _lastPercent;

            public ProgressHandler(Action<float, string> onProgress)
            {
                _onProgress = onProgress;
            }

            void IRemoteUpdateProgressHandler.OnProgress(RemoteContentUpdateProgress progress)
            {
                _lastPercent = progress.Percent;
                _onProgress?.Invoke(progress.Percent, progress.CurrentLabel);
            }

            void IRemoteUpdateProgressHandler.OnComplete(RemoteContentUpdateResult result)
            {
                // Do NOT change the progress bar here.
                // The last OnProgress call already reported the terminal percent.
                // Forcing 1f or 0f would:
                //  - Show "100%" before cache validation (Offline case)
                //  - Overwrite the service's accurate terminal progress
                //  - Mislead the caller about when the phase actually ended
                //
                // The caller (GameBootstrap) handles result status after await
                // and updates progress accordingly (e.g. CompleteReservedSteps).
                //
                // We only report the final status message at the last known
                // progress position.
                var message = result.Status switch
                {
                    RemoteContentUpdateStatus.Complete
                        or RemoteContentUpdateStatus.NoUpdateNeeded
                        => "Content ready.",

                    RemoteContentUpdateStatus.Offline
                        => "Offline — checking cached content…",

                    RemoteContentUpdateStatus.Timeout
                        => "Update timed out.",

                    RemoteContentUpdateStatus.Cancelled
                        => "Update cancelled.",

                    RemoteContentUpdateStatus.Failed
                        => $"Update failed: {string.Join(", ", result.Errors)}",

                    _ => $"Update ended: {result.Status}"
                };

                _onProgress?.Invoke(_lastPercent, message);
            }
        }
    }
}
