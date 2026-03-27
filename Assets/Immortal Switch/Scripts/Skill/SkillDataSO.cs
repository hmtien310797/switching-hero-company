using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;
using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public enum SkillCastType
    {
        Active,
        Passive,
        Switch
    }

    public enum SkillScalingStat
    {
        Attack,
        MaxHp,
        Defense
    }

    [Serializable]
    public class SkillDescriptionParam
    {
        public string Key;
        public float Value;
        public bool IsPercent;
        public int DecimalPlaces = 0;
    }

    [Serializable]
    public class SkillDebuffEffectData
    {
        public string DebuffId;
    }

    [Serializable]
    public class SkillDotEffectData
    {
        public string EffectId;
        public SkillScalingStat ScalingStat = SkillScalingStat.Attack;
        public float TickCoefficient = 0.3f;
        public float Duration = 5f;
        public float TickInterval = 1f;
        public DamageType DamageType = DamageType.Normal;
        public DotStackRule StackRule = DotStackRule.Refresh;
    }

    [Serializable]
    public class SkillPhaseData
    {
        public int PhaseId;
        public string SpineEventName;
        public SkillTargetType TargetTypeOverride;
        public List<SkillEffectData> Effects = new();
    }

    [Serializable]
    public class SkillLevelData
    {
        public int Level = 1;
        public List<SkillPhaseData> Phases = new();
        public List<SkillDescriptionParam> DescriptionParams = new();
    }

    [CreateAssetMenu(fileName = "Skill_", menuName = "ScriptableObjects/SkillDataSo")]
    public class SkillDataSO : ScriptableObject
    {
        [Header("Legacy Data")]
        public int SkillId;
        public float CooldownTime = 10f;
        public TierSkillGroup SkillGroup;
        public TierSkill Tier;
        public int NumSpawn = 1;
        public BaseExternalSkillController SkillPrefab;
        public Sprite skillIcon;
        public float NomalDame;
        public float FinalDame;

        [Header("New Skill Metadata")]
        public string SkillKey;
        public string SkillName;
        [TextArea(3, 8)] public string DescriptionTemplate;
        public SkillCastType CastType = SkillCastType.Active;
        public SkillTargetType DefaultTargetType = SkillTargetType.CurrentTarget;
        public int MaxLevel = 1;

        [Header("New Skill Levels")]
        public List<SkillLevelData> Levels = new();

        public SkillLevelData GetLevelData(int level)
        {
            if (Levels == null || Levels.Count == 0)
                return null;

            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i] != null && Levels[i].Level == level)
                    return Levels[i];
            }

            return null;
        }

        public SkillPhaseData GetPhaseByEvent(int level, string spineEventName)
        {
            if (string.IsNullOrEmpty(spineEventName))
                return null;

            var levelData = GetLevelData(level);
            if (levelData == null || levelData.Phases == null)
                return null;

            for (int i = 0; i < levelData.Phases.Count; i++)
            {
                var phase = levelData.Phases[i];
                if (phase == null) continue;

                if (string.Equals(phase.SpineEventName, spineEventName, StringComparison.Ordinal))
                    return phase;
            }

            return null;
        }
    }
}