## Addressables Remote Content Update System

### Architecture

```
┌──────────────────────────────────────────────────────────┐
│  Caller (GameBootstrap / LoginScene / MainMenu)          │
│  calls AddressableRemoteUpdateService.Instance            │
└───────────────────────┬──────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│  AddressableRemoteUpdateService (Singleton)               │
│                                                          │
│  Public API:                                              │
│   • DownloadRequiredContentAsync(handler, token)          │
│   • DownloadOptionalContentAsync(handler, token)          │
│   • CheckAndDownloadUpdatesAsync(handler, token)  (both)  │
│   • CancelUpdate()                                        │
│   • IsContentAvailableOffline()                           │
│                                                          │
│  Internal pipeline:                                       │
│   1. Guard (duplicate-call prevention)                    │
│   2. InitializeAddressables                                │
│   3. CheckForCatalogUpdates (with retry + offline detect)  │
│   4. UpdateCatalogs (if updates exist)                     │
│   5. GetDownloadSize (required + optional labels)          │
│   6. DownloadDependencies (required, with progress)        │
│   7. DownloadDependencies (optional, with progress)        │
│   8. Report result                                        │
│                                                          │
│  Cross-cutting: timeout, retry, cancellation, offline     │
└───────────────────────┬──────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│  IRemoteUpdateProgressHandler (interface)                 │
│   • OnProgress(RemoteContentUpdateProgress)               │
│   • OnComplete(RemoteContentUpdateResult)                 │
│                                                          │
│  Implementors:                                            │
│   • RemoteUpdateLoadingScreenView (UIView example)        │
│   • BootstrapProgressHandler (adapter for GameBootstrap)  │
└──────────────────────────────────────────────────────────┘
```

### Update Flow (Step by Step)

```
                    ┌──────────┐
                    │  Idle    │
                    └────┬─────┘
                         │
                    ┌────▼──────────┐
                    │ Initializing  │  Addressables.InitializeAsync()
                    └────┬──────────┘
                         │
                    ┌────▼──────────────┐
                    │ CheckingForUpdates │  Addressables.CheckForCatalogUpdates()
                    └────┬──────┬────────┘
                         │      │
              (empty list)│      │(has updates)
                         │      │
              ┌──────────▼─┐  ┌─▼───────────────┐
              │NoUpdateNeeded│  │ UpdatingCatalogs │  Addressables.UpdateCatalogs()
              └──────────────┘  └─┬───────────────┘
                                  │
                         ┌────────▼──────────────┐
                         │ CalculatingDownloadSize│  Addressables.GetDownloadSizeAsync()
                         └────────┬──────────────┘
                                  │
                         ┌────────▼────────┐
                         │  Downloading    │  Addressables.DownloadDependenciesAsync()
                         │  (per-frame     │  ── required label
                         │   progress)     │  ── optional label (if requested)
                         └────────┬────────┘
                                  │
                         ┌────────▼────────┐
                         │   Complete      │
                         └─────────────────┘

Error paths (any step):
  • Network offline → Offline → proceed with cached content if available
  • Timeout → Timeout → retry 3×, then report
  • Transient error → retry 3× with backoff, then Failed
  • Cancellation → Cancelled
```

### Addressables Handle Management

Every `AsyncOperationHandle` created by the service is released. Here is exactly when:

| Operation | Handle Created | Handle Released |
|-----------|---------------|-----------------|
| `Addressables.InitializeAsync()` | Step 2 | Immediately after `await` — init is idempotent, no need to keep the handle |
| `Addressables.CheckForCatalogUpdates()` | Step 3 | After `List<string>` result is extracted — the string list owns the data |
| `Addressables.UpdateCatalogs()` | Step 4 | After `List<IResourceLocator>` is extracted — locators are ref-counted internally |
| `Addressables.GetDownloadSizeAsync(required)` | Step 5 | After `long` size is read |
| `Addressables.GetDownloadSizeAsync(optional)` | Step 5 | After `long` size is read |
| `Addressables.DownloadDependenciesAsync(required)` | Step 6 | After download completes AND final status is read |
| `Addressables.DownloadDependenciesAsync(optional)` | Step 7 | After download completes AND final status is read |

All handles are wrapped in `try { … } finally { Release(handle); }` blocks so
they are released even if the operation throws.

