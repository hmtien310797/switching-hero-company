using System;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// 1 trong 3 choice của 1 roll (DOCX §23). Duplicate → shard value.
    /// </summary>
    [Serializable]
    public sealed class FormationBuffRollChoice
    {
        public string BuffId;
        public BuffRarity Rarity;
        public bool IsAlreadyOwned;
        public int DuplicateShardValue;
    }

    /// <summary>
    /// 1 roll session: 3 choices, player chọn 1. Pending roll phải survive restart (DOCX §24 —
    /// "A pending roll must survive application restart"). Đây là ELEMENT (không phải group root)
    /// nên không mang SchemaVersion — group root là <see cref="PvpPendingBuffRollData"/> /
    /// <see cref="PvpBuffRollHistoryData"/>.
    /// <b>FLAGGED (ambiguity):</b> DOCX §23 không khai báo <c>SelectedBuffId</c>; thêm để hỗ trợ
    /// idempotent re-select (DOCX §24 — "Selecting the same RollId twice returns the previously
    /// stored result and never grants shards or ownership twice").
    /// </summary>
    [Serializable]
    public sealed class FormationBuffRollSession
    {
        public string RollId;
        public string PoolId;
        public long CreatedAtUnix;
        public long ExpiresAtUnix;
        public int Cost;
        public string CurrencyId;
        public FormationBuffRollChoice[] Choices;
        public bool IsResolved;
        public string SelectedBuffId;   // set sau khi player chọn — để idempotent re-select (§24).

        /// <summary>FLAGGED: stored resolve result cho idempotent re-select (DOCX §24 — "returns the
        /// previously stored result"). Trả về nguyên kết quả lần resolve đầu, không grant lại.</summary>
        public FormationBuffRollResolveResult StoredResult;
    }
}
