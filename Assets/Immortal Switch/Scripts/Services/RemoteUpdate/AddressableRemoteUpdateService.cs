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
    /// Owned exclusively by <see cref="GameBootstrap"/>. Do not call this service
    /// from other components during the bootstrap phase.
    ///
    /// Usage (in GameBootstrap):
    /// <code>
    /// var result = await AddressableRemoteUpdateService.Instance
    ///     .DownloadRequiredContentAsync(handler, cancellationToken);
    /// if (result.IsSuccess) { /* proceed into game */ }
    /// else if (result.Status == RemoteContentUpdateStatus.Offline
    ///          &amp;&amp; await service.IsContentAvailableOfflineAsync())
    ///     { /* proceed offline */ }
    /// </code>
    /// </summary>
    public sealed class AddressableRemoteUpdateService : Singleton<AddressableRemoteUpdateService>
    {
        // ── Serialized configuration ──────────────────────────────────────

        [SerializeField, Tooltip("Addressable label for required bundles (must finish before entering game).")]
        private string _requiredLabel = "Required";

        [SerializeField, Tooltip("Addressable label for optional bundles (may be downloaded later).")]
        private string _optionalLabel = "Optional";

        [SerializeField, Tooltip("Seconds before a single network operation is considered timed out.")]
        private float _timeoutSeconds = 30f;

        [SerializeField, Tooltip("Maximum retry attempts per transient failure.")]
        private int _maxRetryCount = 3;

        [SerializeField, Tooltip("Base delay in seconds between retries (multiplied by attempt number).")]
        private float _retryDelaySeconds = 2f;

        [SerializeField, Tooltip("Seconds without any byte progress before the download is considered stalled.")]
        private float _downloadStallTimeoutSeconds = 60f;

        // ── Content-ready marker ──────────────────────────────────────────

        private const string ContentReadyMarkerPrefix = "addr_content_ready_";

        private string ContentReadyMarkerKey =>
            ContentReadyMarkerPrefix + (_requiredLabel ?? "Required");

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
            return await DownloadOptionalOnlyAsync(handler, cancellationToken);
        }

        /// <summary>
        /// True while a pipeline is in flight. External code can check this
        /// to avoid starting a duplicate update.
        /// </summary>
        public bool IsRunning => _runningUpdateTcs != null;

        /// <summary>
        /// Cancels any currently running update pipeline.
        /// Safe to call from any thread, at any time.
        /// Does NOT dispose the internal CTS — the pipeline's finally block handles that.
        /// </summary>
        public void CancelUpdate()
        {
            _internalCts?.Cancel();
        }

        /// <summary>
        /// Returns true when cached content is sufficient to run the game offline
        /// (i.e. the required bundles were downloaded at least once AND the
        /// content-ready marker exists).
        /// </summary>
        public async UniTask<bool> IsContentAvailableOfflineAsync(
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_requiredLabel))
                return false;

            if (!PlayerPrefs.HasKey(ContentReadyMarkerKey))
                return false;

            AsyncOperationHandle<IList<IResourceLocation>>
                locationsHandle = default;

            AsyncOperationHandle<long> sizeHandle = default;

            try
            {
                locationsHandle =
                    Addressables.LoadResourceLocationsAsync(
                        (object)_requiredLabel,
                        null
                    );

                IList<IResourceLocation> locations =
                    await locationsHandle.ToUniTask(
                        cancellationToken: cancellationToken
                    );

                if (locations == null || locations.Count == 0)
                    return false;

                sizeHandle =
                    Addressables.GetDownloadSizeAsync(locations);

                long size = await sizeHandle.ToUniTask(
                    cancellationToken: cancellationToken
                );

                return size == 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[RemoteUpdate] Offline cache validation failed: {ex}"
                );

                return false;
            }
            finally
            {
                if (sizeHandle.IsValid())
                    Addressables.Release(sizeHandle);

                if (locationsHandle.IsValid())
                    Addressables.Release(locationsHandle);
            }
        }

        /// <summary>
        /// Synchronous convenience wrapper. Prefer <see cref="IsContentAvailableOfflineAsync"/>
        /// to avoid blocking the main thread.
        /// </summary>
        [Obsolete(
            "Use IsContentAvailableOfflineAsync instead. " +
            "This synchronous wrapper may block the main thread.")]
        public bool IsContentAvailableOffline()
        {
            if (string.IsNullOrWhiteSpace(_requiredLabel))
                return false;

            if (!PlayerPrefs.HasKey(ContentReadyMarkerKey))
                return false;

            AsyncOperationHandle<IList<IResourceLocation>>
                locationsHandle = default;

            AsyncOperationHandle<long> sizeHandle = default;

            try
            {
                locationsHandle =
                    Addressables.LoadResourceLocationsAsync(
                        (object)_requiredLabel,
                        null
                    );

                IList<IResourceLocation> locations =
                    locationsHandle.WaitForCompletion();

                if (locations == null || locations.Count == 0)
                    return false;

                sizeHandle =
                    Addressables.GetDownloadSizeAsync(locations);

                long size = sizeHandle.WaitForCompletion();
                return size == 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[RemoteUpdate] Synchronous cache validation failed: {ex}");
                return false;
            }
            finally
            {
                if (sizeHandle.IsValid())
                    Addressables.Release(sizeHandle);

                if (locationsHandle.IsValid())
                    Addressables.Release(locationsHandle);
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
                Debug.Log(
                    "[RemoteUpdate] Update already in progress — awaiting existing task.");

                return await _runningUpdateTcs.Task;
            }

            _runningUpdateTcs =
                new UniTaskCompletionSource<RemoteContentUpdateResult>();
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

                // ── Phase 1: Initialise Addressables ──────────────────
                // Weighted progress: 0.00 – 0.10
                ReportPhase(handler, RemoteContentUpdateStatus.Initializing,
                    "Initialising Addressables…", 0.00f);
                await InitializeAddressablesAsync(token);
                ReportPhase(handler, RemoteContentUpdateStatus.Initializing,
                    "Addressables ready.", 0.10f);

                // ── Phase 2: Check for catalog updates ────────────────
                // Weighted progress: 0.10 – 0.25
                ReportPhase(handler, RemoteContentUpdateStatus.CheckingForUpdates,
                    "Checking for updates…", 0.10f);
                var catalogUpdates = await CheckForCatalogUpdatesWithRetryAsync(token);
                ReportPhase(handler, RemoteContentUpdateStatus.CheckingForUpdates,
                    catalogUpdates.Count > 0
                        ? $"Found {catalogUpdates.Count} catalog update(s)."
                        : "Catalog is current.",
                    0.25f);

                // ── Phase 3: Update catalogs (only if needed) ────────
                // Weighted progress: 0.25 – 0.40
                if (catalogUpdates.Count > 0)
                {
                    hadUpdates = true;
                    ReportPhase(handler, RemoteContentUpdateStatus.UpdatingCatalogs,
                        "Updating catalog…", 0.25f);
                    await UpdateCatalogsWithRetryAsync(catalogUpdates, token);
                    catalogUpdated = true;
                }

                ReportPhase(handler, RemoteContentUpdateStatus.UpdatingCatalogs,
                    catalogUpdated ? "Catalog updated." : "Catalog is up to date.",
                    0.40f);

                // ── Phase 4: Calculate download sizes ────────────────
                // Weighted progress: 0.40 – 0.50
                ReportPhase(handler, RemoteContentUpdateStatus.CalculatingDownloadSize,
                    "Calculating download size…", 0.40f);

                long requiredSize = 0;
                long optionalSize = 0;

                if (!string.IsNullOrEmpty(_requiredLabel))
                    requiredSize = await GetDownloadSizeWithTimeoutAsync(_requiredLabel, token);

                if (downloadOptional && !string.IsNullOrEmpty(_optionalLabel))
                    optionalSize = await GetDownloadSizeWithTimeoutAsync(_optionalLabel, token);

                ReportPhase(handler, RemoteContentUpdateStatus.CalculatingDownloadSize,
                    $"Required: {FormatBytes(requiredSize)}, Optional: {FormatBytes(optionalSize)}",
                    0.50f);

                // ── Phase 5: Download required content ───────────────
                // Split the remaining 50% according to actual Required/Optional sizes
                // so overall progress never moves backward.
                long totalDownloadSize = requiredSize + optionalSize;
                float requiredPhaseEnd = 1.00f;

                if (downloadOptional && optionalSize > 0 && totalDownloadSize > 0)
                {
                    float requiredWeight =
                        Mathf.Clamp01(requiredSize / (float)totalDownloadSize);

                    requiredPhaseEnd = Mathf.Lerp(
                        0.50f,
                        1.00f,
                        requiredWeight);
                }

                if (requiredSize > 0)
                {
                    requiredDownloaded = await DownloadContentWithProgressAsync(
                        _requiredLabel,
                        "Required content",
                        requiredSize,
                        handler,
                        token,
                        0.50f,
                        requiredPhaseEnd);
                }
                else
                {
                    ReportPhase(
                        handler,
                        RemoteContentUpdateStatus.Downloading,
                        "Required content is already cached.",
                        requiredPhaseEnd);
                }

                // Required content is now either downloaded or already cached.
                // Write the marker before Optional content, because Optional failure
                // must not invalidate an otherwise usable Required cache.
                MarkRequiredContentReady();

                // ── Phase 6: Download optional content (if requested) ─
                if (downloadOptional && optionalSize > 0)
                {
                    optionalDownloaded = await DownloadContentWithProgressAsync(
                        _optionalLabel,
                        "Optional content",
                        optionalSize,
                        handler,
                        token,
                        requiredPhaseEnd,
                        1.00f);
                }

                // ── Success ───────────────────────────────────────────
                ReportPhase(handler, RemoteContentUpdateStatus.Complete,
                    "Update complete.", 1.00f, isDone: true);
                var successResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Complete,
                    stopwatch.Elapsed, hadUpdates, catalogUpdated,
                    requiredDownloaded, optionalDownloaded, errors);
                return Finish(successResult, handler);
            }
            catch (OfflineException ex)
            {
                Debug.LogWarning($"[RemoteUpdate] Device is offline: {ex.Message}");
                var offlineResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Offline, stopwatch.Elapsed,
                    hadUpdates, catalogUpdated, requiredDownloaded, optionalDownloaded,
                    new[] { ex.Message });
                return Finish(offlineResult, handler);
            }
            catch (TimeoutException ex)
            {
                Debug.LogWarning($"[RemoteUpdate] Operation timed out: {ex.Message}");
                errors.Add(ex.Message);
                var timeoutResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Timeout, stopwatch.Elapsed,
                    hadUpdates, catalogUpdated, requiredDownloaded, optionalDownloaded,
                    errors);
                ReportPhase(handler, RemoteContentUpdateStatus.Timeout,
                    $"Update timed out: {ex.Message}", 1.00f, isDone: true, errorMessage: ex.Message);
                return Finish(timeoutResult, handler);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[RemoteUpdate] Pipeline cancelled.");
                var cancelledResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Cancelled, stopwatch.Elapsed,
                    hadUpdates, catalogUpdated, requiredDownloaded, optionalDownloaded,
                    new[] { "Operation was cancelled." });
                return Finish(cancelledResult, handler);
            }
            catch (Exception ex) when (IsOffline(ex))
            {
                Debug.LogWarning(
                    $"[RemoteUpdate] Device went offline during update: {ex.Message}");

                var offlineResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Offline,
                    stopwatch.Elapsed,
                    hadUpdates,
                    catalogUpdated,
                    requiredDownloaded,
                    optionalDownloaded,
                    new[] { ex.Message });

                ReportPhase(
                    handler,
                    RemoteContentUpdateStatus.Offline,
                    "No internet connection.",
                    1.00f,
                    isDone: true,
                    errorMessage: ex.Message);

                return Finish(offlineResult, handler);
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                Debug.LogError($"[RemoteUpdate] Pipeline failed: {ex}");

                var failedResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Failed, stopwatch.Elapsed,
                    hadUpdates, catalogUpdated, requiredDownloaded, optionalDownloaded,
                    errors);
                ReportPhase(handler, RemoteContentUpdateStatus.Failed,
                    $"Update failed: {ex.Message}", 1.00f, isDone: true, errorMessage: ex.Message);
                return Finish(failedResult, handler);
            }
            finally
            {
                _internalCts?.Dispose();
                _internalCts = null;
                _runningUpdateTcs = null;
            }
        }

        /// <summary>
        /// Downloads optional content only.
        /// Initialises Addressables first since it may not have been done
        /// if the caller skipped the required pipeline.
        /// </summary>
        private async UniTask<RemoteContentUpdateResult> DownloadOptionalOnlyAsync(
            IRemoteUpdateProgressHandler handler,
            CancellationToken externalToken)
        {
            // Optional and Required pipelines must never share the same running task.
            // If another pipeline is active, fail fast instead of returning the
            // result of an unrelated operation.
            if (_runningUpdateTcs != null)
            {
                var busyResult = RemoteContentUpdateResult.Failed(
                    TimeSpan.Zero,
                    "Another Addressables update pipeline is already running.");

                try
                {
                    handler?.OnComplete(busyResult);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return busyResult;
            }

            if (!await IsContentAvailableOfflineAsync(externalToken))
            {
                var requiredNotReadyResult = RemoteContentUpdateResult.Failed(
                    TimeSpan.Zero,
                    "Required content is not ready. Run the Required pipeline first.");

                // No update TCS exists yet in this early-return path.
                try
                {
                    handler?.OnComplete(requiredNotReadyResult);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return requiredNotReadyResult;
            }

            if (string.IsNullOrEmpty(_optionalLabel))
            {
                var emptyResult = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.NoUpdateNeeded,
                    TimeSpan.Zero, false, false, 0, 0);
                return Finish(emptyResult, handler);
            }

            _runningUpdateTcs = new UniTaskCompletionSource<RemoteContentUpdateResult>();
            var stopwatch = Stopwatch.StartNew();
            _internalCts?.Dispose();
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _internalCts.Token;

            try
            {
                token.ThrowIfCancellationRequested();

                // Ensure Addressables is initialised (may not have been done if
                // the required pipeline was skipped).
                ReportPhase(handler, RemoteContentUpdateStatus.Initializing,
                    "Initialising Addressables…", 0.00f);
                await InitializeAddressablesAsync(token);
                ReportPhase(handler, RemoteContentUpdateStatus.Initializing,
                    "Addressables ready.", 0.10f);

                long optionalSize = await GetDownloadSizeWithTimeoutAsync(_optionalLabel, token);
                long optionalDownloaded = 0;

                if (optionalSize > 0)
                {
                    optionalDownloaded = await DownloadContentWithProgressAsync(
                        _optionalLabel, "Optional content", optionalSize,
                        handler, token, 0.10f, 1.00f);
                }

                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Complete,
                    stopwatch.Elapsed, false, false, 0, optionalDownloaded);
                return Finish(result, handler);
            }
            catch (OperationCanceledException)
            {
                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Cancelled, stopwatch.Elapsed,
                    false, false, 0, 0, new[] { "Operation was cancelled." });
                return Finish(result, handler);
            }
            catch (TimeoutException ex)
            {
                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Timeout,
                    stopwatch.Elapsed,
                    false,
                    false,
                    0,
                    0,
                    new[] { ex.Message });

                ReportPhase(
                    handler,
                    RemoteContentUpdateStatus.Timeout,
                    $"Optional download timed out: {ex.Message}",
                    1.00f,
                    isDone: true,
                    errorMessage: ex.Message);

                return Finish(result, handler);
            }
            catch (Exception ex) when (IsOffline(ex))
            {
                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Offline,
                    stopwatch.Elapsed,
                    false,
                    false,
                    0,
                    0,
                    new[] { ex.Message });

                ReportPhase(
                    handler,
                    RemoteContentUpdateStatus.Offline,
                    "No internet connection.",
                    1.00f,
                    isDone: true,
                    errorMessage: ex.Message);

                return Finish(result, handler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteUpdate] Optional download failed: {ex}");
                var result = new RemoteContentUpdateResult(
                    RemoteContentUpdateStatus.Failed, stopwatch.Elapsed,
                    false, false, 0, 0, new[] { ex.Message });
                return Finish(result, handler);
            }
            finally
            {
                _internalCts?.Dispose();
                _internalCts = null;
                _runningUpdateTcs = null;
            }
        }

        // ── Pipeline finalisation ─────────────────────────────────────────

        /// <summary>
        /// Called exactly once at every terminal path.
        /// Sets the TCS result and notifies the progress handler.
        /// </summary>
        private RemoteContentUpdateResult Finish(
            RemoteContentUpdateResult result,
            IRemoteUpdateProgressHandler handler)
        {
            try
            {
                handler?.OnComplete(result);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _runningUpdateTcs?.TrySetResult(result);
            return result;
        }

        // ── Sub-steps (each with proper handle release) ───────────────────

        /// <summary>
        /// Initialise Addressables. Idempotent — safe to call even if already initialised.
        /// The handle is released immediately after completion.
        /// </summary>
        private static async UniTask InitializeAddressablesAsync(CancellationToken token)
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
            CancellationToken token)
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
        /// Returns the updated resource locators.
        /// The handle is released after locators are extracted — Addressables
        /// reference-counts the locators internally so they stay valid.
        /// </summary>
        private async UniTask<List<IResourceLocator>> UpdateCatalogsWithRetryAsync(
            List<string> catalogs,
            CancellationToken token)
        {
            return await RetryAsync(
                async ct =>
                {
                    var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                    try
                    {
                        var locators = await updateHandle.ToUniTask(cancellationToken: ct);
                        return locators;
                    }
                    finally
                    {
                        // Release the handle — locators are reference-counted
                        // by Addressables and remain valid for subsequent queries.
                        if (updateHandle.IsValid())
                            Addressables.Release(updateHandle);
                    }
                },
                "UpdateCatalogs",
                token);
        }

        /// <summary>
        /// Gets the download size for a label, with timeout handling.
        /// </summary>
        private async UniTask<long> GetDownloadSizeWithTimeoutAsync(
            string label,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(label))
                return 0;

            return await WithTimeoutAsync(
                async ct =>
                {
                    AsyncOperationHandle<IList<IResourceLocation>>
                        locationsHandle = default;

                    AsyncOperationHandle<long> sizeHandle = default;

                    try
                    {
                        locationsHandle =
                            Addressables.LoadResourceLocationsAsync(
                                (object)label,
                                null
                            );

                        IList<IResourceLocation> locations =
                            await locationsHandle.ToUniTask(
                                cancellationToken: ct
                            );

                        if (locations == null || locations.Count == 0)
                        {
                            throw new InvalidOperationException(
                                $"No Addressable locations found for label '{label}'. " +
                                "Make sure the label exists and is assigned to the required assets."
                            );
                        }

                        sizeHandle =
                            Addressables.GetDownloadSizeAsync(locations);

                        return await sizeHandle.ToUniTask(
                            cancellationToken: ct
                        );
                    }
                    finally
                    {
                        if (sizeHandle.IsValid())
                            Addressables.Release(sizeHandle);

                        if (locationsHandle.IsValid())
                            Addressables.Release(locationsHandle);
                    }
                },
                $"GetDownloadSize:{label}",
                token
            );
        }

        /// <summary>
        /// Downloads all dependencies for a label and reports per-frame progress.
        /// Maps download sub-progress into the range [phaseStart, phaseEnd] of the
        /// overall pipeline progress bar.
        /// Detects download stalls (no bytes for <see cref="_downloadStallTimeoutSeconds"/>).
        /// Returns the number of bytes actually downloaded.
        /// </summary>
        private async UniTask<long> DownloadContentWithProgressAsync(
            string label,
            string labelDescription,
            long expectedSizeBytes,
            IRemoteUpdateProgressHandler handler,
            CancellationToken token,
            float phaseProgressStart,
            float phaseProgressEnd)
        {
            AsyncOperationHandle<IList<IResourceLocation>>
                locationsHandle = default;

            AsyncOperationHandle downloadHandle = default;

            try
            {
                locationsHandle =
                    Addressables.LoadResourceLocationsAsync(
                        (object)label,
                        null
                    );

                IList<IResourceLocation> locations =
                    await locationsHandle.ToUniTask(
                        cancellationToken: token
                    );

                if (locations == null || locations.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No Addressable locations found for label '{label}'."
                    );
                }

                downloadHandle =
                    Addressables.DownloadDependenciesAsync(
                        locations,
                        false
                    );

                long lastDownloaded = -1;
                float lastStallCheckTime = Time.unscaledTime;

                while (!downloadHandle.IsDone)
                {
                    token.ThrowIfCancellationRequested();

                    var status = downloadHandle.GetDownloadStatus();

                    long downloaded = status.DownloadedBytes;

                    long total = status.TotalBytes > 0
                        ? status.TotalBytes
                        : expectedSizeBytes;

                    if (downloaded != lastDownloaded)
                    {
                        lastDownloaded = downloaded;
                        lastStallCheckTime = Time.unscaledTime;
                    }
                    else if (
                        Time.unscaledTime - lastStallCheckTime >
                        _downloadStallTimeoutSeconds)
                    {
                        throw new TimeoutException(
                            $"Download '{label}' stalled — no progress for " +
                            $"{_downloadStallTimeoutSeconds:F0}s."
                        );
                    }

                    float subPercent = total > 0
                        ? Mathf.Clamp01(
                            downloaded / (float)total
                        )
                        : 0f;

                    float overallPercent = Mathf.Lerp(
                        phaseProgressStart,
                        phaseProgressEnd,
                        subPercent
                    );

                    ReportPhase(
                        handler,
                        RemoteContentUpdateStatus.Downloading,
                        $"Downloading {labelDescription}… " +
                        $"{FormatBytes(downloaded)} / {FormatBytes(total)}",
                        overallPercent,
                        downloadedBytes: downloaded,
                        totalBytes: total
                    );

                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        token
                    );
                }

                await downloadHandle.ToUniTask(
                    cancellationToken: token
                );

                var finalStatus =
                    downloadHandle.GetDownloadStatus();

                long finalDownloaded =
                    finalStatus.DownloadedBytes;

                long finalTotal =
                    finalStatus.TotalBytes > 0
                        ? finalStatus.TotalBytes
                        : expectedSizeBytes;

                ReportPhase(
                    handler,
                    RemoteContentUpdateStatus.Downloading,
                    $"{labelDescription} complete.",
                    phaseProgressEnd,
                    downloadedBytes: finalDownloaded,
                    totalBytes: finalTotal
                );

                return finalDownloaded;
            }
            finally
            {
                if (downloadHandle.IsValid())
                    Addressables.Release(downloadHandle);

                if (locationsHandle.IsValid())
                    Addressables.Release(locationsHandle);
            }
        }

        // ── Retry & timeout helpers ───────────────────────────────────────

        /// <summary>
        /// Retries an async operation up to <see cref="_maxRetryCount"/> times
        /// with exponential backoff. Only retries on transient network errors.
        /// </summary>
        private async UniTask<T> RetryAsync<T>(
            Func<CancellationToken, UniTask<T>> operation,
            string operationName,
            CancellationToken token)
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
            Func<CancellationToken, UniTask<T>> operation,
            string operationName,
            CancellationToken token)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
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
        /// Returns true when the exception indicates a temporary network failure
        /// that is worth retrying. Does NOT retry data errors like invalid keys
        /// or corrupt catalogs.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            if (ex is TimeoutException)
                return true;
            if (ex is System.Net.Sockets.SocketException)
                return true;
            if (ex is System.Net.Http.HttpRequestException)
                return true;

            // UnityWebRequestException may not be directly accessible depending
            // on asmdef. Detect by type name.
            var typeName = ex.GetType().FullName ?? string.Empty;
            if (typeName.Contains("UnityWebRequestException"))
                return true;

            // Addressables RemoteProviderException wraps network errors.
            if (typeName.Contains("RemoteProviderException"))
                return true;

            // Inspect inner exceptions too.
            if (ex.InnerException != null && IsTransientError(ex.InnerException))
                return true;

            // Known transient error substrings in messages.
            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("Cannot resolve destination host") ||
                msg.Contains("No connection could be made") ||
                msg.Contains("Connection timed out") ||
                msg.Contains("Unable to complete SSL connection") ||
                msg.Contains("request timed out") ||
                msg.Contains("NameResolutionFailure"))
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
            var msg = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;
            return msg.Contains("No Internet") ||
                   msg.Contains("NameResolutionFailure") ||
                   msg.Contains("ConnectionRefused") ||
                   msg.Contains("Cannot connect to destination host");
        }

        // ── Progress reporting ────────────────────────────────────────────

        /// <summary>
        /// Reports phase-weighted progress. <paramref name="overallPercent"/>
        /// is the position within the entire pipeline (0.0 – 1.0).
        /// </summary>
        private static void ReportPhase(
            IRemoteUpdateProgressHandler handler,
            RemoteContentUpdateStatus status,
            string label,
            float overallPercent,
            bool isDone = false,
            string errorMessage = null,
            long downloadedBytes = 0,
            long totalBytes = 0,
            long requiredBytes = 0,
            long optionalBytes = 0)
        {
            if (handler == null) return;

            handler.OnProgress(new RemoteContentUpdateProgress(
                status,
                downloadedBytes,
                totalBytes,
                label,
                overallPercent,
                isDone,
                errorMessage,
                requiredBytes,
                optionalBytes));
        }

        // ── Required-content marker ──────────────────────────────────────

        private void MarkRequiredContentReady()
        {
            if (string.IsNullOrWhiteSpace(_requiredLabel))
            {
                throw new InvalidOperationException(
                    "Required Addressables label is empty.");
            }

            PlayerPrefs.SetString(
                ContentReadyMarkerKey,
                _requiredLabel);

            PlayerPrefs.Save();
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
                : base(message, inner)
            {
            }
        }
    }
}