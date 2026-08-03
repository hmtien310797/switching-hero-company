using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Recent roll sessions cho QA/debug (DOCX §24, §29). Key
    /// <see cref="PvpEs3Keys.BuffRollHistory"/>. Retention count configurable.
    /// </summary>
    [Serializable]
    public sealed class PvpBuffRollHistoryData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public int MaxRetention = PvpDefaults.MaxRollHistoryEntries;
        public List<FormationBuffRollSession> Entries = new();
    }
}
