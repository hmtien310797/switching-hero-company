using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// 1 ownership record cho 1 BuffId (DOCX §17 — "Each BuffId has one ownership record").
    /// Duplicate gacha → shard, KHÔNG tạo instance thứ 2 (DOCX §21). Equipped position
    /// (Front/Back) không lưu ở đây — nằm trong pvp_formation (one BuffId chỉ 1 vị trí).
    /// </summary>
    [Serializable]
    public sealed class PvpOwnedBuff
    {
        public string BuffId;
        public bool IsUnlocked;
        public int Level = 1;
        public int Shards;
        public int BuffDataVersion = 1;
    }

    /// <summary>
    /// Tập ownership buff. Key <see cref="PvpEs3Keys.OwnedBuffs"/> (DOCX §6, §24).
    /// </summary>
    [Serializable]
    public sealed class PvpOwnedBuffData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public List<PvpOwnedBuff> Buffs = new();
    }
}
