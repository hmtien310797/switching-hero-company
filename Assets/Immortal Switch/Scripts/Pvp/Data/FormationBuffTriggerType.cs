namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Trigger của Formation Buff (DOCX §18 — "Trigger" examples: Passive, BattleStarted,
    /// BasicAttackHit, SkillCast, UltimateCast, AfterDamageReceived, HpBelowThreshold, AllyDied).
    /// </summary>
    public enum FormationBuffTriggerType
    {
        Passive = 0,
        BattleStarted = 1,
        BasicAttackHit = 2,
        SkillCast = 3,
        UltimateCast = 4,
        AfterDamageReceived = 5,
        HpBelowThreshold = 6,
        AllyDied = 7
    }
}
