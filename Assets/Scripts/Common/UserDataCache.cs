using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.Skill;
using Newtonsoft.Json;
using UnityEngine;

namespace Common
{
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
        [SerializeField] private UserLevelConfigSO userLevelConfigSO;

        [Header("Debug")]
        [SerializeField] private bool enableLog = true;
        
        public ClassSkillUnlockData ClassSkillUnlock = new();
        public HeroSkillLoadoutData HeroSkillLoadout = new();

        /// <summary>Hero inventory từ server — set bởi GameBootstrap từ player/me (owned + lineup + shards).</summary>
        public HeroInventory HeroList { get; set; }

        /// <summary>Summon state từ server — set bởi GameBootstrap sau login.</summary>
        public SummonStateResponse SummonState { get; set; }

        /// <summary>Skill list từ server — set bởi GameBootstrap sau login.</summary>
        public SkillListResponse SkillList { get; set; }

        /// <summary>Weapon list từ server — set bởi GameBootstrap sau login.</summary>
        public WeaponListResponse WeaponList { get; set; }

        public List<int> InBattleHeroIdList { get;  set; } = new();

        public event Action<int> OnHeroSkillChanged;

        public override UniTask InitializeAsync()
        {
            EnsureInitialized();
            return UniTask.CompletedTask;
        }

        #region INIT

        void EnsureInitialized()
        {
            EnsureInitialSkillInventoryForUnlockedClassSkills();
        }

        public void GetPlayerDataFromServer(HeroInventory heroInventory, SkillListResponse skillListResponse, WeaponListResponse weaponListResponse)
        {
            InBattleHeroIdList = new List<int> { -1, -1 };
            try
            {
                HeroList = heroInventory;
                SkillList = skillListResponse;
                WeaponList = weaponListResponse;

                int heroIndex = 0;
                for (int i = 0; i < HeroList.Lineup.Length; i++)
                {
                    string currentHeroInLineUp = HeroList.Lineup[i];
                    for (int j = 0; j < HeroList.Owned.Length; j++)
                    {
                        var currentHeroOwned = HeroList.Owned[j];
                        if (string.Equals(currentHeroInLineUp, currentHeroOwned.Uid))
                        {
                            InBattleHeroIdList[heroIndex] = currentHeroOwned.HeroId;
                            heroIndex++;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            
        }

        #endregion

        #region HERO

        public List<int> GetEquippedSkills(int heroId)
        {
            if (!HeroSkillLoadout.EquippedSkillIdsByHero.TryGetValue(heroId, out var list))
                return new List<int>();

            return new List<int>(list);
        }

        #endregion

        #region CLASS UNLOCK

        private void SetUnlockedSkillsForClass(HeroClass heroClass, List<int> ids)
        {
            ClassSkillUnlock.UnlockedSkillIdsByClass[heroClass] = Normalize(ids);
        }

        public List<int> GetUnlockedSkills(HeroClass heroClass)
        {
            if (!ClassSkillUnlock.UnlockedSkillIdsByClass.TryGetValue(heroClass, out var list))
                return new List<int>();

            return new List<int>(list);
        }
        
        private void EnsureInitialSkillInventoryForUnlockedClassSkills()
        {
            // if (classSkillUnlockInitSO == null || classSkillUnlockInitSO.ClassEntries == null)
            //     return;
            //
            // foreach (var e in classSkillUnlockInitSO.ClassEntries)
            // {
            //     if (e == null || e.UnlockedSkillIds == null)
            //         continue;
            //
            //     foreach (int skillId in e.UnlockedSkillIds)
            //     {
            //         if (skillId <= 0)
            //             continue;
            //
            //         var data = Immortal_Switch.Scripts.Skill.UI.SkillInventorySaveService.GetOrCreate(skillId);
            //
            //         // Rule: skill unlock for game must be owned in inventory 
            //         if (!data.IsOwned)
            //             data.IsOwned = true;
            //
            //         if (data.Level <= 0)
            //             data.Level = 1;
            //
            //         if (data.CurrentShard < 0)
            //             data.CurrentShard = 0;
            //     }
            // }
            //
            // Immortal_Switch.Scripts.Skill.UI.SkillInventorySaveService.Save();
        }

        private bool IsUnlocked(HeroClass heroClass, int skillId)
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
                LogError($"Hero null {heroId} ");
                return;
            }

            var unlocked = GetUnlockedSkills(hero.HeroClass);

            var valid = Normalize(ids)
                .Where(unlocked.Contains)
                .Take(6)
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
            if (hero == null)
                return false;

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
    }
}