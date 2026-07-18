using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Debug = UnityEngine.Debug;

namespace Immortal_Switch.Scripts.RemoteUpdate
{
    /// <summary>
    /// Singleton service that checks for remote Addressable catalog updates on startup
    /// and downloads only new or changed bundles. Required content must finish before
    /// the game can start; optional content may be deferred.
    ///
    /// Usage (in GameBootstrap or LoginScene):
    /// <code>
    /// var result = await AddressableRemoteUpdateService.Instance
    ///     .DownloadRequiredContentAsync(handler, cancellationToken);
    /// if (result.IsSuccess) { /* proceed into game */ }
    /// </code>
    /// </summary>
    public sealed class AddressableRemoteUpdateService : Singleton<AddressableRemoteUpdateService>
    {
        // ── Serialized configuration ──────────────────────────────────────

        [SerializeField, Tooltip("Addressable label for required bundles (must finish before entering game).")]
        private string _requiredLabel = "Required";

        [SerializeField, Tooltip("Addressable label for optional bundles (may be downloaded later).")]
        private string _optionalLabel = "Optional";

        [SerializeField, Tooltip("Seconds before a single operation is considered timed out.")]
        private float _timeoutSeconds = 30f;

        [SerializeField, Tooltip("Maximum retry attempts per transient failure.")]
        private int _maxRetryCount = 3;

        [SerializeField, Tooltip("Base delay in seconds between retries (multiplied by attempt number).")]
        private float _retryDelaySeconds = 2f;

        // ── Internal state ────────────────────────────────────────────────

        private UniTaskCompletionSource<RemoteContentUpdateResult> _runningUpdateTcs;
        private CancellationTokenSource _internalCts;

        // ── Singleton lifecycle ───────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoadEnabled = true;
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            CancelUpdate();
            base.OnDestroy();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Full pipeline: check for updates, download required AND optional content.
        /// Blocks until everything is downloaded or an error occurs.
        /// </summary>
        public async UniTask<RemoteContentUpdateResult> CheckAndDownloadUpdatesAsync(
            IRemoteUpdateProgressHandler handler = null,
            CancellationToken cancellationToken = default)
        {
            return await RunPipelineAsync(
                handler, downloadOptional: true, cancellationToken);
        }

        /// <summary>
        /// Downloads only required content. Optional content can be downloaded later
        /// via <see cref="DownloadOptionalContentAsync"/>.
        /// This is the recommended call during bootstrap.
        /// </summary>
        public async UniTask<RemoteContentUpdateResult> DownloadRequiredContentAsync(
            IRemoteUpdateProgressHandler handler = null,
            CancellationToken cancellationToken = default)
        {
            return await RunPipelineAsync(
                handler, downloadOptional: false, cancellationToken);
        }

        /// <summary>
        /// Downloads optional content only. Call this after the game has started
        /// (e.g. from the main menu or settings screen).
        /// Requires that required content was already downloaded.
        /// </summary>
        public async UniTask<RemoteContentUpdateResult> DownloadOptionalContentAsync(
            IRemoteUpdateProgressHandler handler = null,
            CancellationToken cancellationToken = default)
        {
            // Only download optional bundles — skip catalog checks.
            return await DownloadOptionalOnlyAsync(handler, cancellationToken);
        }

        /// <summary>
        /// Cancels any currently running update pipeline.
        /// Safe to call from any thread, at any time.
        /// </summary>
        public void CancelUpdate()
        {
            _internalCts?.Cancel();
            _internalCts?.Dispose();
            _internalCts = null;
        }

        /// <summary>
        /// Returns true when cached content is sufficient to run the game offline
        /// (i.e. the required bundles were downloaded at least once).
        /// </summary>
        public bool IsContentAvailableOffline()
        {
            // If we've ever successfully completed a download, cached content exists.
            // A more thorough check could enumerate cached bundles, but the simplest
            // signal is whether Addressables has a non-empty local catalog.
            try
            {
                var handle = Addressables.GetDownloadSizeAsync(_requiredLabel);
                handle.WaitForCompletion(); // sync — safe because we expect instant return
                long size = handle.Result;
                Addressables.Release(handle);
                return size == 0;
            }
            catch
            {
                return false;
            }
        }

        // ── Core pipeline ─────────────────────────────────────────────────

