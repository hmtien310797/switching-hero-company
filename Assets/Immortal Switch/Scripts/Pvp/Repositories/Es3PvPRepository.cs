using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Repositories
{
    /// <summary>
    /// Phase-1 ES3-backed implementation của <see cref="IPvPRepository"/>. Mỗi data group = 1 key
    /// riêng + <see cref="IPvPSaveData.SchemaVersion"/>; load reconcile version, migrate nếu cũ
    /// rồi re-save (giống TransmutationSystemStorage clamp-and-re-save). UI/combat KHÔNG gọi thẳng ES3.
    /// (DOCX §6 — "Only repository/service layers read or write ES3".)
    /// </summary>
    internal sealed class Es3PvPRepository : IPvPRepository
    {
        public bool HasKey(string key)
        {
            return ES3.KeyExists(key);
        }

        public T Load<T>(string key) where T : class, IPvPSaveData, new()
        {
            // First-run / missing key fallback (QA §41 — "corrupted/missing key fallback").
            if (!ES3.KeyExists(key))
                return new T();

            T data = ES3.Load<T>(key);
            if (data == null)
                return new T();

            // Reconcile schema version: nếu cũ hơn current → migrate rồi bump + re-save.
            if (data.SchemaVersion < PvpEs3Keys.CurrentSchemaVersion)
            {
                Migrate(data, data.SchemaVersion);
                data.SchemaVersion = PvpEs3Keys.CurrentSchemaVersion;
                ES3.Save(key, data);
            }

            return data;
        }

        public void Save<T>(string key, T data) where T : class, IPvPSaveData, new()
        {
            if (data == null)
                return;

            // Ép version current trước khi save — transaction-like: runtime cache chỉ nên update
            // sau khi save thành công (DOCX §6). Caller chịu trách nhiệm mutate-then-Save.
            data.SchemaVersion = PvpEs3Keys.CurrentSchemaVersion;
            ES3.Save(key, data);
        }

        public void DeleteKey(string key)
        {
            if (ES3.KeyExists(key))
                ES3.DeleteKey(key);
        }

        public void ResetAll()
        {
            foreach (var key in PvpEs3Keys.All)
            {
                if (ES3.KeyExists(key))
                    ES3.DeleteKey(key);
            }

            Debug.Log("[PvP] All local PvP ES3 keys cleared.");
        }

        /// <summary>
        /// Hook migrate cho data group. Khi bump <see cref="PvpEs3Keys.CurrentSchemaVersion"/>,
        /// thêm nhánh cho fromVersion cũ. v1 → v1 = no-op (Phase-1 khởi tạo).
        /// TODO (future version): thay bằng typed migrator registry nếu cần migrate theo-T.
        /// </summary>
        private static void Migrate(IPvPSaveData data, int fromVersion)
        {
            switch (fromVersion)
            {
                default:
                    // v1 hiện tại — không có migration cũ.
                    break;
            }
        }
    }
}
