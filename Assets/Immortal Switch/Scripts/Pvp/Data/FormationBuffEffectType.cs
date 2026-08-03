namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Effect của buff (DOCX §18 — "Effect" examples: StatModifier, Damage, Heal, Shield,
    /// ApplyBuff, Cooldown, Stack, PreventDeath). Revive/reflect/prevent-death/cooldown-reset là
    /// effects, KHÔNG phải StatType (DOCX §19).
    /// </summary>
    public enum FormationBuffEffectType
    {
        StatModifier = 0,
        Damage = 1,
        Heal = 2,
        Shield = 3,
        ApplyBuff = 4,
        Cooldown = 5,
        Stack = 6,
        PreventDeath = 7
    }
}
