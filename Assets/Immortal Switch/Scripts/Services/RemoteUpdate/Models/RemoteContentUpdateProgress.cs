namespace Immortal_Switch.Scripts.RemoteUpdate
{
    /// <summary>
    /// Snapshot of the current download progress, reported to the UI each frame
    /// or whenever a discrete step completes.
    /// </summary>
    public readonly struct RemoteContentUpdateProgress
    {
        /// <summary>Which pipeline stage is currently active.</summary>
        public RemoteContentUpdateStatus Status { get; }

        /// <summary>Bytes downloaded so far in the current download phase.</summary>
        public long DownloadedBytes { get; }

        /// <summary>Total bytes to download in the current phase (may be 0 before size calc).</summary>
        public long TotalBytes { get; }

        /// <summary>
        /// Human-readable label describing what is happening right now.
        /// Examples: "Checking for updates…", "Downloading required content…"
        /// </summary>
        public string CurrentLabel { get; }

        /// <summary>Normalised progress [0, 1]. Safe to drive a slider or fill bar.</summary>
        public float Percent { get; }

        /// <summary>
        /// True when the overall pipeline has finished (Complete, Failed, Offline, Timeout, or Cancelled).
        /// </summary>
        public bool IsDone { get; }

        /// <summary>Non-null error description when Status is Failed or Timeout.</summary>
        public string ErrorMessage { get; }

        /// <summary>Size of the required-content download (populated after size calc).</summary>
        public long RequiredBytes { get; }

        /// <summary>Size of the optional-content download (populated after size calc).</summary>
        public long OptionalBytes { get; }

        public RemoteContentUpdateProgress(
            RemoteContentUpdateStatus status,
            long downloadedBytes,
            long totalBytes,
            string currentLabel,
            float percent,
            bool isDone,
            string errorMessage = null,
            long requiredBytes = 0,
            long optionalBytes = 0)
        {
            Status = status;
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
            CurrentLabel = currentLabel ?? string.Empty;
            Percent = percent;
            IsDone = isDone;
            ErrorMessage = errorMessage;
            RequiredBytes = requiredBytes;
            OptionalBytes = optionalBytes;
        }

        /// <summary>
        /// Convenience factory for a status-only progress update (no download data).
        /// </summary>
        public static RemoteContentUpdateProgress StatusOnly(
            RemoteContentUpdateStatus status,
            string label,
            bool isDone = false,
            string errorMessage = null)
        {
            return new RemoteContentUpdateProgress(
                status, 0, 0, label, 0f, isDone, errorMessage);
        }

        public override string ToString()
        {
            return TotalBytes > 0
                ? $"[{Status}] {CurrentLabel} — {DownloadedBytes}/{TotalBytes} bytes ({Percent:P1})"
                : $"[{Status}] {CurrentLabel}";
        }
    }
}
