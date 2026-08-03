using System;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Wrapper group-root cho pending roll. Key <see cref="PvpEs3Keys.PendingBuffRoll"/>
    /// (DOCX §6, §24). Mang SchemaVersion; chứa 1 <see cref="FormationBuffRollSession"/> hoặc null.
    /// Tạo roll mới BỊ BLOCK khi pending chưa resolve (DOCX §24 — "Creating a new roll is blocked
    /// while an unresolved pending roll exists").
    /// </summary>
    [Serializable]
    public sealed class PvpPendingBuffRollData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public FormationBuffRollSession Pending;
    }
}
