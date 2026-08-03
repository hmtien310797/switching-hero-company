using System;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Config tolerance cho local verifier (DOCX §36 — PvPVerificationConfig). Local verifier không
    /// phải security thật — ép client produce correct request/summaries/validation cho server sau.
    /// </summary>
    [Serializable]
    public sealed class PvPVerificationConfig
    {
        public float DurationToleranceSeconds = 30f;
        public float DamageToleranceMultiplier = 2f;
        public float HealingToleranceMultiplier = 2f;
        public int BasicAttackCountTolerance = 50;
        public int SkillCastCountTolerance = 20;
        public int UltimateCastCountTolerance = 10;
    }
}