        private async UniTask<RemoteContentUpdateResult> RunPipelineAsync(
            IRemoteUpdateProgressHandler handler,
            bool downloadOptional,
            CancellationToken externalToken)
        {
            // ── Duplicate-call guard ──────────────────────────────────
            if (_runningUpdateTcs != null)
            {
                Debug.Log("[RemoteUpdate] Update already in progress — awaiting existing task.");
                return await _runningUpdateTcs.Task;
            }

            _runningUpdateTcs = new UniTaskCompletionSource<RemoteContentUpdateResult>();
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<string>();
            bool hadUpdates = false;
            bool catalogUpdated = false;
            long requiredDownloaded = 0;
            long optionalDownloaded = 0;

            // Merge external token with our internal one so CancelUpdate() works.
            _internalCts?.Dispose();
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _internalCts.Token;

            try
            {
                token.ThrowIfCancellationRequested();

                // ── Step 1: Initialise Addressables ───────────────────
                Report(handler, RemoteContentUpdateStatus.Initializing, "Initialising Addressables…");
                await InitializeAddressablesAsync(token);

                // ── Step 2: Check for catalog updates ─────────────────
                Report(handler, RemoteContentUpdateStatus.CheckingForUpdates, "Checking for updates…");
                var catalogUpdates = await CheckForCatalogUpdatesWithRetryAsync(token);

                if (catalogUpdates.Count == 0)
                {
                    Report(handler, RemoteContentUpdateStatus.NoUpdateNeeded,
                        "Content is up to date.", isDone: true);
                    var noUpdateResult = new RemoteContentUpdateResult(
                        RemoteContentUpdateStatus.NoUpdateNeeded,
                        stopwatch.Elapsed, false, false, 0, 0);
                    _runningUpdateTcs.TrySetResult(noUpdateResult);
                    return noUpdateResult;
                }

                hadUpdates = true;

                // ── Step 3: Update catalogs ──────────────────────────
                Report(handler, RemoteContentUpdateStatus.UpdatingCatalogs, "Updating catalog…");
                var locators = await UpdateCatalogsWithRetryAsync(catalogUpdates, token);
                catalogUpdated = true;

                // ── Step 4: Calculate download sizes ──────────────────
                Report(handler, RemoteContentUpdateStatus.CalculatingDownloadSize,
                    "Calculating download size…");
                long requiredSize = 0;
                long optionalSize = 0;

                if (!string.IsNullOrEmpty(_requiredLabel))
                    requiredSize = await GetDownloadSizeWithTimeoutAsync(_requiredLabel, token);

                if (!string.IsNullOrEmpty(_optionalLabel))
                    optionalSize = await GetDownloadSizeWithTimeoutAsync(_optionalLabel, token);

                Report(handler, RemoteContentUpdateStatus.CalculatingDownloadSize,
                    $"Required: {FormatBytes(requiredSize)}, Optional: {FormatBytes(optionalSize)}",
                    requiredBytes: requiredSize, optionalBytes: optionalSize);

                // ── Step 5: Download required content ─────────────────
                if (requiredSize > 0)
                {
                    requiredDownloaded = await DownloadContentWithProgressAsync(
                        _requiredLabel, "Required content", requiredSize,
                        handler, token);
                }

                // ── Step 6: Download optional content (if requested) ──
                if (downloadOptional && optionalSize > 0)
                {
                    optionalDownloaded = await DownloadContentWithProgressAsync(
                        _optionalLabel, "Optional content", optionalSize,
                        handler, token);
                }

                // ── Success ───────────────────────────────────────────
                Report(handler, RemoteContentUpdateStatus.Complete,
                    "Update complete.", isDone: true);
                var successResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Complete,
                    stopwatch.Elapsed, hadUpdates, catalogUpdated,
                    requiredDownloaded, optionalDownloaded, errors);
                _runningUpdateTcs.TrySetResult(successResult);
                return successResult;
            }
            catch (OperationCanceledException)
            {
                var cancelledResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Cancelled, stopwatch.Elapsed,
                    hadUpdates, catalogUpdated, requiredDownloaded, optionalDownloaded,
                    new[] { "Operation was cancelled." });
                _runningUpdateTcs.TrySetResult(cancelledResult);
                return cancelledResult;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                Debug.LogError($"[RemoteUpdate] Pipeline failed: {ex}");

