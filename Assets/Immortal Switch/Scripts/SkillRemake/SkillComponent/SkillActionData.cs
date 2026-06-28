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

        public SkillAreaShape Shape =
            SkillAreaShape.Circle;

        public SkillAreaPositionType PositionType =
            SkillAreaPositionType.Target;

        [Tooltip(
            "Chỉ áp dụng cho Box.\n" +
            "Center: origin là tâm box.\n" +
            "Forward: box kéo về phía trước.\n" +
            "Backward: box kéo về phía sau.")]
        public SkillAreaAnchor Anchor =
            SkillAreaAnchor.Center;

        public float Radius = 2f;

        [Tooltip(
            "X = chiều dài theo hướng cast.\n" +
            "Y = chiều rộng vuông góc hướng cast.")]
        public Vector2 BoxSize =
            new Vector2(2f, 2f);

        public float Duration;
        public float TickInterval;
        public bool HitOncePerTarget = true;

        public List<SkillActionData> OnHitActions = new();

        public float BoxLength =>
            Mathf.Max(0f, BoxSize.x);

        public float BoxWidth =>
            Mathf.Max(0f, BoxSize.y);

        public Vector3 ResolveAreaCenter(
            Vector3 areaOrigin,
            Vector3 castDirection)
        {
            if (Shape != SkillAreaShape.Box)
                return areaOrigin;

            castDirection.y = 0f;

            if (castDirection.sqrMagnitude <= 0.0001f)
                castDirection = Vector3.right;

            castDirection.Normalize();

            float halfLength =
                BoxLength * 0.5f;

            switch (Anchor)
            {
                case SkillAreaAnchor.Forward:
                    return areaOrigin
                           + castDirection * halfLength;

                case SkillAreaAnchor.Backward:
                    return areaOrigin
                           - castDirection * halfLength;

                case SkillAreaAnchor.Center:
                default:
                    return areaOrigin;
            }
        }
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
