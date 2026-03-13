using System;

namespace Immortal_Switch.Scripts.StatSystem
{
    public enum StatType
    {
        MaxHP,
        ATK,
        DEF,
        Accuracy,
        AttackSpeed,
        AttackRange,
        MoveSpeed,
        CritChance,
        CritDamage,
        DamageToNormalMonster,
        DamageToHeroMonster,
        DamageReduction,
        ClassSkillDamage,
        ExclusiveSkillDamage,
        SwitchSkillDamage
    }

    public enum ModifierOp
    {
        Add,
        Multiply
    }

    public enum BuffKind
    {
        Buff,
        Debuff
    }

    public enum BuffStackRule
    {
        None,       
        Refresh,   
        Stack,     
        Replace     
    }
    
    [Flags]
    public enum StatusEffectType
    {
        None    = 0,
        Stun    = 1 << 0,
        Silence = 1 << 1,
        Freeze  = 1 << 2
    }

    public enum PeriodicEffectType
    {
        None,
        DamageOverTime,
        HealOverTime
    }

    public enum DamageType
    {
        Normal,
        Poison,
        Burn,
        True
    }
}