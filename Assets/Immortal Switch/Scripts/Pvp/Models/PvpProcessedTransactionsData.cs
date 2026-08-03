using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Idempotency: RollId / TransactionId đã xử lý local (DOCX §24, §27 — "transaction-like
    /// and idempotent locally"). Key <see cref="PvpEs3Keys.ProcessedTransactions"/>.
    /// Dùng <see cref="List{T}"/> thay HashSet để tương thích ES3; volume thấp nên Contains OK.
    /// </summary>
    [Serializable]
    public sealed class PvpProcessedTransactionsData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public List<string> ProcessedIds = new();

        public bool IsProcessed(string id)
        {
            return !string.IsNullOrEmpty(id) && ProcessedIds.Contains(id);
        }

        public void MarkProcessed(string id)
        {
            if (string.IsNullOrEmpty(id) || ProcessedIds.Contains(id))
                return;
            ProcessedIds.Add(id);
        }
    }
}