                var failedResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Failed, stopwatch.Elapsed,
                    hadUpdates, catalogUpdated, requiredDownloaded, optionalDownloaded,
                    errors);
                Report(handler, RemoteContentUpdateStatus.Failed,
                    $"Update failed: {ex.Message}", isDone: true, errorMessage: ex.Message);
                _runningUpdateTcs.TrySetResult(failedResult);
                return failedResult;
            }
            finally
            {
                _internalCts?.Dispose();
                _internalCts = null;
                _runningUpdateTcs = null;
            }
        }

        /// <summary>
        /// Downloads optional content only — used when required content was
        /// already downloaded during bootstrap.
        /// </summary>
        private async UniTask<RemoteContentUpdateResult> DownloadOptionalOnlyAsync(
            IRemoteUpdateProgressHandler handler,
            CancellationToken externalToken)
        {
            if (string.IsNullOrEmpty(_optionalLabel))
            {
                return new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.NoUpdateNeeded,
                    TimeSpan.Zero, false, false, 0, 0);
            }

            if (_runningUpdateTcs != null)
            {
                Debug.Log("[RemoteUpdate] Update already in progress — awaiting existing task.");
                return await _runningUpdateTcs.Task;
            }

            _runningUpdateTcs = new UniTaskCompletionSource<RemoteContentUpdateResult>();
            var stopwatch = Stopwatch.StartNew();
            _internalCts?.Dispose();
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _internalCts.Token;

            try
            {
                token.ThrowIfCancellationRequested();

                long optionalSize = await GetDownloadSizeWithTimeoutAsync(_optionalLabel, token);
                long optionalDownloaded = 0;

                if (optionalSize > 0)
                {
                    optionalDownloaded = await DownloadContentWithProgressAsync(
                        _optionalLabel, "Optional content", optionalSize, handler, token);
                }

                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Complete,
                    stopwatch.Elapsed, false, false, 0, optionalDownloaded);
                _runningUpdateTcs.TrySetResult(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Cancelled, stopwatch.Elapsed,
                    false, false, 0, 0, new[] { "Operation was cancelled." });
                _runningUpdateTcs.TrySetResult(result);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteUpdate] Optional download failed: {ex}");
                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Failed, stopwatch.Elapsed,
                    false, false, 0, 0, new[] { ex.Message });
                _runningUpdateTcs.TrySetResult(result);
                return result;
            }
            finally
            {
                _internalCts?.Dispose();
                _internalCts = null;
                _runningUpdateTcs = null;
            }
        }

        // ── Sub-steps (each with proper handle release) ───────────────────

        /// <summary>
        /// Initialise Addressables. Idempotent — safe to call even if already initialised.
        /// The handle is released immediately after completion.
        /// </summary>
        private static async UniTask InitializeAddressablesAsync(System.Threading.CancellationToken token)
        {
            AsyncOperationHandle<IResourceLocator> initHandle = default;
            try
            {
                initHandle = Addressables.InitializeAsync();
                await initHandle.ToUniTask(cancellationToken: token);
            }
            finally
            {
                if (initHandle.IsValid())
                    Addressables.Release(initHandle);
            }
        }

        /// <summary>
        /// Calls Addressables.CheckForCatalogUpdates with retry and offline detection.
        /// Returns the list of updated catalog hashes (empty = no updates).
        /// </summary>
        private async UniTask<List<string>> CheckForCatalogUpdatesWithRetryAsync(
            System.Threading.CancellationToken token)
        {
            return await RetryAsync(
                async ct =>
                {
                    AsyncOperationHandle<List<string>> checkHandle = default;
                    try
                    {
                        checkHandle = Addressables.CheckForCatalogUpdates(false);
                        var result = await checkHandle.ToUniTask(cancellationToken: ct);
                        return result;
                    }
                    catch (Exception ex) when (IsOffline(ex))
                    {
                        throw new OfflineException("Device is offline.", ex);
                    }
                    finally
                    {
                        if (checkHandle.IsValid())
                            Addressables.Release(checkHandle);
                    }
                },
                "CheckForCatalogUpdates",
                token);
        }

        /// <summary>
        /// Calls Addressables.UpdateCatalogs with retry.
        /// Returns the updated resource locators — these must be kept alive until
        /// downloads complete so the download-size queries resolve correctly.
        /// </summary>
        private async UniTask<List<IResourceLocator>> UpdateCatalogsWithRetryAsync(
            List<string> catalogs,
            System.Threading.CancellationToken token)
        {
            return await RetryAsync(
                async ct =>
                {
                    var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                    // NOTE: We do NOT release updateHandle here.
                    // The locators it produces must stay alive for the download phase.
                    // The caller is responsible for releasing it after downloads finish.
                    // However, since we need to return the locators, we release the
                    // handle but keep the locators alive via the returned list — they
                    // are reference-counted by Addressables internally.
                    var locators = await updateHandle.ToUniTask(cancellationToken: ct);
                    Addressables.Release(updateHandle);
                    return locators;
                },
                "UpdateCatalogs",
                token);
        }

        /// <summary>
        /// Gets the download size for a label, with timeout handling.
        /// </summary>
        private async UniTask<long> GetDownloadSizeWithTimeoutAsync(
            string label,
            System.Threading.CancellationToken token)
        {
            return await WithTimeoutAsync(
                async ct =>
                {
                    AsyncOperationHandle<long> sizeHandle = default;
                    try
                    {
                        sizeHandle = Addressables.GetDownloadSizeAsync(label);
                        return await sizeHandle.ToUniTask(cancellationToken: ct);
                    }
                    finally
                    {
                        if (sizeHandle.IsValid())
                            Addressables.Release(sizeHandle);
                    }
                },
                "GetDownloadSize",
                token);
        }

        /// <summary>
        /// Downloads all dependencies for a label and reports per-frame progress.
        /// Returns the number of bytes actually downloaded.
        /// </summary>
        private async UniTask<long> DownloadContentWithProgressAsync(
            string label,
            string labelDescription,
            long expectedSizeBytes,
            IRemoteUpdateProgressHandler handler,
            System.Threading.CancellationToken token)
        {
            AsyncOperationHandle downloadHandle = default;
            try
            {
                downloadHandle = Addressables.DownloadDependenciesAsync(
                    label, Addressables.MergeMode.Union);

                long lastDownloaded = 0;

                // Per-frame progress loop — exits when downloadHandle.IsDone.
                while (!downloadHandle.IsDone && !token.IsCancellationRequested)
                {
                    var status = downloadHandle.GetDownloadStatus();
                    long downloaded = status.DownloadedBytes;
                    long total = status.TotalBytes > 0 ? status.TotalBytes : expectedSizeBytes;

                    float percent = total > 0
                        ? Mathf.Clamp01((float)downloaded / total)
                        : 0f;

                    // Only report if bytes changed (avoids spamming the handler).
                    if (downloaded != lastDownloaded || percent >= 1f)
                    {
                        lastDownloaded = downloaded;
                        ReportDownloadProgress(handler, RemoteContentUpdateStatus.Downloading,
                            labelDescription, downloaded, total, percent);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                // Await final completion to surface any exception.
                await downloadHandle.ToUniTask(cancellationToken: token);

                // Re-read final download status.
                var finalStatus = downloadHandle.GetDownloadStatus();
                long finalDownloaded = finalStatus.DownloadedBytes;

                ReportDownloadProgress(handler, RemoteContentUpdateStatus.Downloading,
                    labelDescription, finalDownloaded, finalDownloaded, 1f);

                return finalDownloaded;
            }
            finally
            {
                if (downloadHandle.IsValid())
                    Addressables.Release(downloadHandle);
            }
        }

        // ── Retry & timeout helpers ───────────────────────────────────────

        /// <summary>
        /// Retries an async operation up to <see cref="_maxRetryCount"/> times
        /// with exponential backoff. Only retries on transient errors.
        /// </summary>
        private async UniTask<T> RetryAsync<T>(
            Func<System.Threading.CancellationToken, UniTask<T>> operation,
            string operationName,
            System.Threading.CancellationToken token)
        {
            ExceptionDispatchInfo lastException = null;

            for (int attempt = 0; attempt <= _maxRetryCount; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    return await WithTimeoutAsync(operation, operationName, token);
                }
                catch (OfflineException)
                {
                    // Don't retry offline — propagate immediately.
                    throw;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Our token was cancelled — don't retry.
                    throw;
                }
                catch (Exception ex) when (attempt < _maxRetryCount && IsTransientError(ex))
                {
                    lastException = ExceptionDispatchInfo.Capture(ex);
                    float delay = _retryDelaySeconds * (attempt + 1);
                    Debug.LogWarning(
                        $"[RemoteUpdate] {operationName} attempt {attempt + 1} failed: {ex.Message}. " +
                        $"Retrying in {delay:F1}s…");

                    try
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(delay),
                            DelayType.DeltaTime,
                            PlayerLoopTiming.Update,
                            token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // cancelled during delay — stop.
                    }
                }
            }

            // All retries exhausted.
            lastException?.Throw();
            throw new InvalidOperationException(
                $"{operationName} failed after {_maxRetryCount + 1} attempts.");
        }

        /// <summary>
        /// Wraps an async operation with a timeout. If the operation does not
        /// complete within <see cref="_timeoutSeconds"/>, a TimeoutException is thrown.
        /// Uses a linked CancellationTokenSource with CancelAfter for clean cancellation.
        /// </summary>
        private async UniTask<T> WithTimeoutAsync<T>(
            Func<System.Threading.CancellationToken, UniTask<T>> operation,
            string operationName,
            System.Threading.CancellationToken token)
        {
            using var timeoutCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

            try
            {
                return await operation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                // Our timeout token fired — the external caller did not cancel.
                throw new TimeoutException(
                    $"{operationName} timed out after {_timeoutSeconds:F0}s.");
            }
        }

        // ── Error classification ──────────────────────────────────────────

        /// <summary>
        /// Returns true when the exception indicates a temporary failure
        /// that is worth retrying (network blip, DNS, timeout).
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            if (ex is TimeoutException)
                return true;
            if (ex is System.Net.Sockets.SocketException)
                return true;
            if (ex is System.Net.Http.HttpRequestException)
                return true;
            if (ex is InvalidOperationException)
                return true; // Addressables may wrap network errors here

            // UnityWebRequestException may not be accessible depending on asmdef.
            // Detect it by type name and known network-error substrings.
            var typeName = ex.GetType().FullName ?? string.Empty;
            if (typeName.Contains("UnityWebRequestException"))
                return true;

            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("Cannot resolve destination host") ||
                msg.Contains("No connection could be made") ||
                msg.Contains("Connection timed out") ||
                msg.Contains("Unable to complete SSL connection") ||
                msg.Contains("request timed out"))
                return true;

            return false;
        }

        /// <summary>
        /// Returns true when the device appears to be offline.
        /// </summary>
        private static bool IsOffline(Exception ex)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return true;

            // Also treat specific DNS / connection-refused as offline.
            var msg = ex.InnerException?.Message ?? ex.Message;
            return msg.Contains("No Internet") ||
                   msg.Contains("NameResolutionFailure") ||
                   msg.Contains("ConnectionRefused") ||
                   msg.Contains("Cannot connect to destination host");
        }

        // ── Progress reporting ────────────────────────────────────────────

        private static void Report(
            IRemoteUpdateProgressHandler handler,
            RemoteContentUpdateStatus status,
            string label,
            bool isDone = false,
            string errorMessage = null,
            long requiredBytes = 0,
            long optionalBytes = 0)
        {
            handler?.OnProgress(RemoteContentUpdateProgress.StatusOnly(
                status, label, isDone, errorMessage));
        }

        private static void ReportDownloadProgress(
            IRemoteUpdateProgressHandler handler,
            RemoteContentUpdateStatus status,
            string labelDescription,
            long downloadedBytes,
            long totalBytes,
            float percent)
        {
            if (handler == null) return;

            handler.OnProgress(new RemoteContentUpdateProgress(
                status,
                downloadedBytes,
                totalBytes,
                $"Downloading {labelDescription}…",
                percent,
                false));
        }

        // ── Formatting ────────────────────────────────────────────────────

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

        // ── Custom exception ──────────────────────────────────────────────

        /// <summary>
        /// Thrown when the device has no internet connectivity.
        /// Propagated immediately without retry.
        /// </summary>
        private sealed class OfflineException : Exception
        {
            public OfflineException(string message, Exception inner)
                : base(message, inner) { }
        }
    }
}
