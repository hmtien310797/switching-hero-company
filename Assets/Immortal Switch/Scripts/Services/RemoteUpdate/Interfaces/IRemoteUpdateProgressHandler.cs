namespace Immortal_Switch.Scripts.RemoteUpdate
{
    /// <summary>
    /// Implement this interface on any component that needs to react to
    /// remote-content-update progress and completion.
    /// Typical implementors: a loading-screen UIView, a bootstrap adapter.
    /// </summary>
    public interface IRemoteUpdateProgressHandler
    {
        /// <summary>
        /// Called every time the pipeline advances to a new status or
        /// new download bytes are available. On a fast machine this may
        /// fire every frame during the Downloading phase.
        /// </summary>
        void OnProgress(RemoteContentUpdateProgress progress);

        /// <summary>
        /// Called exactly once when the pipeline reaches a terminal status.
        /// Check <see cref="RemoteContentUpdateResult.IsSuccess"/> to decide
        /// whether to proceed into the game or show an error dialog.
        /// </summary>
        void OnComplete(RemoteContentUpdateResult result);
    }
}
