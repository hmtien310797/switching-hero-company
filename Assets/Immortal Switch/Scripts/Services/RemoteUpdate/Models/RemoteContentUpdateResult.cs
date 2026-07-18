using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.RemoteUpdate
{
    /// <summary>
    /// Final result produced by <see cref="AddressableRemoteUpdateService"/>
    /// after the update pipeline completes (successfully or otherwise).
    /// </summary>
    public readonly struct RemoteContentUpdateResult
    {
        /// <summary>Terminal status of the pipeline.</summary>
        public RemoteContentUpdateStatus Status { get; }

        /// <summary>Wall-clock time spent in the pipeline.</summary>
        public TimeSpan ElapsedTime { get; }

        /// <summary>True when the remote catalog was newer than the local one.</summary>
        public bool HadUpdates { get; }

        /// <summary>True when <see cref="Addressables.UpdateCatalogs"/> completed successfully.</summary>
        public bool CatalogUpdated { get; }

        /// <summary>Number of bytes downloaded for required bundles.</summary>
        public long RequiredDownloadedBytes { get; }

        /// <summary>Number of bytes downloaded for optional bundles.</summary>
        public long OptionalDownloadedBytes { get; }

        /// <summary>Per-step or aggregate error messages collected during the pipeline.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// True when the pipeline finished without errors.
        /// Offline is NOT considered success — callers must separately validate
        /// cached content via <see cref="AddressableRemoteUpdateService.IsContentAvailableOfflineAsync"/>.
        /// </summary>
        public bool IsSuccess =>
            Status == RemoteContentUpdateStatus.Complete ||
            Status == RemoteContentUpdateStatus.NoUpdateNeeded;

        public RemoteContentUpdateResult(
            RemoteContentUpdateStatus status,
            TimeSpan elapsedTime,
            bool hadUpdates,
            bool catalogUpdated,
            long requiredDownloadedBytes,
            long optionalDownloadedBytes,
            IReadOnlyList<string> errors = null)
        {
            Status = status;
            ElapsedTime = elapsedTime;
            HadUpdates = hadUpdates;
            CatalogUpdated = catalogUpdated;
            RequiredDownloadedBytes = requiredDownloadedBytes;
            OptionalDownloadedBytes = optionalDownloadedBytes;
            Errors = errors ?? Array.Empty<string>();
        }

        /// <summary>Quick failure factory when a step gives up early.</summary>
        public static RemoteContentUpdateResult Failed(
            TimeSpan elapsed,
            string error,
            bool hadUpdates = false,
            bool catalogUpdated = false)
        {
            return new RemoteContentUpdateResult(
                RemoteContentUpdateStatus.Failed,
                elapsed,
                hadUpdates,
                catalogUpdated,
                0, 0,
                new[] { error });
        }

        public override string ToString()
        {
            var totalBytes = RequiredDownloadedBytes + OptionalDownloadedBytes;
            return totalBytes > 0
                ? $"[{Status}] Downloaded {totalBytes} bytes in {ElapsedTime.TotalSeconds:F1}s"
                : $"[{Status}] Elapsed {ElapsedTime.TotalSeconds:F1}s";
        }
    }
}
