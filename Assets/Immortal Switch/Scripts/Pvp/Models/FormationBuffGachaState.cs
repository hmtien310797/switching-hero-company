using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Pity state cho 1 pool (DOCX §23). Reset/advance pity chỉ sau khi player chọn result,
    /// không phải lúc tạo roll (DOCX §26).
    /// </summary>
    [Serializable]
    public sealed class FormationBuffPityState
    {
        public string PoolId;
        public int RollsSinceRareOrHigher;
        public int RollsSinceEpicOrHigher;
        public int RollsSinceLegendary;
        public int TotalRollCount;
    }

    /// <summary>
    /// Gacha state local: pity theo pool + pending roll id. Key
    /// <see cref="PvpEs3Keys.BuffGachaState"/> (DOCX §23, §24). SchemaVersion per DOCX §23.
    /// </summary>
    [Serializable]
    public sealed class FormationBuffGachaState : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, FormationBuffPityState> PityByPool = new();
        public string PendingRollId;
    }
}
