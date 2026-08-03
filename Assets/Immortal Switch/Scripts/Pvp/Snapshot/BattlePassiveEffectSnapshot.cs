using System;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Mechanic-changing effect (DOCX §9 — "Mechanic-changing sources create PassiveEffectSnapshot
    /// or a dedicated snapshot"). <b>FLAGGED:</b> generic envelope; M6 có thể định nghĩa dedicated
    /// snapshot thay JsonPayload.
    /// </summary>
    [Serializable]
    public sealed class BattlePassiveEffectSnapshot
    {
        public string EffectId;
        public string SourceId;
        public string JsonPayload;
    }
}
