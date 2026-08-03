using System;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>Kết quả upgrade (DOCX §22 — FormationBuffUpgradeResult). Atomic (§28).</summary>
    [Serializable]
    public sealed class FormationBuffUpgradeResult
    {
        public string TransactionId;
        public string BuffId;
        public bool Success;
        public int NewLevel;
        public string Message;
    }
}
