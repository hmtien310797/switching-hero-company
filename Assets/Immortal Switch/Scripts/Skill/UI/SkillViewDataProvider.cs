using System;
using System.Collections.Generic;
using System.Linq;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Nakama;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillViewHeroContext
    {
        public int HeroId;
        public HeroClass HeroClass;
        public Sprite HeroIcon;
        public List<int> EquippedSkillIds = new();
        public HeroActor RuntimeController;
    }

    public class SkillViewSkillState
    {
        public SkillDataSO SkillData;
        public int SkillId;
        public TierSkill TierSkill;
        public int Level;
        public int CurrentShard;
        public int RequiredShard;
        public bool IsOwned;
        public bool IsEquipped;
        public int EquippedSlotIndex = -1;
    }

    public class SkillViewDataProvider : Singleton<SkillViewDataProvider>
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private Dictionary<int, SkillDataSO> skillCache;
        private Dictionary<HeroClass, List<SkillDataSO>> poolLookup = new();
        private List<SkillDataSO> allSkills = new();

        private SpriteAtlas heroSpriteAtlas;
        private const string HeroSpriteAtlasKey = "hero_sprite_atlas";
        public event Action OnDataChanged;
        private UserDataCache userDataCache;

        public override async UniTask InitializeAsync()
        {
            allSkills = MasterDataCache.Instance.GetAllSkillData();
            BuildPoolLookup();
            BuildCacheIfNeeded();
            userDataCache = UserDataCache.Instance;
            heroSpriteAtlas = await AddressableSpriteAtlasService.AcquireAtlasAsync(HeroSpriteAtlasKey);
        }

        private void OnEnable()
        {
            GameEventManager.Subscribe(GameEvents.OnActiveLineupChanged, HandleBattleLineupChanged);

            if (UserDataCache.Instance != null)
                UserDataCache.Instance.OnHeroSkillChanged += HandleHeroSkillChanged;
        }

        private void OnDisable()
        {
            GameEventManager.Unsubscribe(GameEvents.OnActiveLineupChanged, HandleBattleLineupChanged);

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

        private void BuildPoolLookup()
        {
            poolLookup.Clear();

            if (allSkills == null || allSkills.Count == 0)
                return;

            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillDataSO skill = allSkills[i];

                if (skill == null)
                    continue;
                
                if (skill.OwnerType != SkillOwnerType.ClassSkill)
                    continue;

                HeroClass heroClass = skill.SkillClass;

                if (!poolLookup.TryGetValue(heroClass, out List<SkillDataSO> skillPool))
                {
                    skillPool = new List<SkillDataSO>();
                    poolLookup.Add(heroClass, skillPool);
                }

                skillPool.Add(skill);
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
            bool result = PvEBattleController.Instance.HasActiveHeroOfClass(heroClass);
            Log($"HasAssignedHero -> class={heroClass}, result={result}");
            return result;
        }

        public List<SkillViewHeroContext> GetAssignedHeroes()
        {
            var result = new List<SkillViewHeroContext>();

            var activeHeroes = UserDataCache.Instance.inBattleHeroes;

            foreach (var hero in activeHeroes)
            {
                if (hero == null)
                {
                    LogWarning("GetAssignedHeroes found null hero.");
                    continue;
                }

                int heroId = hero.GetHeroId();
                var equipped = UserDataCache.Instance != null
                    ? UserDataCache.Instance.GetEquippedClassSkillIds(heroId)
                    : new List<int>();

                result.Add(new SkillViewHeroContext
                {
                    HeroId = heroId,
                    HeroClass = hero.HeroClass,
                    HeroIcon = heroSpriteAtlas.GetSprite(hero.HeroData.HeroIconKey),
                    EquippedSkillIds = equipped,
                    RuntimeController = hero
                });

                Log($"GetAssignedHeroes -> heroId={heroId}, class={hero.HeroClass}, equipped=[{string.Join(",", equipped)}]");
            }

            return result;
        }

        public SkillViewHeroContext GetAssignedHeroByClass(HeroClass heroClass)
        {
            HeroActor heroController = PvEBattleController.Instance.TryGetActiveHeroByClass(heroClass);
            if (heroController == null)
            {
                LogWarning($"GetAssignedHeroByClass -> no active hero for class={heroClass}");
                return null;
            }

            int heroId = heroController.GetHeroId();
            var equipped = UserDataCache.Instance != null
                ? UserDataCache.Instance.GetEquippedClassSkillIds(heroId)
                : new List<int>();

            Log($"GetAssignedHeroByClass -> class={heroClass}, heroId={heroId}, equipped=[{string.Join(",", equipped)}]");

            return new SkillViewHeroContext
            {
                HeroId = heroId,
                HeroClass = heroController.HeroClass,
                HeroIcon = heroSpriteAtlas.GetSprite(heroController.HeroData.HeroIconKey),
                EquippedSkillIds = equipped,
                RuntimeController = heroController
            };
        }

        public List<SkillDataSO> GetClassPool(HeroClass heroClass)
        {
            if (poolLookup == null)
                BuildPoolLookup();

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

            int level = userDataCache.GetServerSkillLevel(skillData.SkillId);
            int currentShard = GetServerSkillShard(skillData.SkillId);
            bool isOwned = IsServerSkillOwned(skillData.SkillId) || currentShard > 0;

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
                IsOwned = isOwned,
                TierSkill = skillData.SkillTier
            };

            state.EquippedSlotIndex = equippedIds.FindIndex(x => x == state.SkillId);
            state.IsEquipped = state.EquippedSlotIndex >= 0;

            return state;
        }
        
        public SkillViewSkillState BuildSkillStateForNotInBattleHeroTab(SkillDataSO skillData)
        {
            if (skillData == null)
                return null;

            int level = userDataCache.GetServerSkillLevel(skillData.SkillId);
            int currentShard = GetServerSkillShard(skillData.SkillId);
            bool isOwned = IsServerSkillOwned(skillData.SkillId) || currentShard > 0;

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
                IsOwned = isOwned,
                TierSkill = skillData.SkillTier
            };
            
            state.IsEquipped = state.EquippedSlotIndex >= 0;
            return state;
        }

        private int GetServerSkillShard(int skillId)
        {
            var skillList = UserDataCache.Instance?.SkillList;
            if (skillList?.Shards == null) return 0;
            return skillList.Shards.TryGetValue(skillId.ToString(), out int v) ? v : 0;
        }

        private bool IsServerSkillOwned(int skillId)
        {
            var skillList = UserDataCache.Instance?.SkillList;
            if (skillList?.Owned == null) return false;
            foreach (var s in skillList.Owned)
                if (s.SkillId == skillId) return true;
            return false;
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
                    return state?.TierSkill ?? TierSkill.B;
                }).ToList();

            Log($"GetSortedPoolForHero -> heroId={heroContext.HeroId}, class={heroContext.HeroClass}, count={sorted.Count}");
            return sorted;
        }
        
        public List<SkillDataSO> GetSortedPoolForNotInBattleHero(HeroClass heroClass)
        {
            var pool = GetClassPool(heroClass);
            
            var sorted = pool
                .OrderByDescending(x =>
                {
                    var state = BuildSkillStateForNotInBattleHeroTab(x);
                    return state?.TierSkill ?? TierSkill.B;
                }).ToList();
            
            return sorted;
        }

        public async UniTask<bool> TryEquipSkillToHero(SkillViewHeroContext heroContext, int skillId)
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

            int slotIndex = heroContext.EquippedSkillIds?.Count ?? 0;
            bool success = await UserDataCache.Instance.EquipSkill(heroContext.HeroId, skillId);
            if (!success)
            {
                LogWarning($"TryEquipSkillToHero failed -> heroId={heroContext.HeroId}, skillId={skillId}");
                return false;
            }

            heroContext.RuntimeController?.RefreshSelectedSkillsRuntime();
            OnDataChanged?.Invoke();

            SyncEquipAsync(heroContext.HeroId, skillId, slotIndex).Forget();

            Log($"TryEquipSkillToHero success -> heroId={heroContext.HeroId}, skillId={skillId}, slotIndex={slotIndex}");
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

            bool success = UserDataCache.Instance.UnequipSkill(heroContext.HeroId, skillId);
            if (!success)
            {
                LogWarning($"TryUnequipSkillFromHero failed -> heroId={heroContext.HeroId}, skillId={skillId}");
                return false;
            }

            heroContext.RuntimeController?.RefreshSelectedSkillsRuntime();
            OnDataChanged?.Invoke();

            SyncUnequipAsync(skillId).Forget();

            Log($"TryUnequipSkillFromHero success -> heroId={heroContext.HeroId}, skillId={skillId}");
            return true;
        }

        public async UniTask<bool> TryReplaceSkillOnHero(SkillViewHeroContext heroContext, int slotIndex, int newSkillId)
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

            bool success = await UserDataCache.Instance.ReplaceSkill(heroContext.HeroId, slotIndex, newSkillId);
            if (!success)
            {
                LogWarning($"TryReplaceSkillOnHero failed -> heroId={heroContext.HeroId}, slotIndex={slotIndex}, newSkillId={newSkillId}");
                return false;
            }

            heroContext.RuntimeController?.RefreshSelectedSkillsRuntime();
            OnDataChanged?.Invoke();

            SyncEquipAsync(heroContext.HeroId, newSkillId, slotIndex).Forget();

            Log($"TryReplaceSkillOnHero success -> heroId={heroContext.HeroId}, slotIndex={slotIndex}, newSkillId={newSkillId}");
            return true;
        }

        private async UniTaskVoid SyncEquipAsync(int heroId, int skillId, int slotIndex)
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn) return;

            string heroUid  = UserDataCache.Instance?.GetHeroUid(heroId);
            string skillUid = UserDataCache.Instance?.GetSkillUid(skillId);
            if (heroUid == null || skillUid == null)
            {
                LogError($"SyncEquipAsync aborted — heroUid or skillUid not found. heroId={heroId}, skillId={skillId}");
                return;
            }

            try
            {
                await NakamaClient.Instance.SkillEquipAsync(heroUid, slotIndex, skillUid);
                Log($"SyncEquipAsync done — heroUid={heroUid}, skillUid={skillUid}, slot={slotIndex}");
            }
            catch (ApiResponseException ex) when (ex.StatusCode == 16)
            {
                LogWarning($"SyncEquipAsync session expired (UNAUTHENTICATED).");
            }
            catch (ApiResponseException ex)
            {
                LogError($"SyncEquipAsync failed: {ex.StatusCode} {ex.Message}");
            }
        }

        private async UniTaskVoid SyncUnequipAsync(int skillId)
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn) return;

            string skillUid = UserDataCache.Instance?.GetSkillUid(skillId);
            if (skillUid == null)
            {
                LogError($"SyncUnequipAsync aborted — skillUid not found. skillId={skillId}");
                return;
            }

            try
            {
                await NakamaClient.Instance.SkillUnequipAsync(skillUid);
                Log($"SyncUnequipAsync done — skillUid={skillUid}");
            }
            catch (ApiResponseException ex) when (ex.StatusCode == 16)
            {
                LogWarning($"SyncUnequipAsync session expired (UNAUTHENTICATED).");
            }
            catch (ApiResponseException ex)
            {
                LogError($"SyncUnequipAsync failed: {ex.StatusCode} {ex.Message}");
            }
        }
    }
}