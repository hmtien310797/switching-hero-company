namespace Immortal_Switch.Scripts.Pvp.Repositories
{
    /// <summary>
    /// Repository localStorage cho PvP. Phase 1 = <see cref="Es3PvPRepository"/> (ES3);
    /// Phase 2 server thay bằng RemotePvPRepository/API client (DOCX §6, §40).
    /// UI, combat core, service chỉ phụ thuộc interface này — không phụ thuộc ES3 hay mock.
    /// </summary>
    public interface IPvPRepository
    {
        bool HasKey(string key);

        /// <summary>
        /// Đọc data group. Nếu key chưa có → trả <c>new T()</c>. Nếu có → deserialize,
        /// reconcile <see cref="IPvPSaveData.SchemaVersion"/> (migrate + re-save nếu cũ).
        /// </summary>
        T Load<T>(string key) where T : class, IPvPSaveData, new();

        /// <summary>Lưu data group, ép SchemaVersion = current trước khi save (transaction-like).</summary>
        void Save<T>(string key, T data) where T : class, IPvPSaveData, new();

        void DeleteKey(string key);

        /// <summary>Xoá toàn bộ key PvP (dev reset / clean QA — DOCX §6, §14).</summary>
        void ResetAll();
    }
}
