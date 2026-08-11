namespace Editor.ExcelConfigTool.Services
{
    /// <summary>
    /// Hợp đồng tối thiểu mà một importer window "Tools/Game Data" cần cung cấp
    /// để GameDataSyncCoordinator có thể chạy nó trong batch.
    /// </summary>
    public interface IGameDataSyncStep
    {
        /// <summary>true khi import đang chạy (coordinator poll cờ này).</summary>
        bool IsRunning { get; }

        /// <summary>true nếu bước chạy gần nhất thất bại.</summary>
        bool LastImportFailed { get; }

        /// <summary>Chạy import không cần cửa sổ, không hiện dialog chặn.</summary>
        void RunForBatch();
    }
}