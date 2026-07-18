namespace Immortal_Switch.Scripts.RemoteUpdate
{
    /// <summary>
    /// Represents the current stage of the remote content update pipeline.
    /// The UI maps each status to a user-visible message.
    /// </summary>
    public enum RemoteContentUpdateStatus : byte
    {
        /// <summary>Initial state — not started.</summary>
        Idle = 0,

        /// <summary>Initializing the Addressables system.</summary>
        Initializing = 1,

        /// <summary>Checking for remote catalog updates on the CDN.</summary>
        CheckingForUpdates = 2,

        /// <summary>No catalog update is available; local content is current.</summary>
        NoUpdateNeeded = 3,

        /// <summary>Downloading updated catalog metadata.</summary>
        UpdatingCatalogs = 4,

        /// <summary>Calculating the size of bundles that must be downloaded.</summary>
        CalculatingDownloadSize = 5,

        /// <summary>Actively downloading required and/or optional bundles.</summary>
        Downloading = 6,

        /// <summary>All updates finished successfully.</summary>
        Complete = 7,

        /// <summary>One or more steps failed after retries were exhausted.</summary>
        Failed = 8,

        /// <summary>No network connectivity — running with previously cached content.</summary>
        Offline = 9,

        /// <summary>The operation timed out after the configured duration.</summary>
        Timeout = 10,

        /// <summary>The operation was cancelled by the caller or user.</summary>
        Cancelled = 11
    }
}
