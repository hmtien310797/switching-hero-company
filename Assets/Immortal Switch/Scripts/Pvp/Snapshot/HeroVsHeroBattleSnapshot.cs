using System;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Immutable battle snapshot (DOCX §10). Matchmaking tạo BattleId + RandomSeed + snapshot.
    /// Sau khi tạo, formation/equipment thay đổi KHÔNG ảnh hưởng battle đang chạy; combat không đọc
    /// UserDataCache/ES3.
    /// </summary>
    [Serializable]
    public sealed class HeroVsHeroBattleSnapshot
    {
        public string BattleId;
        public int SnapshotVersion = 1;
        public int BattleRulesVersion = 1;
        public ulong RandomSeed;
        public TeamBattleSnapshot Attacker;
        public TeamBattleSnapshot Defender;
    }
}
