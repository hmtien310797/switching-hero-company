namespace Immortal_Switch.Scripts.Pvp.Repositories
{
    /// <summary>
    /// Marker cho mọi data-group ROOT được ES3PvPRepository lưu. Mỗi nhóm phải mang
    /// <see cref="SchemaVersion"/> để migrate (DOCX §6 — "Each data group uses a dedicated
    /// key and SchemaVersion"). Các element con (PvpOwnedBuff, history entry, roll choice...)
    /// không implements interface này — chỉ group root mới cần version.
    /// </summary>
    public interface IPvPSaveData
    {
        int SchemaVersion { get; set; }
    }
}
