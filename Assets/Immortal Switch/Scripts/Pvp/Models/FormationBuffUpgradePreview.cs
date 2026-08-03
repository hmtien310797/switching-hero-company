using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>Preview upgrade cho 1 buff (DOCX §22 — FormationBuffUpgradePreview, §28).</summary>
    [Serializable]
    public sealed class FormationBuffUpgradePreview
    {
        public string BuffId;
        public int CurrentLevel;
        public int NextLevel;
        public int RequiredShards;
        public long RequiredArenaToken;
        public bool CanUpgrade;
        public string Reason;             // nếu CanUpgrade = false
        public List<string> CurrentValues = new();
        public List<string> NextValues = new();
    }
}
