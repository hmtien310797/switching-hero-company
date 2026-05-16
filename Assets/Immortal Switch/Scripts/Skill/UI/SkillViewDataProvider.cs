using System;
using System.Collections.Generic;
using System.Linq;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill.UI
{
    [Serializable]
    public class HeroClassSkillPoolEntry
    {
        public HeroClass HeroClass;
        public List<SkillDataSO> Skills = new();
    }

    public class SkillViewHeroContext
    {
        public int HeroId;
        public HeroClass HeroClass;
        public Sprite HeroIcon;
        public List<int> EquippedSkillIds = new();
        public PlayerHeroController RuntimeController;
    }

    public class SkillViewSkillState
    {
        public SkillDataSO SkillData;
        public int SkillId;
        public int Level;
        public int CurrentShard;
        public int RequiredShard;
        public bool IsOwned;
        public bool IsEquipped;
        public int EquippedSlotIndex = -1;
    }

    public class SkillViewDataProvider : Singleton<SkillViewDataProvider>
    {
        private PvEBattleController battleController;

        [Header("Skill Pools By Class")]
        [SerializeField] private List<HeroClassSkillPoolEntry> classPools = new();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private List<SkillDataSO> allSkills = new();

        private Dictionary<int, SkillDataSO> skillCache;
        private Dictionary<HeroClass, List<SkillDataSO>> poolLookup;

        public event Action OnDataChanged;

        public override UniTask InitializeAsync()
        {
            battleController = PvEBattleController.Instance;
            BuildLookup();
            BuildCacheIfNeeded();
            return UniTask.CompletedTask;
        }

        private void OnEnable()
        {
            if (battleController != null)
                battleController.OnActiveLineupChanged += HandleBattleLineupChanged;

            if (UserDataCache.Instance != null)
                UserDataCache.Instance.OnHeroSkillChanged += HandleHeroSkillChanged;
        }

        private void OnDisable()
        {
            if (battleController != null)
                battleController.OnActiveLineupChanged -= HandleBattleLineupChanged;

            if (UserDataCache.Instance != null)
                UserDataCache.Instance.OnHeroSkillChanged -= HandleHeroSkillChanged;
        }
        
        public void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }

        private void Log(string message)
        {
            if (!enableDebugLog) return;
            Debug.Log($"[SkillViewDataProvider] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!enableDebugLog) return;
            Debug.LogWarning($"[SkillViewDataProvider] {message}", this);
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SkillViewDataProvider] {message}", this);
        }

        private void HandleBattleLineupChanged()
        {
            Log("Battle lineup changed -> refresh UI");
            OnDataChanged?.Invoke();
        }

        private void HandleHeroSkillChanged(int heroId)
        {
            Log($"Hero skill loadout changed -> heroId={heroId}");
            OnDataChanged?.Invoke();
        }

        private void BuildLookup()
        {
            poolLookup = new Dictionary<HeroClass, List<SkillDataSO>>();

            if (classPools == null)
            {
                LogError("classPools is null.");
                return;
            }

            foreach (var entry in classPools)
            {
                if (entry == null) 
                    continue;

                var list = entry.Skills?
                    .Where(x => x != null)
                    .Distinct()
                    .OrderBy(x => x.SkillId)
                    .ToList() ?? new List<SkillDataSO>();

                poolLookup[entry.HeroClass] = list;

                Log($"BuildLookup -> class={entry.HeroClass}, count={list.Count}");
            }
        }

        private void BuildCacheIfNeeded()
        {
            if (skillCache != null)
                return;

            skillCache = new Dictionary<int, SkillDataSO>();

            if (allSkills == null)
                return;

            for (int i = 0; i < allSkills.Count; i++)
            {
                var skill = allSkills[i];
                if (skill == null)
                    continue;

                skillCache[skill.SkillId] = skill;
            }
        }

        public List<SkillDataSO> GetAllSkillData()
        {
            BuildCacheIfNeeded();

            if (allSkills == null)
                return new List<SkillDataSO>();

            return allSkills
                .Where(x => x != null)
                .OrderBy(x => x.SkillId)
                .ToList();
        }

        public SkillDataSO GetSkillData(int skillId)
        {
            if (skillId <= 0)
                return null;

            BuildCacheIfNeeded();

            if (skillCache != null && skillCache.TryGetValue(skillId, out var localData))
                return localData;

            var masterData = MasterDataCache.Instance != null
                ? MasterDataCache.Instance.GetSkillDataById(skillId)
                : null;

            if (masterData != null)
            {
                if (skillCache == null)
                    skillCache = new Dictionary<int, SkillDataSO>();

                skillCache[skillId] = masterData;
            }

            return masterData;
        }

        public bool HasAssignedHero(HeroClass heroClass)
        {
            if (battleController == null)
            {
                LogError("HasAssignedHero failed because battleController is null.");
                return false;
            }

            bool result = battleController.HasActiveHeroOfClass(heroClass);
            Log($"HasAssignedHero -> class={heroClass}, result={result}");
            return result;
        }

        public List<SkillViewHeroContext> GetAssignedHeroes()
        {
            var result = new List<SkillViewHeroContext>();

            if (battleController == null)
            {
                LogError("GetAssignedHeroes failed because battleController is null.");
                return result;
            }

            var activeHeroes = battleController.GetActiveHeroControllers();
            Log($"GetAssignedHeroes -> active count={activeHeroes.Count}");

            foreach (var hero in activeHeroes)
            {
                if (hero == null)
                {
                    LogWarning("GetAssignedHeroes found null hero.");
                    continue;
                }

                int heroId = hero.GetHeroId();
                var equipped = UserDataCache.Instance != null
                    ? UserDataCache.Instance.GetEquippedSkills(heroId)
                    : new List<int>();

                result.Add(new SkillViewHeroContext
                {
                    HeroId = heroId,
                    HeroClass = hero.HeroClass,
                    HeroIcon = hero.HeroIcon,
                    EquippedSkillIds = equipped,
                    RuntimeController = hero
                });

                Log($"GetAssignedHeroes -> heroId={heroId}, class={hero.HeroClass}, equipped=[{string.Join(",", equipped)}]");
            }

            return result;
        }

        public SkillViewHeroContext GetAssignedHeroByClass(HeroClass heroClass)
        {
            if (battleController == null)
            {
                LogError("GetAssignedHeroByClass failed because battleController is null.");
                return null;
            }

            PlayerHeroController heroController = battleController.TryGetActiveHeroByClass(heroClass);
            if (heroController == null)
            {
                LogWarning($"GetAssignedHeroByClass -> no active hero for class={heroClass}");
                return null;
            }

            int heroId = heroController.GetHeroId();
            var equipped = UserDataCache.Instance != null
                ? UserDataCache.Instance.GetEquippedSkills(heroId)
                : new List<int>();

            Log($"GetAssignedHeroByClass -> class={heroClass}, heroId={heroId}, equipped=[{string.Join(",", equipped)}]");

            return new SkillViewHeroContext
            {
                HeroId = heroId,
                HeroClass = heroController.HeroClass,
                HeroIcon = heroController.HeroIcon,
                EquippedSkillIds = equipped,
                RuntimeController = heroController
            };
        }

        public List<SkillDataSO> GetClassPool(HeroClass heroClass)
        {
            if (poolLookup == null)
                BuildLookup();

            if (poolLookup == null)
            {
                LogError("GetClassPool failed because poolLookup is null.");
                return new List<SkillDataSO>();
            }

            if (!poolLookup.TryGetValue(heroClass, out var list) || list == null)
            {
                LogWarning($"GetClassPool -> no pool found for class={heroClass}");
                return new List<SkillDataSO>();
            }

            return list;
        }

        public SkillViewSkillState BuildSkillState(SkillViewHeroContext heroContext, SkillDataSO skillData)
        {
            if (skillData == null)
                return null;

            var equippedIds = heroContext != null ? heroContext.EquippedSkillIds : new List<int>();

            int level = SkillInventorySaveService.GetLevel(skillData.SkillId);
            int currentShard = SkillInventorySaveService.GetCurrentShard(skillData.SkillId);
            bool isOwned = SkillInventorySaveService.IsOwned(skillData.SkillId) || currentShard > 0;

            int requiredShard = 0;
            if (!skillData.IsMaxLevel(level))
                requiredShard = skillData.GetRequiredShardForLevel(level);

            var state = new SkillViewSkillState
            {
                SkillData = skillData,
                SkillId = skillData.SkillId,
                Level = level,
                CurrentShard = currentShard,
                RequiredShard = requiredShard,
                IsOwned = isOwned
            };

            state.EquippedSlotIndex = equippedIds.FindIndex(x => x == state.SkillId);
            state.IsEquipped = state.EquippedSlotIndex >= 0;

            return state;
        }

        public List<SkillDataSO> GetSortedPoolForHero(SkillViewHeroContext heroContext)
        {
            if (heroContext == null)
                return new List<SkillDataSO>();

            var pool = GetClassPool(heroContext.HeroClass);

            // Sort:
            // 1. skill đang equip
            // 2. skill đã unlock
            // 3. theo id
            var sorted = pool
                .OrderByDescending(x =>
                {
                    var state = BuildSkillState(heroContext, x);
                    return state != null && state.IsEquipped;
                })
                .ThenByDescending(x =>
                {
                    var state = BuildSkillState(heroContext, x);
                    return state != null && state.IsOwned;
                })
                .ThenBy(x => x.SkillId)
                .ToList();

            Log($"GetSortedPoolForHero -> heroId={heroContext.HeroId}, class={heroContext.HeroClass}, count={sorted.Count}");
            return sorted;
        }

        public bool TryEquipSkillToHero(SkillViewHeroContext heroContext, int skillId)
        {
            if (heroContext == null)
            {
                LogError("TryEquipSkillToHero failed because heroContext is null.");
                return false;
            }

            if (UserDataCache.Instance == null)
            {
                LogError("TryEquipSkillToHero failed because UserDataCache.Instance is null.");
                return false;
            }

            Log($"TryEquipSkillToHero -> heroId={heroContext.HeroId}, skillId={skillId}");

            bool success = UserDataCache.Instance.Equip(heroContext.HeroId, skillId);
            if (!success)
            {
                LogWarning($"TryEquipSkillToHero failed -> heroId={heroContext.HeroId}, skillId={skillId}");
                return false;
            }

            heroContext.RuntimeController?.RefreshSelectedSkillsRuntime();
            OnDataChanged?.Invoke();

            Log($"TryEquipSkillToHero success -> heroId={heroContext.HeroId}, skillId={skillId}");
            return true;
        }

        public bool TryUnequipSkillFromHero(SkillViewHeroContext heroContext, int skillId)
        {
            if (heroContext == null)
            {
                LogError("TryUnequipSkillFromHero failed because heroContext is null.");
                return false;
            }

            if (UserDataCache.Instance == null)
            {
                LogError("TryUnequipSkillFromHero failed because UserDataCache.Instance is null.");
                return false;
            }

            Log($"TryUnequipSkillFromHero -> heroId={heroContext.HeroId}, skillId={skillId}");

            bool success = UserDataCache.Instance.Unequip(heroContext.HeroId, skillId);
            if (!success)
            {
                LogWarning($"TryUnequipSkillFromHero failed -> heroId={heroContext.HeroId}, skillId={skillId}");
                return false;
            }

            heroContext.RuntimeController?.RefreshSelectedSkillsRuntime();
            OnDataChanged?.Invoke();

            Log($"TryUnequipSkillFromHero success -> heroId={heroContext.HeroId}, skillId={skillId}");
            return true;
        }

        public bool TryReplaceSkillOnHero(SkillViewHeroContext heroContext, int slotIndex, int newSkillId)
        {
            if (heroContext == null)
            {
                LogError("TryReplaceSkillOnHero failed because heroContext is null.");
                return false;
            }

            if (UserDataCache.Instance == null)
            {
                LogError("TryReplaceSkillOnHero failed because UserDataCache.Instance is null.");
                return false;
            }

            Log($"TryReplaceSkillOnHero -> heroId={heroContext.HeroId}, slotIndex={slotIndex}, newSkillId={newSkillId}");

            bool success = UserDataCache.Instance.Replace(heroContext.HeroId, slotIndex, newSkillId);
            if (!success)
            {
                LogWarning($"TryReplaceSkillOnHero failed -> heroId={heroContext.HeroId}, slotIndex={slotIndex}, newSkillId={newSkillId}");
                return false;
            }

            heroContext.RuntimeController?.RefreshSelectedSkillsRuntime();
            OnDataChanged?.Invoke();

            Log($"TryReplaceSkillOnHero success -> heroId={heroContext.HeroId}, slotIndex={slotIndex}, newSkillId={newSkillId}");
            return true;
        }
    }
}