using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.SkillSystem.Description;
using Immortal_Switch.Scripts.Sound;
using Immortal_Switch.Scripts.StatSystem;
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
        [ShowIf("MoveToCastRange")]
        [Min(0f)] public float CastRange = 2f;
        [Min(0f)] public float Cooldown = 1f;
        public SkillTargetSelectType TargetSelectType = SkillTargetSelectType.NearestEnemy;

        [FormerlySerializedAs("AnimationName")]
        public string HeroAnimationName;
    }
    
    public enum SkillRuntimeSpawnMode
    {
        /// <summary>
        /// Addressable instance độc lập.
        /// Dùng cho controller, spawner hoặc runtime object ít được tạo.
        /// </summary>
        AddressableInstance,

        /// <summary>
        /// Addressable pooled instance.
        /// Dùng cho Spine VFX, projectile, ground effect...
        /// </summary>
        AddressablePool
    }
    
    [Serializable]
    public class SkillMultiSpawnConfig
    {
        [Header("Child Runtime Object")]
        public SkillRuntimeSpawnMode ChildSpawnMode =
            SkillRuntimeSpawnMode.AddressablePool;

        [Tooltip("Full Addressable key của child runtime prefab.")]
        public string ChildRuntimeAddressableKey;

        public string ChildAnimationName;
        public bool ChildLoopAnimation;

        [Header("Child Lifetime")]
        public bool ChildUseLifeTime = true;

        [Min(0f)]
        public float ChildLifeTime = 1.5f;

        public bool ChildDespawnOnAnimationComplete = true;

        [Header("Spawn Pattern")]
        [Min(1)]
        public int SpawnCount = 10;

        [Min(0f)]
        public float StartDelay;

        [Min(0f)]
        public float SpawnInterval = 0.15f;

        [Min(0f)]
        public float SpawnRadius = 2.5f;

        public bool RandomInsideCircle = true;
        public bool IncludeCenterAsFirstSpawn = true;

        [Header("Position")]
        public Vector3 ChildSpawnOffset;
        public bool RandomizeYRotation;

        [Header("Controller Lifetime")]
        public bool DespawnControllerAfterSpawn = true;

        [Min(0f)]
        public float DespawnDelayAfterLastSpawn = 0.25f;

        [Header("Debug")]
        public bool DebugDrawSpawnRadius;
    }

    [Serializable]
    public class SkillRuntimeObjectConfig
    {
        public SkillRuntimeVisualType RuntimeVisualType = SkillRuntimeVisualType.SpawnedSkillObject;
        
        [Header("Spawned Skill Object")]
        [ShowIf(nameof(IsUsingSkillRuntimePrefab))]
        public SkillRuntimeSpawnMode SpawnMode =
            SkillRuntimeSpawnMode.AddressablePool;

        [ShowIf(nameof(IsUsingSkillRuntimePrefab))]
        [Tooltip("Full Addressable key của SkillRuntimeObject prefab.")]
        public string RuntimeAddressableKey;
        
        public SoundSetting soundDefinition;
        public SkillSpawnPositionType SpawnPositionType = SkillSpawnPositionType.Self;
        public SkillFollowType FollowType = SkillFollowType.None;
        public Vector3 SpawnOffset;

        [Header("Lifetime")]
        [Min(0f)] public float LifeTime = 1f;
        public bool UseLifeTime = true;
        
        [ShowIf( nameof(IsUsingSkillRuntimePrefab))]
        public bool DespawnOnAnimationComplete = true;

        [ShowIf( "IsUsingSkillRuntimePrefab")]
        [Header("Animation / Visual")]
        public string AnimationName;
        
        [ShowIf( "IsUsingSkillRuntimePrefab")]
        public bool LoopAnimation;

        [Header("Caster Lock")]
        public bool LockCasterWhileAlive;
        
        public bool LockCasterDuringHeroAnimation = true;
        
        [Header("Multi Spawn")]
        [Tooltip("Chỉ sử dụng khi SkillRuntimePrefab kế thừa SkillMultiSpawnRuntimeObject.")]
        public SkillMultiSpawnConfig MultiSpawnConfig = new();

        public static string BulletPatternSpawnerKey = "bullet_spawner_skill_runtime_object";
        public static string HomingBulletSpawnerKey = "homing_chain_bullet_skill_runtime_object";

        private bool IsUsingSkillRuntimePrefab()
        {
            return RuntimeVisualType == SkillRuntimeVisualType.SpawnedSkillObject ||
                   RuntimeVisualType == SkillRuntimeVisualType.SpawnProjectilePatternBehavior ||
                   RuntimeVisualType == SkillRuntimeVisualType.SpawnHomingProjectile ||
                   RuntimeVisualType == SkillRuntimeVisualType.HeroSpineAndSpawnedSkillObject ||
                   RuntimeVisualType == SkillRuntimeVisualType.HeroSpineObjectAndProjectile ||
                   RuntimeVisualType == SkillRuntimeVisualType.HeroSpineObjectAndHomingProjectile;
        }
    }
    
    [Serializable]
    public class SkillPassiveLevelConfig
    {
        [Tooltip(
            "Override: xóa modifier từ level trước.\n" +
            "Additive: giữ modifier level trước và cộng thêm modifier hiện tại.")]
        public PassiveLevelMergeMode MergeMode =
            PassiveLevelMergeMode.Override;

        public List<StatModifier> Modifiers = new();
    }

    [Serializable]
    public class SkillTriggerStackGainData
    {
        public SkillTriggerEventType EventType = SkillTriggerEventType.OnHit;

        public SkillEventSourceFilter SourceFilter =
            SkillEventSourceFilter.Owner;

        [Tooltip(
            "Any: nhận mọi loại hit.\n" +
            "BasicAttackOnly: chỉ nhận hit không có SkillDataSO.\n" +
            "SkillOnly: chỉ nhận hit có SkillDataSO.")]
        public PassiveHitSourceFilter HitSourceFilter =
            PassiveHitSourceFilter.Any;

        [Min(1)]
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
    public struct SoundSetting
    {
        public SoundId startSound;
        public SoundId hitSound;
        public SoundId finalHitSound;
        public bool loopStartSoundUntilSkillEnd;
    }

    [Serializable]
    public class SkillPassiveConfig
    {
        [Header("Stack")]
        public string StackKey;

        [Min(1)]
        public int RequiredStack = 3;

        [Min(1)]
        public int MaxStack = 3;

        public bool ResetStackOnTrigger = true;
        public bool ConsumeStackOnTrigger = true;

        [Tooltip("Trong cooldown passive sẽ không nhận thêm stack.")]
        public bool BlockStackGainDuringCooldown = true;

        public List<SkillTriggerStackGainData> StackGainTriggers = new();

        [Header("Activation Condition")]
        public SkillEnemyCountConditionData EnemyCountCondition = new();

        [Header("Buff")]
        [Min(0f)]
        public float BuffDuration = 4f;

        public List<StatModifier> BaseModifiers = new();

        [Header("Spine Animation")]
        [Tooltip("Animation aura luôn chạy khi hero có passive.")]
        public string PassiveAuraAnimation = "passive";

        [Min(0)]
        [Tooltip("Nên dùng track riêng để aura không ghi đè idle, attack và skill.")]
        public int PassiveAuraTrackIndex = 2;

        [Tooltip("Có chạy animation khi passive được kích hoạt hay không.")]
        public bool PlayTriggerAnimation = true;

        public string TriggerAnimation = "passive_cast";
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
        [Min(1)]
        public int Level = 1;

        public List<SkillPhaseData> Phases = new();
        [TextArea(3,12)]
        public String Description;

        [ShowIf("@$root.OwnerType == SkillOwnerType.PassiveSkill")]
        public SkillPassiveLevelConfig PassiveLevelConfig;
    }

    [Serializable]
    public class SkillPhaseData
    {
        public int PhaseId;
        public SkillPhaseTriggerType TriggerType = SkillPhaseTriggerType.RuntimeObjectSpineEvent;

        [Tooltip("Generic event name. For Spine objects, this is the Spine event name, e.g. hit/shoot/finalhit.")]
        public string EventName = "hit";

        [Min(0f)] public float Delay;
        [Range(0f, 1f)] public float NormalizedTime;
        public List<SkillActionData> Actions = new();
        public bool hasCameraShake;
    }

    [Serializable]
    public class SkillUpgradeCostEntry
    {
        [Min(1)] public int Level = 1;
        [Min(1)] public int RequiredShard = 2;
    }
    
    [Serializable]
    public class ClassSkillLevelScalingConfig
    {
        [Tooltip("Mỗi level sau level 1 tăng thêm bao nhiêu % dựa trên giá trị base.")]
        [Min(0f)]
        public float GrowthPercentPerLevel = 6f;

        public float GetMultiplier(int currentLevel)
        {
            int additionalLevel = Mathf.Max(0, currentLevel - 1);
            return 1f + GrowthPercentPerLevel / 100f * additionalLevel;
        }

        public float Scale(float baseValue, int currentLevel)
        {
            return baseValue * GetMultiplier(currentLevel);
        }
    }
    
    public sealed class ResolvedPassiveConfig
    {
        public SkillPassiveConfig BaseConfig;
        public List<StatModifier> Modifiers = new();
    }

    [CreateAssetMenu(fileName = "Skill_", menuName = "ScriptableObjects/SkillDataSO")]
    public class SkillDataSO : ScriptableObject
    {
        [Header("Identity")]
        public int SkillId;
        public int HeroId;
        public string SkillKey;
        public HeroClass SkillClass;
        public string SkillName;
        public string IconSkillKey;
        [TextArea(3,12)]
        public String Description;

        [Header("Type")]
        public SkillOwnerType OwnerType = SkillOwnerType.ClassSkill;
        [Min(1)] public int MaxLevel = 1;

        [ShowIf("OwnerType", SkillOwnerType.ClassSkill)]
        [Header("Summon / Rarity")]
        public TierSkill SkillTier;

        [Header("Default Config")]
        public SkillCastConfig CastConfig = new();
        public SkillRuntimeObjectConfig RuntimeObjectConfig = new();
        public List<SkillPhaseData> BasePhases;
        public ClassSkillLevelScalingConfig ClassSkillScaling = new();
        [ShowIf("OwnerType", SkillOwnerType.PassiveSkill)]
        public SkillPassiveConfig PassiveConfig = new();

        [Header("Custom Behaviour")]
        public SkillBehaviour CustomBehaviourPrefab;

        [Header("Upgrade")]
        public List<SkillUpgradeCostEntry> UpgradeShardCosts = new();

        [Header("Levels")]
        public List<SkillLevelData> Levels = new();
        
        [SerializeField]
        private Color hitDamageColor = new Color(1f, 0.25f, 0.25f);

        public int GetSafeLevel(int level)
        {
            return Mathf.Clamp(level, 1, Mathf.Max(1, MaxLevel));
        }

        public bool IsMaxLevel(int level)
        {
            return GetSafeLevel(level) >= Mathf.Max(1, MaxLevel);
        }

        public string BuildDescription(int level)
        {
            switch (RuntimeObjectConfig.RuntimeVisualType)
            {
                case SkillRuntimeVisualType.SpawnedSkillObject:
                case SkillRuntimeVisualType.HeroSpineAndSpawnedSkillObject:
                    return SpineSkillDescriptionBuilder.Build(this, level, hitDamageColor, hitDamageColor);
            }

            return null;
        }
        
        public SoundId[] GetAllNeedSound()
        {
            return new []{RuntimeObjectConfig.soundDefinition.startSound, RuntimeObjectConfig.soundDefinition.hitSound, RuntimeObjectConfig.soundDefinition.finalHitSound};
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
            return CastConfig;
        }

        public SkillRuntimeObjectConfig GetRuntimeObjectConfig(int level)
        {
            SkillLevelData levelData = GetLevelData(level);
            return RuntimeObjectConfig;
        }

        public SkillPassiveConfig GetPassiveConfig(int level)
        {
            SkillLevelData levelData = GetLevelData(level);
            return PassiveConfig;
        }
        
        public ResolvedPassiveConfig GetResolvedPassiveConfig(int level)
        {
            if (PassiveConfig == null)
                return null;

            int safeLevel = GetSafeLevel(level);

            ResolvedPassiveConfig result = new ResolvedPassiveConfig
            {
                BaseConfig = PassiveConfig,
                Modifiers = new List<StatModifier>()
            };

            AddClonedModifiers(
                result.Modifiers,
                PassiveConfig.BaseModifiers);

            if (Levels == null || Levels.Count == 0)
                return result;

            // Resolve lần lượt từ level 1 đến current level.
            // Không phụ thuộc thứ tự phần tử trong Inspector.
            for (int currentLevel = 1;
                 currentLevel <= safeLevel;
                 currentLevel++)
            {
                SkillLevelData levelData =
                    FindExactLevelData(currentLevel);

                SkillPassiveLevelConfig passiveLevelConfig =
                    levelData?.PassiveLevelConfig;

                if (passiveLevelConfig == null)
                    continue;

                if (passiveLevelConfig.MergeMode ==
                    PassiveLevelMergeMode.Override)
                {
                    result.Modifiers.Clear();
                }

                AddClonedModifiers(
                    result.Modifiers,
                    passiveLevelConfig.Modifiers);
            }

            return result;
        }

        private SkillLevelData FindExactLevelData(int level)
        {
            if (Levels == null)
                return null;

            for (int i = 0; i < Levels.Count; i++)
            {
                SkillLevelData levelData = Levels[i];

                if (levelData != null &&
                    levelData.Level == level)
                {
                    return levelData;
                }
            }

            return null;
        }

        private static void AddClonedModifiers(
            List<StatModifier> destination,
            List<StatModifier> source)
        {
            if (destination == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                StatModifier modifier = source[i];

                if (modifier == null)
                    continue;

                destination.Add(modifier.Clone());
            }
        }

        public SkillPhaseData GetPhaseByEvent(int level, string eventName)
        {
            return GetPhaseByEvent(level, SkillPhaseTriggerType.SpineEvent, eventName);
        }

        public SkillPhaseData GetPhaseByEvent(
            int level,
            SkillPhaseTriggerType triggerType,
            string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return null;

            IReadOnlyList<SkillPhaseData> phases = GetPhases(level);
            if (phases == null)
                return null;

            for (int i = 0; i < phases.Count; i++)
            {
                SkillPhaseData phase = phases[i];

                if (phase == null || phase.TriggerType != triggerType)
                    continue;

                if (string.Equals(
                        phase.EventName,
                        eventName,
                        StringComparison.Ordinal))
                {
                    return phase;
                }
            }

            return null;
        }

        public void GetPhasesByEvent(
            int level,
            SkillPhaseTriggerType triggerType,
            string eventName,
            List<SkillPhaseData> results)
        {
            results?.Clear();

            if (results == null || string.IsNullOrEmpty(eventName))
                return;

            IReadOnlyList<SkillPhaseData> phases = GetPhases(level);
            if (phases == null)
                return;

            for (int i = 0; i < phases.Count; i++)
            {
                SkillPhaseData phase = phases[i];

                if (phase == null || phase.TriggerType != triggerType)
                    continue;

                if (string.Equals(
                        phase.EventName,
                        eventName,
                        StringComparison.Ordinal))
                {
                    results.Add(phase);
                }
            }
        }
        
        public IReadOnlyList<SkillPhaseData> GetPhases(int level)
        {
            // Class skill có nhiều level:
            // luôn dùng BasePhases và scale giá trị bằng công thức runtime.
            if (OwnerType == SkillOwnerType.ClassSkill)
                return BasePhases;

            SkillLevelData levelData = GetLevelData(level);
            return levelData.Phases;
        }
        
        public bool UsesAutomaticClassSkillScaling =>
            OwnerType == SkillOwnerType.ClassSkill;

        public float GetScaledClassSkillValue(
            float baseValue,
            int currentLevel,
            bool scaleWithLevel)
        {
            if (!scaleWithLevel)
                return baseValue;

            if (!UsesAutomaticClassSkillScaling)
                return baseValue;

            if (ClassSkillScaling == null)
                return baseValue;

            return ClassSkillScaling.Scale(baseValue, currentLevel);
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
    }
}
