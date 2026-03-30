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
        [SerializeField] private List<HeroClassSkillPoolEntry> classPools = new();

        public event Action OnDataChanged;

        private Dictionary<HeroClass, List<SkillDataSO>> poolLookup;
        private PvEBattleController battleController;
        protected override void Awake()
        {
            base.Awake();
            BuildLookup();
            battleController = PvEBattleController.Instance;
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
                    .OrderByDescending(x => x.Tier)
                    .ThenBy(x => x.SkillId)
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
            if (battleController == null) return result;

            var activeHeroes = battleController.GetActiveHeroControllers();
            foreach (var hero in activeHeroes)
            {
                if (hero == null) continue;

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

            return poolLookup != null && poolLookup.TryGetValue(heroClass, out var list)
                ? list
                : new List<SkillDataSO>();
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

        public bool TryEquipSkillToHero(SkillViewHeroContext heroContext, int skillId)
        {
            if (heroContext == null || skillId <= 0)
                return false;

            if (!SkillInventorySaveService.IsOwned(skillId))
                return false;

            var current = SkillLoadoutSaveService.GetSelectedSkillIdsByHeroId(heroContext.HeroId);
            if (current.Contains(skillId))
                return true;

            if (current.Count >= 5)
                return false;

            current.Add(skillId);
            SkillLoadoutSaveService.SaveSelectedSkillIdsByHeroId(heroContext.HeroId, current);
            RefreshHeroRuntime(heroContext);
            OnDataChanged?.Invoke();
            return true;
        }

        public bool TryUnequipSkillFromHero(SkillViewHeroContext heroContext, int skillId)
        {
            if (heroContext == null || skillId <= 0)
                return false;

            var current = SkillLoadoutSaveService.GetSelectedSkillIdsByHeroId(heroContext.HeroId);
            if (!current.Remove(skillId))
                return false;

            SkillLoadoutSaveService.SaveSelectedSkillIdsByHeroId(heroContext.HeroId, current);
            RefreshHeroRuntime(heroContext);
            OnDataChanged?.Invoke();
            return true;
        }

        public bool TryReplaceSkillOnHero(SkillViewHeroContext heroContext, int slotIndex, int newSkillId)
        {
            if (heroContext == null || newSkillId <= 0)
                return false;

            if (!SkillInventorySaveService.IsOwned(newSkillId))
                return false;

            if (slotIndex < 0 || slotIndex >= 5)
                return false;

            var current = SkillLoadoutSaveService.GetSelectedSkillIdsByHeroId(heroContext.HeroId);

            while (current.Count < 5)
                current.Add(0);

            current[slotIndex] = newSkillId;
            current = current.Where(x => x > 0).Distinct().Take(5).ToList();

            SkillLoadoutSaveService.SaveSelectedSkillIdsByHeroId(heroContext.HeroId, current);
            RefreshHeroRuntime(heroContext);
            OnDataChanged?.Invoke();
            return true;
        }

        private void RefreshHeroRuntime(SkillViewHeroContext heroContext)
        {
            heroContext?.RuntimeController?.RefreshSelectedSkillsRuntime();
        }
    }
}