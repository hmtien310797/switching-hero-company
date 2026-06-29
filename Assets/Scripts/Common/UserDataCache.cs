using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SkillRemake;
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
        public bool AutoSkill;

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
        
        public void SetAutoSkill(bool isAutoSkill)
        {
            AutoSkill = isAutoSkill;
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                inBattleHeroes[i].SetAutoSkill(isAutoSkill);
            }
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

                if (currentHero == null ||
                    currentHero.HeroData == null ||
                    currentHero.HeroData.Id != heroId ||
                    currentHero.HeroSkillController == null)
                {
                    continue;
                }

                var equippedSkills = currentHero.HeroSkillController.GetAllEquippedClassSkills();
                var skillIds = new List<int>();

                if (equippedSkills == null)
                {
                    return skillIds;
                }

                // Giữ đúng vị trí slot thật (0 = slot trống) — KHÔNG dồn (compact) danh sách,
                // vì server lưu equipped[hero_uid] là mảng cố định 5 phần tử theo slot_index thật
                // (xem docs/api_skill_equip.md). Nếu dồn lại, slotIndex tính từ List.Count ở các
                // hàm gọi (TryEquipSkillToHero, TryAutoEquipSkillsToHero) sẽ lệch khỏi slot thật
                // ngay khi 1 skill bị unequip không phải ở cuối danh sách, khiến lần equip kế tiếp
                // ghi đè/làm mất skill ở slot khác trên server.
                for (int j = 0; j < equippedSkills.Count; j++)
                {
                    var skill = equippedSkills[j];
                    skillIds.Add(skill != null ? skill.SkillId : 0);
                }

                return skillIds;
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

            List<SkillDataSO> allEquipClassSkill = currentHero.HeroSkillController.GetAllEquippedClassSkills();

            if (allEquipClassSkill == null)
            {
                return null;
            }
            
            for (int i = 0; i < allEquipClassSkill.Count; i++)
            {
                SkillDataSO skillData = allEquipClassSkill[i];
                if (skillData != null && skillData.SkillId == skillId)
                {
                    return skillData;
                }
            }
            return null;
        }
        
        public int GetServerSkillLevel(int skillId)
        {
            if (SkillList?.Owned == null) return 0;
            foreach (var s in SkillList.Owned)
                if (s.SkillId == skillId) return s.Level > 0 ? s.Level : 1;
            return 0;
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

        /// <summary>Trang bị skill vào slot trống đầu tiên (thật, không phải vị trí đã dồn).
        /// Trả về slot_index thật đã dùng để caller gửi đúng lên server (skill/equip), hoặc -1 nếu
        /// không trang bị được.</summary>
        public async UniTask<int> EquipSkill(int heroId, int skillId)
        {
            HeroActor currentHero = GetInBattleHeroActorById(heroId);

            if (!IsUnlocked(currentHero.HeroClass, skillId))
                return -1;

            SkillDataSO skillToEquip = MasterDataCache.Instance.GetSkillDataById(skillId);

            bool canEquip = currentHero.HeroSkillController.CanEquipClassSkill(skillToEquip, out int slotIndex);
            if (!canEquip)
                return -1;

            await AddressableSkillSpawnService.PrewarmSkillRuntimeAssetsAsync(skillToEquip);
            currentHero.HeroSkillController.EquipSkill(skillToEquip);
            OnHeroSkillChanged?.Invoke(heroId);
            return slotIndex;
        }

        public bool UnequipSkill(int heroId, int skillId)
        {
            SkillDataSO equippedSkillData = GetEquippedSkillDataById(heroId, skillId);
            HeroActor actor = GetInBattleHeroActorById(heroId);
            bool equipResult = actor.HeroSkillController.UnequipSkill(equippedSkillData);
            if (equipResult)
            {
                AddressableSkillSpawnService.DisposeSkillComponent(equippedSkillData);
                OnHeroSkillChanged?.Invoke(heroId);
            }
            return equipResult;
        }
        
        public async UniTask<bool> ReplaceSkill(int heroId, int slot, int skillId)
        {
            HeroActor currentHero = GetInBattleHeroActorById(heroId);
            SkillDataSO skillData = GetEquippedSkillDataById(heroId, skillId);
            if (skillData == null || currentHero == null)
            {
                return false;
            }
            AddressableSkillSpawnService.DisposeSkillComponent(currentHero.HeroSkillController.GetClassSkillAt(slot));
            await AddressableSkillSpawnService.PrewarmSkillRuntimeAssetsAsync(skillData);
            bool equipResult = currentHero.HeroSkillController.ReplaceSkillAt(slot, skillData, true);
            OnHeroSkillChanged?.Invoke(heroId);
            return equipResult;
        }

        /// <summary>uid → skill_id, dùng để resolve mảng equipped[hero_uid] (skill_uid) trả về từ server.</summary>
        private int GetSkillIdByUid(string skillUid)
        {
            if (string.IsNullOrEmpty(skillUid) || SkillList?.Owned == null) return 0;
            foreach (var s in SkillList.Owned)
                if (s.Uid == skillUid) return s.SkillId;
            return 0;
        }

        /// <summary>Resolve mảng skill_uid|null (5 slot) của 1 hero_uid trong SkillList.Equipped thành SkillDataSO.
        /// Trả null nếu hero_uid chưa có entry nào trong map (khác với "có entry nhưng toàn slot trống").</summary>
        private List<SkillDataSO> ResolveEquippedSkillData(string heroUid)
        {
            if (string.IsNullOrEmpty(heroUid) || SkillList?.Equipped == null) return null;
            if (!SkillList.Equipped.TryGetValue(heroUid, out var slotSkillUids) || slotSkillUids == null) return null;

            var result = new List<SkillDataSO>(slotSkillUids.Length);
            foreach (var skillUid in slotSkillUids)
            {
                int skillId = GetSkillIdByUid(skillUid);
                result.Add(skillId > 0 ? MasterDataCache.Instance.GetSkillDataById(skillId) : null);
            }
            return result;
        }

        /// <summary>
        /// Áp loadout skill từ server (SkillList.Equipped) lên 1 hero vừa spawn — gọi từ HeroActor.Init,
        /// nhận trực tiếp instance vì tại thời điểm đó hero CHƯA được đăng ký vào inBattleHeroes
        /// (PvEBattleController chỉ gán sau khi Init trả về) nên không thể lookup qua GetInBattleHeroActorById.
        /// Nếu hero_uid chưa có entry nào trong Equipped (chưa từng equip qua server), giữ nguyên skill
        /// mặc định bake sẵn trên prefab — không xoá trắng.
        /// </summary>
        public void ApplyServerLoadoutToHero(HeroActor actor, int heroId)
        {
            var resolved = ResolveEquippedSkillData(GetHeroUid(heroId));
            if (resolved == null) return;

            actor?.HeroSkillController.SetClassSkills(resolved);
        }

        /// <summary>
        /// Đối soát toàn bộ map equipped trả về từ skill/equip hoặc skill/unequip — ghi đè SkillList.Equipped
        /// và áp lại cho MỌI hero đang in-battle (không chỉ hero vừa thao tác), vì server có thể đã âm thầm
        /// gỡ skill khỏi 1 hero/slot khác (xem Docs/api_skill_equip.md mục 1 — "ghi đè im lặng").
        /// </summary>
        public void ReconcileEquippedFromServer(Dictionary<string, string[]> equippedMap)
        {
            if (SkillList == null) SkillList = new SkillListResponse();
            SkillList.Equipped = equippedMap;

            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                HeroActor actor = inBattleHeroes[i];
                if (actor?.HeroData == null) continue;

                string heroUid = GetHeroUid(actor.HeroData.Id);
                var resolved = ResolveEquippedSkillData(heroUid) ?? new List<SkillDataSO>();
                actor.HeroSkillController.SetClassSkills(resolved);
                OnHeroSkillChanged?.Invoke(actor.HeroData.Id);
            }
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