**Why release each handle?** Addressables uses reference counting. If a handle
is not released, the underlying `AssetBundle` stays in memory permanently — a leak.
The `DownloadDependenciesAsync` handle is released after the download finishes
because the downloaded bundles are cached to disk by Addressables; they don't
need the handle to stay alive.

### Error Handling Details

| Scenario | Detection | Behavior |
|----------|-----------|----------|
| **No internet + content cached** | `Application.internetReachability` + exception message inspection | Return `Offline` → proceed to game |
| **No internet + content NOT cached** | Same, but `GetDownloadSize > 0` | Return `Offline` → caller should block entry |
| **CDN unreachable / DNS failure** | `UnityWebRequestException` with connection errors | Classified as transient → retry 3×, then `Failed` |
| **Catalog check timeout** | `UniTask.WhenAny` with `UniTask.Delay(30s)` | Retry 3×, then `Timeout` |
| **Download interrupted mid-way** | `DownloadDependenciesAsync` throws | Retry 3×, then `Failed` (user may retry from UI) |
| **User presses Cancel** | `CancellationToken` triggered by `CancelUpdate()` | `OperationCanceledException` → `Cancelled` |
| **Duplicate service calls** | `_runningUpdateTcs != null` guard | Returns the existing `UniTask` — no second pipeline |
| **Catalog corruption after UpdateCatalogs fails** | Exception from `UpdateCatalogs` | Retry 3×, then `Failed` → may need full re-download |

### Required vs Optional Content

Assign Addressable labels to your asset groups:

- **Label `"Required"`** (configurable via `_requiredLabel`): Core bundles needed to
  reach the main menu. This includes UI prefabs, hero data, localization tables,
  core scripts — anything referenced before/during `GameBootstrap`.

- **Label `"Optional"`** (configurable via `_optionalLabel`): Bundles that can be
  downloaded after the player is in the game. Examples: high-res textures,
  voice-over audio, cosmetic skins, seasonal event assets.

The service downloads **required** content during bootstrap and **optional** content
later via `DownloadOptionalContentAsync()`. If a label string is empty, that phase
is skipped entirely.

### Integration into GameBootstrap

Add this as **step 0** in `GameBootstrap.RunAsync()`, before Nakama authentication:

```csharp
// In GameBootstrap.RunAsync():
public async UniTask RunAsync(Action<float, string> onProgress)
{
    var progress = new BootstrapProgress(onProgress);
    using var cts = new CancellationTokenSource();

    // ── Step 0: Remote content update ──
    bool updateOk = await RemoteUpdateBootstrapStep.RunAsync(
        progress.Report, cts.Token);
    if (!updateOk)
    {
        // Show error UI and block entry.
        // The bootstrap progress callback will already show "Update failed."
        return;
    }

    // ── Step 1: Nakama auth (existing) ──
    // ...
}
```

### Caching

Addressables caches downloaded bundles to disk automatically (via Unity's
`Caching` API). On the next launch:

1. `CheckForCatalogUpdates` compares the local catalog hash with the remote.
2. If the hashes match → no update needed, exit early with `NoUpdateNeeded`.
3. If the hashes differ → `UpdateCatalogs` loads the new catalog.
4. `GetDownloadSizeAsync` only counts bundles whose local cache entry is stale
   or missing. Bundles that haven't changed return 0 bytes.
5. `DownloadDependenciesAsync` only fetches the delta.

No manual cache management is required — Addressables handles this.

### Timeout and Retry Configuration

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `_timeoutSeconds` | 30 | Per-operation timeout (applies to each Addressables call) |
| `_maxRetryCount` | 3 | Retries before giving up (only on transient errors) |
| `_retryDelaySeconds` | 2 | Base delay; multiplied by attempt number (2, 4, 6 seconds) |

Tune these in the Inspector on the `AddressableRemoteUpdateService` GameObject.

### Preventing Memory Leaks

1. Every `AsyncOperationHandle` is released in a `finally` block.
2. The `_runningUpdateTcs` is nulled in `finally` so the GC can collect it.
3. `CancelUpdate()` disposes the internal `CancellationTokenSource`.
4. `OnDestroy()` calls `CancelUpdate()` to clean up if the GameObject is destroyed.
5. The `LoadingScreenView` disposes its own `CancellationTokenSource` in `PlayHideAsync()`.
6. Button listeners are unregistered in `Cleanup()`.
