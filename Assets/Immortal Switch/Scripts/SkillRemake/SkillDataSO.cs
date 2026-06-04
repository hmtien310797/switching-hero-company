using System;
using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.SkillRemake;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.Skill
{
    public enum TierSkill
    {
        B,A,S,SS
    }
    
    [Serializable]
    public class SkillCastConfig
    {
        public bool RequireTarget = true;
        public bool MoveToCastRange = true;
        [Min(0f)] public float CastRange = 2f;
        [Min(0f)] public float Cooldown = 1f;
        public SkillTargetSelectType TargetSelectType = SkillTargetSelectType.NearestEnemy;

        [FormerlySerializedAs("AnimationName")]
        public string HeroAnimationName;
    }

    [Serializable]
    public class SkillRuntimeObjectConfig
    {
        public SkillRuntimeVisualType RuntimeVisualType = SkillRuntimeVisualType.SpawnedSkillObject;
        
        [ShowIf( "IsUsingSpineRuntime")]
        [Header("Spawned Skill Object")]
        [FormerlySerializedAs("RuntimePrefab")]
        public SkillRuntimeObject SpineRuntimePrefab;
        
        [ShowIf( "IsUsingBulletSpawner")]
        [Header("Spawned Projectile Pattern Spawner")]
        public BulletSpawnerSkillRuntimeObject runtimeObjectProjectileSpawner;
        
        [ShowIf( "IsUsingHomingChainBulletSpawner")]
        [Header("Spawned Homing Projectile")]
        public HomingChainBulletSkillRuntimeObject homingChainBulletSpawner;
        
        public SkillSpawnPositionType SpawnPositionType = SkillSpawnPositionType.Self;
        public SkillFollowType FollowType = SkillFollowType.None;
        public Vector3 SpawnOffset;

        [Header("Lifetime")]
        [Min(0f)] public float LifeTime = 1f;
        public bool UseLifeTime = true;
        
        [ShowIf( nameof(IsUsingSpineRuntime))]
        public bool DespawnOnAnimationComplete = true;

        [ShowIf( "IsUsingSpineRuntime")]
        [Header("Animation / Visual")]
        public string AnimationName;
        
        [ShowIf( "IsUsingSpineRuntime")]
        public bool LoopAnimation;

        [Header("Caster Lock")]
        public bool LockCasterWhileAlive;
        
        public bool LockCasterDuringHeroAnimation = true;

        private bool IsUsingSpineRuntime()
        {
            return RuntimeVisualType == SkillRuntimeVisualType.SpawnedSkillObject;
        }

        private bool IsUsingBulletSpawner()
        {
            return RuntimeVisualType == SkillRuntimeVisualType.HeroSpineObjectAndProjectile ||
                   RuntimeVisualType == SkillRuntimeVisualType.SpawnProjectilePatternBehavior;
        }
        
        private bool IsUsingHomingChainBulletSpawner()
        {
            return RuntimeVisualType == SkillRuntimeVisualType.SpawnHomingProjectile;
        }
    }

    [Serializable]
    public class SkillTriggerStackGainData
    {
        public SkillTriggerEventType EventType = SkillTriggerEventType.OnHit;
        public SkillEventSourceFilter SourceFilter = SkillEventSourceFilter.Owner;
        public int StackGainAmount = 1;
    }

    [Serializable]
    public class SkillEnemyCountConditionData
    {
        public bool Enabled;
        public int MinEnemyCount = 1;
        public float Range = 5f;
    }

    [Serializable]
    public class SkillPassiveConfig
    {
        public string StackKey;
        public int RequiredStack = 3;
        public int MaxStack = 3;
        public bool ResetStackOnTrigger = true;
        public bool ConsumeStackOnTrigger = true;
        public List<SkillTriggerStackGainData> StackGainTriggers = new();
        public SkillEnemyCountConditionData EnemyCountCondition = new();
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
    public class SkillLevelData
    {
        [Min(1)] public int Level = 1;
        public bool UseCastOverride;
        public bool UseRuntimeObjectOverride;
        public bool UsePassiveOverride;
        public bool HasBulletSpawner;

        [ShowIf("UseCastOverride")]
        [Header("Cast Override")]
        [Tooltip("Off = use SkillDataSO.CastConfig. On = use CastOverride for this level.")]
        public SkillCastConfig CastOverride = new();

        [ShowIf("UseRuntimeObjectOverride")]
        [Header("Runtime Object Override")]
        [Tooltip("Off = use SkillDataSO.RuntimeObjectConfig. On = use RuntimeObjectOverride for this level.")]
        public SkillRuntimeObjectConfig RuntimeObjectOverride = new();

        [ShowIf("UsePassiveOverride")]
        [Header("Passive Override")]
        [Tooltip("Off = use SkillDataSO.PassiveConfig. On = use PassiveOverride for this level.")]
        public SkillPassiveConfig PassiveOverride = new();
        
        [ShowIf("HasBulletSpawner")]
        [Header("Bullet Spawner Config")]
        public BulletPatternConfig BulletSpawnerConfig = new();
        public List<SkillPhaseData> Phases = new();
        public List<SkillDescriptionParam> DescriptionParams = new();
    }

    [Serializable]
    public class SkillPhaseData
    {
        public int PhaseId;
        public SkillPhaseTriggerType TriggerType = SkillPhaseTriggerType.RuntimeObjectEvent;

        [Tooltip("Generic event name. For Spine objects, this is the Spine event name, e.g. hit/shoot/finalhit.")]
        public string EventName = "hit";

        [Min(0f)] public float Delay;
        [Range(0f, 1f)] public float NormalizedTime;
        public List<SkillActionData> Actions = new();
    }

    [Serializable]
    public class SkillUpgradeCostEntry
    {
        [Min(1)] public int Level = 1;
        [Min(1)] public int RequiredShard = 2;
    }

    [CreateAssetMenu(fileName = "Skill_", menuName = "ScriptableObjects/SkillDataSO")]
    public class SkillDataSO : ScriptableObject
    {
        [Header("Identity")]
        public int SkillId;
        public string SkillKey;
        public string SkillName;
        public Sprite SkillIcon;
        [TextArea(3, 8)] public string DescriptionTemplate;

        [Header("Type")]
        public SkillOwnerType OwnerType = SkillOwnerType.ClassSkill;
        [Min(1)] public int MaxLevel = 1;

        [ShowIf("OwnerType", SkillOwnerType.ClassSkill)]
        [Header("Summon / Rarity")]
        public TierSkill SkillTier;

        [Header("Default Config")]
        public SkillCastConfig CastConfig = new();
        public SkillRuntimeObjectConfig RuntimeObjectConfig = new();
        [ShowIf("OwnerType", SkillOwnerType.PassiveSkill)]
        public SkillPassiveConfig PassiveConfig = new();

        [Header("Custom Behaviour")]
        public SkillBehaviour CustomBehaviourPrefab;

        [Header("Upgrade")]
        public List<SkillUpgradeCostEntry> UpgradeShardCosts = new();

        [Header("Levels")]
        public List<SkillLevelData> Levels = new();

        public int GetSafeLevel(int level)
        {
            return Mathf.Clamp(level, 1, Mathf.Max(1, MaxLevel));
        }

        public bool IsMaxLevel(int level)
        {
            return GetSafeLevel(level) >= Mathf.Max(1, MaxLevel);
        }

        public SkillLevelData GetLevelData(int level)
        {
            int safeLevel = GetSafeLevel(level);
            if (Levels == null || Levels.Count == 0)
                return null;

            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i] != null && Levels[i].Level == safeLevel)
                    return Levels[i];
            }

            return null;
        }

        public SkillCastConfig GetCastConfig(int level)
        {
            SkillLevelData levelData = GetLevelData(level);
            return levelData != null && levelData.UseCastOverride && levelData.CastOverride != null
                ? levelData.CastOverride
                : CastConfig;
        }

        public SkillRuntimeObjectConfig GetRuntimeObjectConfig(int level)
        {
            SkillLevelData levelData = GetLevelData(level);
            return levelData != null && levelData.UseRuntimeObjectOverride && levelData.RuntimeObjectOverride != null
                ? levelData.RuntimeObjectOverride
                : RuntimeObjectConfig;
        }

        public SkillPassiveConfig GetPassiveConfig(int level)
        {
            SkillLevelData levelData = GetLevelData(level);
            return levelData != null && levelData.UsePassiveOverride && levelData.PassiveOverride != null
                ? levelData.PassiveOverride
                : PassiveConfig;
        }

        public SkillPhaseData GetPhaseByEvent(int level, string eventName)
        {
            return GetPhaseByEvent(level, SkillPhaseTriggerType.SpineEvent, eventName);
        }

        public SkillPhaseData GetPhaseByEvent(int level, SkillPhaseTriggerType triggerType, string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return null;

            SkillLevelData levelData = GetLevelData(level);
            if (levelData == null || levelData.Phases == null)
                return null;

            for (int i = 0; i < levelData.Phases.Count; i++)
            {
                SkillPhaseData phase = levelData.Phases[i];
                if (phase == null || phase.TriggerType != triggerType)
                    continue;

                if (string.Equals(phase.EventName, eventName, StringComparison.Ordinal))
                    return phase;
            }

            return null;
        }

        public void GetPhasesByEvent(int level, SkillPhaseTriggerType triggerType, string eventName, List<SkillPhaseData> results)
        {
            results?.Clear();
            if (results == null || string.IsNullOrEmpty(eventName))
                return;

            SkillLevelData levelData = GetLevelData(level);
            if (levelData == null || levelData.Phases == null)
                return;

            for (int i = 0; i < levelData.Phases.Count; i++)
            {
                SkillPhaseData phase = levelData.Phases[i];
                if (phase == null || phase.TriggerType != triggerType)
                    continue;

                if (string.Equals(phase.EventName, eventName, StringComparison.Ordinal))
                    results.Add(phase);
            }
        }

        public int GetRequiredShardForLevel(int currentLevel)
        {
            int safeLevel = GetSafeLevel(currentLevel);
            if (IsMaxLevel(safeLevel))
                return 0;

            if (UpgradeShardCosts != null)
            {
                for (int i = 0; i < UpgradeShardCosts.Count; i++)
                {
                    SkillUpgradeCostEntry entry = UpgradeShardCosts[i];
                    if (entry != null && entry.Level == safeLevel)
                        return Mathf.Max(1, entry.RequiredShard);
                }
            }

            return 2;
        }

        public int GetNextLevelNumber()
        {
            int max = 0;
            if (Levels != null)
            {
                for (int i = 0; i < Levels.Count; i++)
                {
                    if (Levels[i] != null)
                        max = Mathf.Max(max, Levels[i].Level);
                }
            }

            return Mathf.Max(1, max + 1);
        }

        public SkillLevelData GetFirstLevelData()
        {
            if (Levels == null || Levels.Count == 0)
                return null;

            SkillLevelData best = null;
            for (int i = 0; i < Levels.Count; i++)
            {
                SkillLevelData level = Levels[i];
                if (level == null)
                    continue;

                if (best == null || level.Level < best.Level)
                    best = level;
            }

            return best;
        }

        public SkillLevelData GetLastLevelData()
        {
            if (Levels == null || Levels.Count == 0)
                return null;

            SkillLevelData best = null;
            for (int i = 0; i < Levels.Count; i++)
            {
                SkillLevelData level = Levels[i];
                if (level == null)
                    continue;

                if (best == null || level.Level > best.Level)
                    best = level;
            }

            return best;
        }

        public SkillLevelData AddLevelCopyFromFirstLevel()
        {
            SkillLevelData source = GetFirstLevelData();
            SkillLevelData newLevel = source != null
                ? CloneLevel(source, GetNextLevelNumber())
                : CreateLevelFromDefaultConfigs(GetNextLevelNumber(), false);

            Levels ??= new List<SkillLevelData>();
            Levels.Add(newLevel);
            MaxLevel = Mathf.Max(MaxLevel, newLevel.Level);
            return newLevel;
        }

        public SkillLevelData AddLevelCopyFromLastLevel()
        {
            SkillLevelData source = GetLastLevelData();
            SkillLevelData newLevel = source != null
                ? CloneLevel(source, GetNextLevelNumber())
                : CreateLevelFromDefaultConfigs(GetNextLevelNumber(), false);

            Levels ??= new List<SkillLevelData>();
            Levels.Add(newLevel);
            MaxLevel = Mathf.Max(MaxLevel, newLevel.Level);
            return newLevel;
        }

        public SkillLevelData AddLevelFromDefaultConfigs(bool enableOverrides)
        {
            SkillLevelData newLevel = CreateLevelFromDefaultConfigs(GetNextLevelNumber(), enableOverrides);
            Levels ??= new List<SkillLevelData>();
            Levels.Add(newLevel);
            MaxLevel = Mathf.Max(MaxLevel, newLevel.Level);
            return newLevel;
        }

        public void CopyRootConfigsToAllLevelOverrides(bool enableOverrides)
        {
            if (Levels == null)
                return;

            for (int i = 0; i < Levels.Count; i++)
            {
                SkillLevelData level = Levels[i];
                if (level == null)
                    continue;

                level.UseCastOverride = enableOverrides;
                level.CastOverride = CloneCastConfig(CastConfig);
                level.UseRuntimeObjectOverride = enableOverrides;
                level.RuntimeObjectOverride = CloneRuntimeObjectConfig(RuntimeObjectConfig);
                level.UsePassiveOverride = enableOverrides;
                level.PassiveOverride = ClonePassiveConfig(PassiveConfig);
            }
        }

        public void DisableAllConfigOverrides()
        {
            if (Levels == null)
                return;

            for (int i = 0; i < Levels.Count; i++)
            {
                SkillLevelData level = Levels[i];
                if (level == null)
                    continue;

                level.UseCastOverride = false;
                level.UseRuntimeObjectOverride = false;
                level.UsePassiveOverride = false;
            }
        }

        public void CopyFirstLevelPhasesToAllLevels(bool keepLevelNumber = true)
        {
            SkillLevelData first = GetFirstLevelData();
            if (first == null || Levels == null)
                return;

            for (int i = 0; i < Levels.Count; i++)
            {
                SkillLevelData level = Levels[i];
                if (level == null || ReferenceEquals(level, first))
                    continue;

                int targetLevel = keepLevelNumber ? level.Level : first.Level;
                SkillLevelData cloned = CloneLevel(first, targetLevel);

                level.UseCastOverride = cloned.UseCastOverride;
                level.CastOverride = cloned.CastOverride;
                level.UseRuntimeObjectOverride = cloned.UseRuntimeObjectOverride;
                level.RuntimeObjectOverride = cloned.RuntimeObjectOverride;
                level.UsePassiveOverride = cloned.UsePassiveOverride;
                level.PassiveOverride = cloned.PassiveOverride;
                level.Phases = cloned.Phases;
                level.DescriptionParams = cloned.DescriptionParams;
            }
        }

        [ContextMenu("Skill/Add Next Level - Copy From Last Level")]
        private void ContextAddLevelCopyFromLastLevel()
        {
            AddLevelCopyFromLastLevel();
        }

        [ContextMenu("Skill/Add Next Level - Copy From First Level")]
        private void ContextAddLevelCopyFromFirstLevel()
        {
            AddLevelCopyFromFirstLevel();
        }

        [ContextMenu("Skill/Add Next Level - From Root Configs")]
        private void ContextAddLevelFromDefaultConfigs()
        {
            AddLevelFromDefaultConfigs(false);
        }

        [ContextMenu("Skill/Disable All Level Config Overrides")]
        private void ContextDisableAllConfigOverrides()
        {
            DisableAllConfigOverrides();
        }

        private SkillLevelData CreateLevelFromDefaultConfigs(int level, bool enableOverrides)
        {
            return new SkillLevelData
            {
                Level = Mathf.Max(1, level),
                UseCastOverride = enableOverrides,
                CastOverride = CloneCastConfig(CastConfig),
                UseRuntimeObjectOverride = enableOverrides,
                RuntimeObjectOverride = CloneRuntimeObjectConfig(RuntimeObjectConfig),
                UsePassiveOverride = enableOverrides,
                PassiveOverride = ClonePassiveConfig(PassiveConfig),
                Phases = new List<SkillPhaseData>(),
                DescriptionParams = new List<SkillDescriptionParam>()
            };
        }

        private static SkillLevelData CloneLevel(SkillLevelData source, int newLevel)
        {
            if (source == null)
                return new SkillLevelData { Level = Mathf.Max(1, newLevel) };

            return new SkillLevelData
            {
                Level = Mathf.Max(1, newLevel),
                UseCastOverride = source.UseCastOverride,
                CastOverride = CloneCastConfig(source.CastOverride),
                UseRuntimeObjectOverride = source.UseRuntimeObjectOverride,
                RuntimeObjectOverride = CloneRuntimeObjectConfig(source.RuntimeObjectOverride),
                UsePassiveOverride = source.UsePassiveOverride,
                PassiveOverride = ClonePassiveConfig(source.PassiveOverride),
                Phases = ClonePhases(source.Phases),
                DescriptionParams = CloneDescriptionParams(source.DescriptionParams)
            };
        }

        private static SkillCastConfig CloneCastConfig(SkillCastConfig source)
        {
            if (source == null)
                return new SkillCastConfig();

            return new SkillCastConfig
            {
                RequireTarget = source.RequireTarget,
                MoveToCastRange = source.MoveToCastRange,
                CastRange = source.CastRange,
                Cooldown = source.Cooldown,
                TargetSelectType = source.TargetSelectType,
                HeroAnimationName = source.HeroAnimationName
            };
        }

        private static SkillRuntimeObjectConfig CloneRuntimeObjectConfig(SkillRuntimeObjectConfig source)
        {
            if (source == null)
                return new SkillRuntimeObjectConfig();

            return new SkillRuntimeObjectConfig
            {
                RuntimeVisualType = source.RuntimeVisualType,
                SpineRuntimePrefab = source.SpineRuntimePrefab,
                SpawnPositionType = source.SpawnPositionType,
                FollowType = source.FollowType,
                SpawnOffset = source.SpawnOffset,
                LifeTime = source.LifeTime,
                UseLifeTime = source.UseLifeTime,
                DespawnOnAnimationComplete = source.DespawnOnAnimationComplete,
                AnimationName = source.AnimationName,
                LoopAnimation = source.LoopAnimation,
                LockCasterWhileAlive = source.LockCasterWhileAlive
            };
        }

        private static SkillPassiveConfig ClonePassiveConfig(SkillPassiveConfig source)
        {
            SkillPassiveConfig clone = new SkillPassiveConfig();
            if (source == null)
                return clone;

            clone.StackKey = source.StackKey;
            clone.RequiredStack = source.RequiredStack;
            clone.MaxStack = source.MaxStack;
            clone.ResetStackOnTrigger = source.ResetStackOnTrigger;
            clone.ConsumeStackOnTrigger = source.ConsumeStackOnTrigger;
            clone.StackGainTriggers = new List<SkillTriggerStackGainData>();

            if (source.StackGainTriggers != null)
            {
                for (int i = 0; i < source.StackGainTriggers.Count; i++)
                {
                    SkillTriggerStackGainData trigger = source.StackGainTriggers[i];
                    if (trigger == null)
                        continue;

                    clone.StackGainTriggers.Add(new SkillTriggerStackGainData
                    {
                        EventType = trigger.EventType,
                        SourceFilter = trigger.SourceFilter,
                        StackGainAmount = trigger.StackGainAmount
                    });
                }
            }

            clone.EnemyCountCondition = source.EnemyCountCondition != null
                ? new SkillEnemyCountConditionData
                {
                    Enabled = source.EnemyCountCondition.Enabled,
                    MinEnemyCount = source.EnemyCountCondition.MinEnemyCount,
                    Range = source.EnemyCountCondition.Range
                }
                : new SkillEnemyCountConditionData();

            return clone;
        }

        private static List<SkillPhaseData> ClonePhases(List<SkillPhaseData> source)
        {
            List<SkillPhaseData> result = new List<SkillPhaseData>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(ClonePhase(source[i]));

            return result;
        }

        private static SkillPhaseData ClonePhase(SkillPhaseData source)
        {
            if (source == null)
                return new SkillPhaseData();

            return new SkillPhaseData
            {
                PhaseId = source.PhaseId,
                TriggerType = source.TriggerType,
                EventName = source.EventName,
                Delay = source.Delay,
                NormalizedTime = source.NormalizedTime,
                Actions = CloneActions(source.Actions)
            };
        }

        private static List<SkillActionData> CloneActions(List<SkillActionData> source)
        {
            List<SkillActionData> result = new List<SkillActionData>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(CloneAction(source[i]));

            return result;
        }

        private static SkillActionData CloneAction(SkillActionData source)
        {
            if (source == null)
                return new SkillActionData();

            return new SkillActionData
            {
                ActionType = source.ActionType,
                ChancePercent = source.ChancePercent,
                TargetTypeOverride = source.TargetTypeOverride,
                Damage = CloneDamageData(source.Damage),
                Dot = CloneDotData(source.Dot),
                StatModifier = CloneStatModifierData(source.StatModifier),
                Area = CloneAreaData(source.Area),
                Projectile = CloneProjectileData(source.Projectile),
                Stack = CloneStackActionData(source.Stack),
                TriggerSkill = source.TriggerSkill
            };
        }

        private static SkillDamageData CloneDamageData(SkillDamageData source)
        {
            if (source == null)
                return new SkillDamageData();

            return new SkillDamageData
            {
                SkillDamageBonusPercent = source.SkillDamageBonusPercent,
                CountAsSkillDamage = source.CountAsSkillDamage
            };
        }

        private static SkillDotData CloneDotData(SkillDotData source)
        {
            if (source == null)
                return new SkillDotData();

            return new SkillDotData
            {
                EffectId = source.EffectId,
                TickDamageBonusPercent = source.TickDamageBonusPercent,
                TickInterval = source.TickInterval,
                Duration = source.Duration,
                DamageType = source.DamageType,
                StackRule = source.StackRule
            };
        }

        private static SkillStatModifierData CloneStatModifierData(SkillStatModifierData source)
        {
            if (source == null)
                return new SkillStatModifierData();

            return new SkillStatModifierData
            {
                ModifierId = source.ModifierId,
                StatType = source.StatType,
                PercentValue = source.PercentValue,
                Duration = source.Duration
            };
        }

        private static SkillAreaData CloneAreaData(SkillAreaData source)
        {
            if (source == null)
                return new SkillAreaData();

            return new SkillAreaData
            {
                AreaPrefab = source.AreaPrefab,
                Shape = source.Shape,
                PositionType = source.PositionType,
                Radius = source.Radius,
                BoxSize = source.BoxSize,
                Duration = source.Duration,
                TickInterval = source.TickInterval,
                HitOncePerTarget = source.HitOncePerTarget,
                OnHitActions = CloneActions(source.OnHitActions)
            };
        }

        private static SkillProjectileData CloneProjectileData(SkillProjectileData source)
        {
            if (source == null)
                return new SkillProjectileData();

            return new SkillProjectileData
            {
                ProjectilePrefab = source.ProjectilePrefab,
                MoveType = source.MoveType,
                HitDetectionType = source.HitDetectionType,
                SpawnPositionType = source.SpawnPositionType,
                FollowType = source.FollowType,
                CustomSocketName = source.CustomSocketName,
                Count = source.Count,
                SpreadAngle = source.SpreadAngle,
                DelayBetweenShots = source.DelayBetweenShots,
                Speed = source.Speed,
                LifeTime = source.LifeTime,
                HitRadius = source.HitRadius,
                Pierce = source.Pierce,
                PierceCount = source.PierceCount,
                OnHitActions = CloneActions(source.OnHitActions)
            };
        }

        private static SkillStackActionData CloneStackActionData(SkillStackActionData source)
        {
            if (source == null)
                return new SkillStackActionData();

            return new SkillStackActionData
            {
                StackKey = source.StackKey,
                Amount = source.Amount
            };
        }

        private static List<SkillDescriptionParam> CloneDescriptionParams(List<SkillDescriptionParam> source)
        {
            List<SkillDescriptionParam> result = new List<SkillDescriptionParam>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                SkillDescriptionParam param = source[i];
                if (param == null)
                    continue;

                result.Add(new SkillDescriptionParam
                {
                    Key = param.Key,
                    Value = param.Value,
                    IsPercent = param.IsPercent,
                    DecimalPlaces = param.DecimalPlaces
                });
            }

            return result;
        }
    }
}
