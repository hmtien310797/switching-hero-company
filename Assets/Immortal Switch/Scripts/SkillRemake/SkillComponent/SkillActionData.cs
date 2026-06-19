using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [Serializable]
    public class SkillActionData
    {
        public SkillActionType ActionType = SkillActionType.DealDamage;
        [Range(0, 100)] public float ChancePercent = 100f;
        public SkillTargetType TargetTypeOverride = SkillTargetType.CurrentTarget;

        public SkillDamageData Damage = new();
        public SkillDotData Dot = new();
        public SkillStatModifierData StatModifier = new();
        public SkillAreaData Area = new();
        public SkillProjectileData Projectile = new();
        public SkillStackActionData Stack = new();
        public SkillDataSO TriggerSkill;
    }

    [Serializable]
    public class SkillDamageData
    {
        [Tooltip("Bonus percent. 0 = normal attack damage. 528 = normal + 528% ATK bonus.")]
        public float SkillDamageBonusPercent;
        public bool ScaleWithClassSkillLevel = true;
        public bool CountAsSkillDamage = true;
    }

    [Serializable]
    public class SkillDotData
    {
        public string EffectId = "DOT";
        [Tooltip("Snapshot tick damage bonus percent. 0 = normal attack damage per tick.")]
        public float TickDamageBonusPercent;
        public bool ScaleDamageWithClassSkillLevel = true;
        [Min(0.01f)] public float TickInterval = 1f;
        [Min(0.01f)] public float Duration = 5f;
        public DamageType DamageType = DamageType.Normal;
        public DotStackRule StackRule = DotStackRule.Refresh;
    }

    [Serializable]
    public class SkillStatModifierData
    {
        public string ModifierId;
        public StatType StatType;
        public float PercentValue;
        public bool ScaleValueWithClassSkillLevel = true;
        public float Duration = 5f;
    }

    [Serializable]
    public class SkillAreaData
    {
        public SkillAreaRuntime AreaPrefab;
        public SkillAreaShape Shape = SkillAreaShape.Circle;
        public SkillAreaPositionType PositionType = SkillAreaPositionType.Target;
        public float Radius = 2f;
        public Vector2 BoxSize = new(2f, 2f);
        public float Duration;
        public float TickInterval;
        public bool HitOncePerTarget = true;
        public List<SkillActionData> OnHitActions = new();
    }

    [Serializable]
    public class SkillProjectileData
    {
        public BulletPatternConfig BulletPatternConfig;
        public HomingChainBulletConfig HomingChainBulletConfig;
        public List<SkillActionData> OnHitActions = new();
    }

    [Serializable]
    public class SkillStackActionData
    {
        public string StackKey;
        public int Amount = 1;
    }
}
