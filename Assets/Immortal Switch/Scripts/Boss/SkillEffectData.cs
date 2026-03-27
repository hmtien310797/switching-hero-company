using System;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [Serializable]
    public class SkillEffectData
    {
        public SkillEffectType EffectType;

        [Header("Common")]
        public float Value;
        public float Duration;
        public int StackCount = 1;

        [Header("Damage")]
        [Tooltip("Ví dụ 180 = 180% ATK")]
        public float DamageMultiplier;

        [Header("Stat Modifier")]
        public StatType StatType;
        [Tooltip("Ví dụ -20 = giảm 20%, +25 = tăng 25%")]
        public float StatModifierPercent;

        [Header("Status")]
        public StatusEffectType StatusType;

        [Header("Chance")]
        [Range(0, 100)]
        public float ChancePercent = 100f;
        
        [Header("Scaling")]
        public SkillScalingStat ScalingStat = SkillScalingStat.Attack;

        [Header("Target")]
        public SkillTargetType TargetTypeOverride = SkillTargetType.CurrentTarget;
    }
    
    public enum StatusEffectType
    {
        Curse,
        Stun,
        Freeze,
        Mark,
        CustomDebuff
    }
    
    public enum SkillEffectType
    {
        Damage,
        HealPercentMaxHp,
        ShieldPercentMaxHp,
        ModifyStatPercent,
        ApplyStatus,
        ReflectDamagePercent,
        DamageReductionPercent,
        AddMark,
        TeleportToTarget
    }
}