using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Core;
using Scripts.Battle;
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
        public bool IsMain;
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
        [SerializeField] private List<HeroClassSkillPoolEntry> classPools = new();

        public event Action OnDataChanged;

        private Dictionary<HeroClass, List<SkillDataSO>> poolLookup;
        
        [SerializeField] private bool enableDebugLog = true;

        private void LogProvider(string message)
        {
            if (!enableDebugLog) return;
            Debug.Log($"[SkillViewDataProvider] {message}", this);
        }

        private void LogWarningProvider(string message)
        {
            if (!enableDebugLog) return;
            Debug.LogWarning($"[SkillViewDataProvider] {message}", this);
        }

        private void LogErrorProvider(string message)
        {
            Debug.LogError($"[SkillViewDataProvider] {message}", this);
        }

        protected override void Awake()
        {
            base.Awake();
            battleController = PvEBattleController.Instance;
            BuildLookup();
        }

        private void OnEnable()
        {
            if (battleController != null)
                battleController.OnActiveLineupChanged += HandleBattleLineupChanged;
        }

        private void OnDisable()
        {
            if (battleController != null)
                battleController.OnActiveLineupChanged -= HandleBattleLineupChanged;
        }

        private void HandleBattleLineupChanged()
        {
            LogProvider("Battle lineup changed. Notify UI refresh.");
            OnDataChanged?.Invoke();
        }

        private void BuildLookup()
        {
            poolLookup = new Dictionary<HeroClass, List<SkillDataSO>>();

            foreach (var entry in classPools)
            {
                if (entry == null) continue;

                poolLookup[entry.HeroClass] = entry.Skills?
                    .Where(x => x != null)
                    .OrderBy(x => x.SkillId)
                    .ToList() ?? new List<SkillDataSO>();
            }
        }

        public bool HasAssignedHero(HeroClass heroClass)
        {
            return battleController != null && battleController.HasActiveHeroOfClass(heroClass);
        }

        public List<SkillViewHeroContext> GetAssignedHeroes()
        {
            var result = new List<SkillViewHeroContext>();
            if (battleController == null)
            {
                LogErrorProvider("battleController is null.");
                return result;
            }

            var activeHeroes = battleController.GetActiveHeroControllers();
            LogProvider($"GetAssignedHeroes -> activeHeroesCount={activeHeroes.Count}");

            foreach (var hero in activeHeroes)
            {
                if (hero == null)
                {
                    LogWarningProvider("GetAssignedHeroes found null hero in active list.");
                    continue;
                }

                result.Add(new SkillViewHeroContext
                {
                    HeroId = hero.GetHeroId(),
                    HeroClass = hero.HeroClass,
                    HeroIcon = hero.HeroIcon,
                    IsMain = hero.IsMainHero,
                    EquippedSkillIds = hero.GetOrderedEquippedSkillIds(),
                    RuntimeController = hero
                });
            }

            return result;
        }

        public SkillViewHeroContext GetAssignedHeroByClass(HeroClass heroClass)
        {
            if (battleController == null) return null;
            if (!battleController.TryGetActiveHeroByClass(heroClass, out var hero) || hero == null)
                return null;

            return new SkillViewHeroContext
            {
                HeroId = hero.GetHeroId(),
                HeroClass = hero.HeroClass,
                HeroIcon = hero.HeroIcon,
                IsMain = hero.IsMainHero,
                EquippedSkillIds = hero.GetOrderedEquippedSkillIds(),
                RuntimeController = hero
            };
        }

        public List<SkillDataSO> GetClassPool(HeroClass heroClass)
        {
            if (poolLookup == null)
                BuildLookup();
            List<SkillDataSO> pool;
            if(poolLookup != null && poolLookup.TryGetValue(heroClass, out var list))
            {
                pool = list;
            }
            else
            {
                pool = new List<SkillDataSO>();
            }
            return pool;
        }

        public SkillViewSkillState BuildSkillState(SkillViewHeroContext heroContext, SkillDataSO skillData)
        {
            if (skillData == null)
                return null;

            var state = new SkillViewSkillState
            {
                SkillData = skillData,
                SkillId = skillData.SkillId,
                Level = SkillInventorySaveService.GetLevel(skillData.SkillId),
                CurrentShard = SkillInventorySaveService.GetCurrentShard(skillData.SkillId),
                RequiredShard = SkillInventorySaveService.GetRequiredShard(skillData.SkillId),
                IsOwned = SkillInventorySaveService.IsOwned(skillData.SkillId)
            };

            if (heroContext != null && heroContext.EquippedSkillIds != null)
            {
                state.EquippedSlotIndex = heroContext.EquippedSkillIds.FindIndex(x => x == state.SkillId);
                state.IsEquipped = state.EquippedSlotIndex >= 0;
            }

            return state;
        }

        public List<SkillDataSO> GetSortedPoolForHero(SkillViewHeroContext heroContext)
        {
            var pool = heroContext != null ? GetClassPool(heroContext.HeroClass) : new List<SkillDataSO>();

            return pool
                .OrderByDescending(x => BuildSkillState(heroContext, x).IsEquipped)
                .ThenByDescending(x => BuildSkillState(heroContext, x).IsOwned)
                .ThenBy(x => x.SkillId)
                .ToList();
        }

        public bool TryEquipSkillToHero(SkillViewHeroContext heroContext, int skillId)
        {
            if (heroContext == null || skillId <= 0)
            {
                LogErrorProvider($"TryEquipSkillToHero invalid input heroContext={(heroContext == null ? "null" : heroContext.HeroId.ToString())}, skillId={skillId}");
                return false;
            }

            if (!SkillInventorySaveService.IsOwned(skillId))
            {
                LogWarningProvider($"TryEquipSkillToHero skill not owned -> heroId={heroContext.HeroId}, skillId={skillId}");
                return false;
            }

            var current = SkillLoadoutSaveService.GetSelectedSkillIdsByHeroId(heroContext.HeroId);
            LogProvider($"TryEquipSkillToHero before -> heroId={heroContext.HeroId}, current=[{string.Join(",", current)}], newSkillId={skillId}");

            if (current.Contains(skillId))
            {
                LogWarningProvider($"TryEquipSkillToHero ignored because skill already equipped. heroId={heroContext.HeroId}, skillId={skillId}");
                return true;
            }

            if (current.Count >= 5)
            {
                LogWarningProvider($"TryEquipSkillToHero failed because hero already has 5 skills. heroId={heroContext.HeroId}");
                return false;
            }

            current.Add(skillId);
            SkillLoadoutSaveService.SaveSelectedSkillIdsByHeroId(heroContext.HeroId, current);
            RefreshHeroRuntime(heroContext);

            LogProvider($"TryEquipSkillToHero success -> heroId={heroContext.HeroId}, after=[{string.Join(",", current)}]");
            OnDataChanged?.Invoke();
            return true;
        }

        public bool TryUnequipSkillFromHero(SkillViewHeroContext heroContext, int skillId)
        {
            if (heroContext == null || skillId <= 0)
            {
                LogErrorProvider($"TryUnequipSkillFromHero invalid input heroContext={(heroContext == null ? "null" : heroContext.HeroId.ToString())}, skillId={skillId}");
                return false;
            }

            var current = SkillLoadoutSaveService.GetSelectedSkillIdsByHeroId(heroContext.HeroId);
            LogProvider($"TryUnequipSkillFromHero before -> heroId={heroContext.HeroId}, current=[{string.Join(",", current)}], skillId={skillId}");

            if (!current.Remove(skillId))
            {
                LogWarningProvider($"TryUnequipSkillFromHero failed because skill not found. heroId={heroContext.HeroId}, skillId={skillId}");
                return false;
            }

            SkillLoadoutSaveService.SaveSelectedSkillIdsByHeroId(heroContext.HeroId, current);
            RefreshHeroRuntime(heroContext);

            LogProvider($"TryUnequipSkillFromHero success -> heroId={heroContext.HeroId}, after=[{string.Join(",", current)}]");
            OnDataChanged?.Invoke();
            return true;
        }

        public bool TryReplaceSkillOnHero(SkillViewHeroContext heroContext, int slotIndex, int newSkillId)
        {
            if (heroContext == null || newSkillId <= 0)
            {
                LogErrorProvider($"TryReplaceSkillOnHero invalid input heroContext={(heroContext == null ? "null" : heroContext.HeroId.ToString())}, newSkillId={newSkillId}");
                return false;
            }

            if (!SkillInventorySaveService.IsOwned(newSkillId))
            {
                LogWarningProvider($"TryReplaceSkillOnHero skill not owned -> heroId={heroContext.HeroId}, newSkillId={newSkillId}");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= 5)
            {
                LogErrorProvider($"TryReplaceSkillOnHero invalid slotIndex={slotIndex}");
                return false;
            }

            var current = SkillLoadoutSaveService.GetSelectedSkillIdsByHeroId(heroContext.HeroId);
            while (current.Count < 5)
                current.Add(0);

            LogProvider($"TryReplaceSkillOnHero before -> heroId={heroContext.HeroId}, slotIndex={slotIndex}, newSkillId={newSkillId}, current=[{string.Join(",", current)}]");

            current[slotIndex] = newSkillId;
            current = current.Where(x => x > 0).Distinct().Take(5).ToList();

            SkillLoadoutSaveService.SaveSelectedSkillIdsByHeroId(heroContext.HeroId, current);
            RefreshHeroRuntime(heroContext);

            LogProvider($"TryReplaceSkillOnHero success -> heroId={heroContext.HeroId}, after=[{string.Join(",", current)}]");
            OnDataChanged?.Invoke();
            return true;
        }

        private void RefreshHeroRuntime(SkillViewHeroContext heroContext)
        {
            heroContext?.RuntimeController?.RefreshSelectedSkillsRuntime();
        }
    }
}