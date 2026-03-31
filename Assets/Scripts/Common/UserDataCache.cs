using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Scripts.Common
{
    [Serializable]
    public class HeroDataOwn
    {
        public List<int> OwnedHeroIds = new();
    }

    [Serializable]
    public class SelectedHero
    {
        public int MainHeroId;
        public int SubHeroId;
    }

    [Serializable]
    public class ClassSkillUnlockData
    {
        public Dictionary<HeroClass, List<int>> UnlockedSkillIdsByClass = new();
    }

    [Serializable]
    public class HeroSkillLoadoutData
    {
        public Dictionary<int, List<int>> EquippedSkillIdsByHero = new();
    }

    public class UserDataCache : Singleton<UserDataCache>
    {
        [Header("Init Config")]
        [SerializeField] private ClassSkillUnlockInitSO classSkillUnlockInitSO;
        [SerializeField] private HeroSkillLoadoutInitSO heroSkillLoadoutInitSO;

        [Header("Debug")]
        [SerializeField] private bool enableLog = true;

        public HeroDataOwn OwnedHeroData = new();
        public SelectedHero SelectedHeros = new();
        public ClassSkillUnlockData ClassSkillUnlock = new();
        public HeroSkillLoadoutData HeroSkillLoadout = new();

        public int initialGold = 1000;
        public int initialDiamond = 1000;
        public int initialHeroTicket = 1000;

        public event Action<int> OnHeroSkillChanged;

        public override UniTask InitializeAsync()
        {
            EnsureInitialized();
            return UniTask.CompletedTask;
        }

        #region LOG

        void Log(string msg)
        {
            if (enableLog) Debug.Log($"[UserData] {msg}", this);
        }

        void LogError(string msg)
        {
            Debug.LogError($"[UserData] {msg}", this);
        }

        #endregion

        #region INIT

        void EnsureInitialized()
        {
            if (SelectedHeros.MainHeroId <= 0) SelectedHeros.MainHeroId = 1;
            if (SelectedHeros.SubHeroId <= 0) SelectedHeros.SubHeroId = 3;

            AddOwnedHero(SelectedHeros.MainHeroId);
            AddOwnedHero(SelectedHeros.SubHeroId);

            // Class unlock
            foreach (var e in classSkillUnlockInitSO.ClassEntries)
            {
                SetUnlockedSkillsForClass(e.HeroClass, e.UnlockedSkillIds);
            }

            // Hero loadout
            foreach (var e in heroSkillLoadoutInitSO.HeroEntries)
            {
                AddOwnedHero(e.HeroId);
                SetEquippedSkillsForHero(e.HeroId, e.EquippedSkillIds, false);
            }

        }

        #endregion

        #region HERO

        public void AddOwnedHero(int heroId)
        {
            if (!OwnedHeroData.OwnedHeroIds.Contains(heroId))
                OwnedHeroData.OwnedHeroIds.Add(heroId);
        }

        public List<int> GetEquippedSkills(int heroId)
        {
            if (!HeroSkillLoadout.EquippedSkillIdsByHero.TryGetValue(heroId, out var list))
                return new List<int>();

            return new List<int>(list);
        }

        #endregion

        #region CLASS UNLOCK

        public void SetUnlockedSkillsForClass(HeroClass heroClass, List<int> ids)
        {
            ClassSkillUnlock.UnlockedSkillIdsByClass[heroClass] = Normalize(ids);
        }

        public List<int> GetUnlockedSkills(HeroClass heroClass)
        {
            if (!ClassSkillUnlock.UnlockedSkillIdsByClass.TryGetValue(heroClass, out var list))
                return new List<int>();

            return list;
        }

        public bool IsUnlocked(HeroClass heroClass, int skillId)
        {
            return GetUnlockedSkills(heroClass).Contains(skillId);
        }

        #endregion

        #region HERO LOADOUT

        public void SetEquippedSkillsForHero(int heroId, List<int> ids, bool notify = true)
        {
            var hero = MasterDataCache.Instance.GetHeroDataById(heroId);
            if (hero == null)
            {
                LogError($"Hero null {heroId}");
                return;
            }

            var unlocked = GetUnlockedSkills(hero.HeroClass);

            var valid = Normalize(ids)
                .Where(unlocked.Contains)
                .Take(5)
                .ToList();

            HeroSkillLoadout.EquippedSkillIdsByHero[heroId] = valid;

            if (notify)
            {
                OnHeroSkillChanged?.Invoke(heroId);
            }
        }

        public bool Equip(int heroId, int skillId)
        {
            var hero = MasterDataCache.Instance.GetHeroDataById(heroId);
            if (!IsUnlocked(hero.HeroClass, skillId))
                return false;

            var list = GetEquippedSkills(heroId);

            if (list.Count >= 5) return false;
            if (list.Contains(skillId)) return true;

            list.Add(skillId);
            SetEquippedSkillsForHero(heroId, list);
            return true;
        }

        public bool Unequip(int heroId, int skillId)
        {
            var list = GetEquippedSkills(heroId);
            if (!list.Remove(skillId)) return false;

            SetEquippedSkillsForHero(heroId, list);
            return true;
        }

        public bool Replace(int heroId, int slot, int skillId)
        {
            var hero = MasterDataCache.Instance.GetHeroDataById(heroId);
            if (!IsUnlocked(hero.HeroClass, skillId)) return false;

            var list = GetEquippedSkills(heroId);

            while (list.Count < 5)
                list.Add(0);

            list[slot] = skillId;

            list = list.Where(x => x > 0).Distinct().ToList();

            SetEquippedSkillsForHero(heroId, list);
            return true;
        }

        #endregion

        #region UTIL

        List<int> Normalize(List<int> ids)
        {
            return ids?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();
        }

        #endregion
    }
}