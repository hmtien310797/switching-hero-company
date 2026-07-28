using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.SkillSystem.Description;
using Immortal_Switch.Scripts.Sound;
using Immortal_Switch.Scripts.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

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

    /// <summary>
    /// Giá trị mô tả (ValueKey) cho riêng một level của Ultimate/Passive skill.
    /// Mỗi entry lưu <see cref="Level"/> (1-3) và mảng <see cref="Values"/>
    /// dùng để format localized description template tại level đó.
    /// </summary>
    [Serializable]
    public class SkillDescriptionLevelValues
    {
        [Range(1, 3)]
        public int Level = 1;

        public float[] Values;
    }
    
    public class ClassSkillDescriptionLevelValues
    {
        public float Values;
        public bool ScaleWithSkillLevel;
    }

    [Serializable]
    public class SkillLevelData
    {
        [Min(1)]
        public int Level = 1;

        public List<SkillPhaseData> Phases = new();
        
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
        public string SkillNameKey;
        public string IconSkillKey;
        public String DescriptionKey;

        /// <summary>
        /// Legacy field (từ version cũ khi Ultimate/Passive dùng chung một
        /// <c>float[] ValueKey</c> cho mọi level). Giữ lại hidden để migrate
        /// dữ liệu cũ sang <see cref="DescriptionValuesByLevel"/>.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("ValueKey")]
        private float[] legacyValueKey;

        /// <summary>
        /// Giá trị mô tả theo từng level (1-3) cho Ultimate/Passive skill.
        /// Chỉ hiển thị với skill đặc biệt (không phải Class skill).
        /// </summary>
        [ShowIf(nameof(IsSpecialSkill))]
        public SkillDescriptionLevelValues[] DescriptionValuesByLevel;
        
        [ShowIf(nameof(IsSpecialSkill), false)]
        public ClassSkillDescriptionLevelValues[] classSkillDescriptionLevelValuesByLevel;

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

        public int GetSafeLevel(int level)
        {
            return Mathf.Clamp(level, 1, Mathf.Max(1, MaxLevel));
        }

        public bool IsMaxLevel(int level)
        {
            return GetSafeLevel(level) >= Mathf.Max(1, MaxLevel);
        }

        public bool IsNotClassSkill()
        {
            return OwnerType != SkillOwnerType.ClassSkill;
        }

        /// <summary>
        /// Skill đặc biệt (Ultimate hoặc Passive) — có cấu trúc description 3 level
        /// (<see cref="DescriptionValuesByLevel"/>) và dùng localized template.
        /// </summary>
        public bool IsSpecialSkill()
        {
            return OwnerType == SkillOwnerType.UltimateSkill ||
                   OwnerType == SkillOwnerType.PassiveSkill;
        }

        public string BuildDescription(int level)
        {
            return SpineSkillDescriptionBuilder.Build(this, level, classSkillDescriptionLevelValuesByLevel);
        }

        /// <summary>
        /// Lấy localized skill name. Fallback theo thứ tự:
        /// 1. <see cref="SkillNameKey"/> localized value.
        /// 2. <see cref="SkillName"/>.
        /// 3. <see cref="SkillKey"/>.
        /// 4. <see cref="string.Empty"/>.
        /// Không trả về null.
        /// </summary>
        public string GetLocalizedSkillName()
        {
            if (!string.IsNullOrWhiteSpace(SkillNameKey) &&
                LocalizationManager.TryGetRawText(SkillNameKey, out string localized) &&
                !string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            if (!string.IsNullOrWhiteSpace(SkillKey))
                return SkillKey;

            return string.Empty;
        }

        /// <summary>
        /// Lấy raw localized description template (giữ nguyên <c>{0}</c>, <c>{1}</c>...).
        /// Fallback về <see cref="Description"/> nếu key rỗng, entry không tồn tại
        /// hoặc localization chưa sẵn sàng. Không trả về null.
        /// </summary>
        public string GetLocalizedDescriptionTemplate()
        {
            if (!string.IsNullOrWhiteSpace(DescriptionKey) &&
                LocalizationManager.TryGetRawText(DescriptionKey, out string localized) &&
                !string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            return string.Empty;
        }

        /// <summary>
        /// Giá trị mặc định khi khởi tạo thêm một phần tử Values cho level.
        /// Level 1 → 0, Level 2 → 1, Level 3 → 2 (dễ nhận biết = level - 1).
        /// Không dùng công thức tổng quát để dễ điều chỉnh từng level riêng biệt.
        /// </summary>
        public static float GetDefaultDescriptionValue(int level)
        {
            return level switch
            {
                1 => 0f,
                2 => 1f,
                3 => 2f,
                _ => 0f
            };
        }

        /// <summary>
        /// Đảm bảo <see cref="DescriptionValuesByLevel"/> có đủ entry cho level 1, 2, 3,
        /// sắp xếp theo thứ tự level, giữ nguyên giá trị designer đã nhập.
        /// Xử lý entry null, level trùng (giữ entry đầu tiên), level ngoài 1-3.
        /// Trả về true nếu cấu trúc thay đổi.
        /// </summary>
        public bool EnsureDescriptionLevelStructure()
        {
            bool changed = false;

            List<SkillDescriptionLevelValues> list =
                DescriptionValuesByLevel == null
                    ? new List<SkillDescriptionLevelValues>()
                    : new List<SkillDescriptionLevelValues>(DescriptionValuesByLevel);

            bool hasValidEntry = false;
            for (int i = 0; i < list.Count; i++)
            {
                SkillDescriptionLevelValues entry = list[i];
                if (entry != null && entry.Level >= 1 && entry.Level <= 3)
                {
                    hasValidEntry = true;
                    break;
                }
            }

            // Migration: khi chưa có level hợp lệ nào và có legacy ValueKey
            // chứa data thật (≠ 0) → copy legacy sang cả 3 level (giữ mô tả đang hiển thị).
            // Legacy toàn 0 (artifact do sync cũ) được coi như không có data → bỏ qua,
            // để tạo entry mới với default per-level (0/1/2).
            if (!hasValidEntry && LegacyHasMeaningfulData())
            {
                list.Clear();

                for (int lvl = 1; lvl <= 3; lvl++)
                {
                    list.Add(new SkillDescriptionLevelValues
                    {
                        Level = lvl,
                        Values = (float[])legacyValueKey.Clone()
                    });
                }

                Debug.Log(
                    $"[SkillDataSO] Migrated legacy ValueKey (length {legacyValueKey.Length}) " +
                    $"to all 3 levels for '{name}'.");

                changed = true;
            }

            // Bỏ entry null và level ngoài 1-3.
            for (int i = list.Count - 1; i >= 0; i--)
            {
                SkillDescriptionLevelValues entry = list[i];

                if (entry == null)
                {
                    list.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (entry.Level < 1 || entry.Level > 3)
                {
                    Debug.LogWarning(
                        $"[SkillDataSO] '{name}' has out-of-range level {entry.Level}; removed.");
                    list.RemoveAt(i);
                    changed = true;
                }
            }

            // Xử lý level trùng: giữ entry đầu tiên (theo thứ tự duyệt xuôi), bỏ phần còn lại.
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!seen.Add(list[i].Level))
                {
                    Debug.LogWarning(
                        $"[SkillDataSO] '{name}' has duplicate level {list[i].Level}; removed.");
                    list.RemoveAt(i);
                    i--;
                    changed = true;
                }
            }

            // Đảm bảo đủ level 1, 2, 3.
            for (int lvl = 1; lvl <= 3; lvl++)
            {
                bool exists = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Level == lvl)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    list.Add(new SkillDescriptionLevelValues { Level = lvl });
                    changed = true;
                }
            }

            // Sắp xếp theo level.
            list.Sort((a, b) => a.Level.CompareTo(b.Level));

            // Đảm bảo Values không null.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Values == null)
                {
                    list[i].Values = Array.Empty<float>();
                    changed = true;
                }
            }

            if (changed)
                DescriptionValuesByLevel = list.ToArray();

            return changed;
        }

        /// <summary>
        /// Legacy <c>ValueKey</c> có data thật (ít nhất 1 phần tử ≠ 0) để migrate.
        /// Toàn 0 (artifact do sync cũ) hoặc rỗng → false.
        /// </summary>
        private bool LegacyHasMeaningfulData()
        {
            if (legacyValueKey == null || legacyValueKey.Length == 0)
                return false;

            for (int i = 0; i < legacyValueKey.Length; i++)
            {
                if (!Mathf.Approximately(legacyValueKey[i], 0f))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Lấy Values (chỉ đọc) cho level, clamp 1-3, tìm theo field
        /// <see cref="SkillDescriptionLevelValues.Level"/>. Không trả về null; empty nếu thiếu.
        /// </summary>
        public IReadOnlyList<float> GetDescriptionValues(int level)
        {
            int safeLevel = Mathf.Clamp(level, 1, 3);

            if (DescriptionValuesByLevel != null)
            {
                for (int i = 0; i < DescriptionValuesByLevel.Length; i++)
                {
                    SkillDescriptionLevelValues entry = DescriptionValuesByLevel[i];

                    if (entry != null && entry.Level == safeLevel && entry.Values != null)
                        return entry.Values;
                }
            }

            return Array.Empty<float>();
        }

        private float[] GetDescriptionValuesArray(int level)
        {
            int safeLevel = Mathf.Clamp(level, 1, 3);

            if (DescriptionValuesByLevel != null)
            {
                for (int i = 0; i < DescriptionValuesByLevel.Length; i++)
                {
                    SkillDescriptionLevelValues entry = DescriptionValuesByLevel[i];

                    if (entry != null && entry.Level == safeLevel && entry.Values != null)
                        return entry.Values;
                }
            }

            return Array.Empty<float>();
        }

        /// <summary>
        /// Luồng hiển thị description dùng cho UI:
        /// - Special skill (Ultimate/Passive): clamp level 1-3, lấy localized template,
        ///   format bằng <see cref="GetDescriptionValues"/> của level đó.
        /// - Class skill: dùng <see cref="BuildDescription"/> (Spine {hit} builder).
        /// Fallback cuối: <see cref="Description"/> raw → <see cref="string.Empty"/>.
        /// Không throw, không trả về null.
        /// </summary>
        public string GetDisplayDescription(int level)
        {
            if (IsSpecialSkill())
            {
                int safeLevel = Mathf.Clamp(level, 1, 3);
                string template = GetLocalizedDescriptionTemplate();

                if (!string.IsNullOrEmpty(template))
                {
                    float[] values = GetDescriptionValuesArray(safeLevel);
                    string formatted = SkillDescriptionFormatUtility.FormatDescription(template, values);

                    if (!string.IsNullOrEmpty(formatted))
                        return formatted;
                }
            }

            string built = BuildDescription(level);

            if (!string.IsNullOrEmpty(built))
                return built;

            return string.Empty;
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

#if UNITY_EDITOR
        public enum SkillValueKeySyncStatus
        {
            Unchanged,
            Updated,
            MissingLocalizationKey
        }

        /// <summary>
        /// Sync <see cref="DescriptionValuesByLevel"/> với số placeholder của localized
        /// description template (chỉ Ultimate/Passive).
        /// - Safe Sync (forceMatch=false): chỉ grow, không xóa data designer.
        /// - Force Match (forceMatch=true): shrink về đúng số placeholder (warn nếu mất data ≠ 0).
        /// Đọc String Table trực tiếp qua editor API (không cần Play Mode).
        /// </summary>
        public SkillValueKeySyncStatus SyncDescriptionValuesWithLocalizedTemplate(bool forceMatch)
        {
            if (!IsSpecialSkill())
            {
                UnityEngine.Debug.Log(
                    $"[SkillDataSO] '{name}' is not a special skill (ultimate/passive); nothing to sync.");

                return SkillValueKeySyncStatus.Unchanged;
            }

            // TryGetEditorLocalizedTemplate đã log lý do cụ thể nếu template rỗng.
            string template = TryGetEditorLocalizedTemplate(DescriptionKey);
            int required = string.IsNullOrEmpty(template)
                ? -1
                : SkillDescriptionFormatUtility.GetRequiredArgumentCount(template);

            bool structureChanged = EnsureDescriptionLevelStructure();
            bool valuesChanged = false;

            if (!string.IsNullOrEmpty(template) && DescriptionValuesByLevel != null)
            {
                foreach (SkillDescriptionLevelValues entry in DescriptionValuesByLevel)
                {
                    if (entry == null)
                        continue;

                    if (ResizeValuesEditor(entry, required, forceMatch))
                        valuesChanged = true;
                }
            }

            bool changed = structureChanged || valuesChanged;

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);

                UnityEngine.Debug.Log(
                    $"[SkillDataSO] Synced '{name}': DescriptionKey='{DescriptionKey}', "
                    + $"placeholders={required}, forceMatch={forceMatch}.");
            }

            if (string.IsNullOrEmpty(template))
                return SkillValueKeySyncStatus.MissingLocalizationKey;

            return changed ? SkillValueKeySyncStatus.Updated : SkillValueKeySyncStatus.Unchanged;
        }

        /// <summary>Safe Sync (chỉ grow) — API công khai tương thích MD section 13.</summary>
        public bool SyncDescriptionValuesWithLocalizedTemplate()
        {
            return SyncDescriptionValuesWithLocalizedTemplate(false)
                == SkillValueKeySyncStatus.Updated;
        }

        [Button("Sync Localized Description Values")]
        public void SyncLocalizedDescriptionValues()
        {
            SyncDescriptionValuesWithLocalizedTemplate(false);
        }

        [Button("Force Match Description Values")]
        public void ForceMatchDescriptionValues()
        {
            SyncDescriptionValuesWithLocalizedTemplate(true);
        }

        /// <summary>
        /// Ghi đè toàn bộ Values của mỗi level bằng default per-level (L1=0, L2=1, L3=2),
        /// độ dài = số placeholder của localized template.
        /// Khác với Sync (chỉ grow, giữ data cũ) — Reinitialize XÓA data hiện tại.
        /// Dùng để fix asset đã bị migrate artifact 0, hoặc setup lại từ đầu.
        /// </summary>
        [Button("Reinitialize Description Values")]
        public SkillValueKeySyncStatus ReinitializeDescriptionValues()
        {
            if (!IsSpecialSkill())
            {
                UnityEngine.Debug.Log(
                    $"[SkillDataSO] '{name}' is not a special skill (ultimate/passive); nothing to reinitialize.");

                return SkillValueKeySyncStatus.Unchanged;
            }

            // TryGetEditorLocalizedTemplate đã log lý do nếu template rỗng.
            string template = TryGetEditorLocalizedTemplate(DescriptionKey);

            if (string.IsNullOrEmpty(template))
                return SkillValueKeySyncStatus.MissingLocalizationKey;

            int required = SkillDescriptionFormatUtility.GetRequiredArgumentCount(template);

            EnsureDescriptionLevelStructure();

            bool changed = false;

            if (DescriptionValuesByLevel != null)
            {
                foreach (SkillDescriptionLevelValues entry in DescriptionValuesByLevel)
                {
                    if (entry == null)
                        continue;

                    int safeLevel = Mathf.Clamp(entry.Level, 1, 3);
                    float[] fresh = new float[required];
                    for (int i = 0; i < required; i++)
                        fresh[i] = GetDefaultDescriptionValue(safeLevel);
                    entry.Values = fresh;
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);

                UnityEngine.Debug.Log(
                    $"[SkillDataSO] Reinitialized '{name}': {required} values/level "
                    + "(L1=0, L2=1, L3=2). Existing values overwritten.");
            }

            return changed ? SkillValueKeySyncStatus.Updated : SkillValueKeySyncStatus.Unchanged;
        }

        private static string TryGetEditorLocalizedTemplate(string descriptionKey)
        {
            if (string.IsNullOrWhiteSpace(descriptionKey))
                return null;

            StringTableCollection collection = FindEditorStringTableCollection();

            if (collection == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[SkillDataSO] String Table Collection '{LocalizationManager.TABLE_NAME}' not found. "
                    + "Open Window > Asset Management > Localization > Tables, "
                    + "or ensure Localization Settings + 'Default' collection exist.");
                return null;
            }

            if (collection.SharedData == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[SkillDataSO] Collection '{LocalizationManager.TABLE_NAME}' has no SharedData.");
                return null;
            }

            StringTable table = PickEditorStringTable(collection);

            if (table == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[SkillDataSO] Collection '{LocalizationManager.TABLE_NAME}' has no locale table.");
                return null;
            }

            StringTableEntry entry = table.GetEntry(descriptionKey);

            if (entry == null || string.IsNullOrEmpty(entry.Value))
            {
                UnityEngine.Debug.LogWarning(
                    $"[SkillDataSO] No entry for DescriptionKey '{descriptionKey}' "
                    + $"in table '{LocalizationManager.TABLE_NAME}'. Check key spelling.");
                return null;
            }

            return entry.Value;
        }

        private static StringTableCollection FindEditorStringTableCollection()
        {
            StringTableCollection collection = LocalizationEditorSettings
                .GetStringTableCollection(LocalizationManager.TABLE_NAME);

            if (collection != null)
                return collection;

            // Fallback: editor cache lạnh → tìm asset trực tiếp qua AssetDatabase.
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StringTableCollection");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                StringTableCollection candidate =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);

                if (candidate != null
                    && candidate.SharedData != null
                    && string.Equals(
                        candidate.TableCollectionName,
                        LocalizationManager.TABLE_NAME,
                        StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static StringTable PickEditorStringTable(StringTableCollection collection)
        {
            StringTable fallback = null;

            foreach (StringTable table in collection.StringTables)
            {
                if (table == null)
                    continue;

                if (fallback == null)
                    fallback = table;

                if (string.Equals(
                        table.LocaleIdentifier.Code,
                        "en",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return table;
                }
            }

            return fallback;
        }

        /// <summary>
        /// Resize mảng Values về đúng số placeholder (required) cho level, giữ data cũ.
        /// - grow: giữ giá trị cũ, pad bằng <see cref="GetDefaultDescriptionValue"/> của level.
        /// - forceMatch=true: shrink về required (warn nếu mất giá trị ≠ 0).
        /// - forceMatch=false: không shrink (Safe Sync).
        /// Trả về true nếu mảng thay đổi. Pure function — test độc lập được.
        /// </summary>
        public static bool ResizeDescriptionValues(
            ref float[] values,
            int required,
            int level,
            bool forceMatch)
        {
            if (required < 0)
                required = 0;

            int safeLevel = Mathf.Clamp(level, 1, 3);

            if (values == null)
            {
                if (required == 0)
                    return false;

                values = new float[required];
                for (int i = 0; i < required; i++)
                    values[i] = GetDefaultDescriptionValue(safeLevel);
                return true;
            }

            int current = values.Length;

            if (current == required)
                return false;

            if (current < required)
            {
                float[] grown = new float[required];
                Array.Copy(values, grown, current);
                for (int i = current; i < required; i++)
                    grown[i] = GetDefaultDescriptionValue(safeLevel);
                values = grown;
                return true;
            }

            // current > required
            if (!forceMatch)
                return false; // Safe Sync: không shrink.

            bool removedNonZero = false;
            for (int i = required; i < current; i++)
            {
                if (!Mathf.Approximately(values[i], 0f))
                {
                    removedNonZero = true;
                    break;
                }
            }

            if (removedNonZero)
            {
                UnityEngine.Debug.LogWarning(
                    $"[SkillDataSO] Force-match trimming Values (level {safeLevel}, "
                    + $"length {current} -> {required}); non-zero data may be lost.");
            }

            float[] trimmed = new float[required];
            Array.Copy(values, trimmed, required);
            values = trimmed;
            return true;
        }

        private static bool ResizeValuesEditor(
            SkillDescriptionLevelValues entry,
            int required,
            bool forceMatch)
        {
            if (entry == null)
                return false;

            float[] values = entry.Values;
            bool changed = ResizeDescriptionValues(ref values, required, entry.Level, forceMatch);

            if (changed)
                entry.Values = values;

            return changed;
        }
#endif
    }
}
