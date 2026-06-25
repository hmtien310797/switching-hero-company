using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Common
{
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
        //public HeroSkillLoadoutData HeroSkillLoadout = new();

        /// <summary>Hero inventory từ server — set bởi GameBootstrap từ player/me (owned + lineup + shards).</summary>
        public HeroInventory HeroList { get; set; }

        /// <summary>Summon state từ server — set bởi GameBootstrap sau login.</summary>
        public SummonStateResponse SummonState { get; set; }

        /// <summary>Skill list từ server — set bởi GameBootstrap sau login.</summary>
        [ShowInInspector]
        public SkillListResponse SkillList { get; set; }

        /// <summary>Weapon list từ server — set bởi GameBootstrap sau login.</summary>
        public WeaponListResponse WeaponList { get; set; }

        public List<int> InBattleHeroIdList { get; private set; } = new();
        public readonly HeroActor[] inBattleHeroes = new HeroActor[2];

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

        public void ApplySkillEnhanceEntries(SkillEnhanceEntry[] entries)
        {
            if (SkillList == null || entries == null) return;

            foreach (var entry in entries)
            {
                if (SkillList.Owned != null)
                {
                    foreach (var inst in SkillList.Owned)
                    {
                        if (inst.SkillId == entry.SkillId)
                        {
                            inst.Level = entry.NewLevel;
                            break;
                        }
                    }
                }

                if (SkillList.Shards == null)
                    SkillList.Shards = new Dictionary<string, int>();
                SkillList.Shards[entry.SkillId.ToString()] = entry.NewShard;
            }
        }

        public void ApplySkillSummonEntries(SummonEntry[] entries)
        {
            if (entries == null) return;
            if (SkillList == null) SkillList = new SkillListResponse();

            foreach (var entry in entries)
            {
                if (entry.SkillId <= 0) continue;

                if (entry.IsNew)
                {
                    var list = SkillList.Owned != null
                        ? new List<SkillInstance>(SkillList.Owned)
                        : new List<SkillInstance>();
                    list.Add(new SkillInstance { SkillId = entry.SkillId, Level = 1 });
                    SkillList.Owned = list.ToArray();
                }

                if (entry.ShardGained > 0)
                {
                    if (SkillList.Shards == null)
                        SkillList.Shards = new Dictionary<string, int>();
                    string key = entry.SkillId.ToString();
                    SkillList.Shards[key] = (SkillList.Shards.TryGetValue(key, out int cur) ? cur : 0) + entry.ShardGained;
                }
            }
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

        public List<int> GetEquippedClassSkillIds(int heroId)
        {
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                var currentHero = inBattleHeroes[i];
                if (currentHero.HeroData.Id == heroId)
                {
                    List<int> allClassSkillIds = currentHero.HeroSkillController.GetAllEquippedClassSkills().ConvertAll(skill => skill.SkillId);
                    return allClassSkillIds;
                }
            }

            return null;
        }

        public HeroActor GetInBattleHeroActorById(int heroId)
        {
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                var currentHero = inBattleHeroes[i];
                if (currentHero.HeroData.Id == heroId)
                {
                    return currentHero;
                }
            }
            return null;
        }

        public SkillDataSO GetEquippedSkillDataById(int heroId,int skillId)
        {
            var currentHero = GetInBattleHeroActorById(heroId);
            for (int i = 0; i < currentHero.HeroSkillController.GetAllEquippedClassSkills().Count; i++)
            {
                SkillDataSO skillData = currentHero.HeroSkillController.GetAllEquippedClassSkills()[i];
                if (skillData.SkillId == skillId)
                {
                    return skillData;
                }
            }
            return null;
        }

        #endregion

        #region CLASS UNLOCK

        // private void SetUnlockedSkillsForClass(HeroClass heroClass, List<int> ids)
        // {
        //     ClassSkillUnlock.UnlockedSkillIdsByClass[heroClass] = Normalize(ids);
        // }

        // public List<int> GetUnlockedSkills(HeroClass heroClass)
        // {
        //     if (!ClassSkillUnlock.UnlockedSkillIdsByClass.TryGetValue(heroClass, out var list))
        //         return new List<int>();
        //
        //     return new List<int>(list);
        // }
        
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
            // Source of truth: server-owned skill list (UnlockedSkillIdsByClass is never populated)
            if (SkillList?.Owned != null)
                foreach (var inst in SkillList.Owned)
                    if (inst.SkillId == skillId) return true;
            return false;
        }

        public string GetHeroUid(int heroId)
        {
            if (HeroList?.Owned == null) return null;
            foreach (var h in HeroList.Owned)
                if (h.HeroId == heroId) return h.Uid;
            return null;
        }

        public string GetSkillUid(int skillId)
        {
            if (SkillList?.Owned == null) return null;
            foreach (var s in SkillList.Owned)
                if (s.SkillId == skillId) return s.Uid;
            return null;
        }

        #endregion

        #region HERO LOADOUT

        public bool EquipSkill(int heroId, int skillId)
        {
            var hero = MasterDataCache.Instance.GetHeroDataById(heroId);
            if (hero == null)
                return false;
        
            if (!IsUnlocked(hero.HeroClass, skillId))
                return false;
        
            var list = GetEquippedClassSkillIds(heroId);
        
            if (list.Count >= 5) return false;
            if (list.Contains(skillId)) return true;
        
            list.Add(skillId);
            OnHeroSkillChanged?.Invoke(heroId);
            return true;
        }

        public bool UnequipSkill(int heroId, int skillId)
        {
            SkillDataSO equippedSkillData = GetEquippedSkillDataById(heroId, skillId);
            HeroActor actor = GetInBattleHeroActorById(heroId);
            return actor.HeroSkillController.UnequipSkill(equippedSkillData);
        }
        
        // public bool ReplaceSkill(int heroId, int slot, int skillId)
        // {
        //     var hero = MasterDataCache.Instance.GetHeroDataById(heroId);
        //     if (!IsUnlocked(hero.HeroClass, skillId)) return false;
        //
        //     var list = GetEquippedSkills(heroId);
        //
        //     while (list.Count < 5)
        //         list.Add(0);
        //
        //     list[slot] = skillId;
        //
        //     list = list.Where(x => x > 0).Distinct().ToList();
        //
        //     SetEquippedSkillsForHero(heroId, list);
        //     return true;
        // }

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