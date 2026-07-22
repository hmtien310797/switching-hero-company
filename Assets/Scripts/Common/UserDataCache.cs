using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SkillRemake;
using Immortal_Switch.Scripts.Sound;
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

    public partial class UserDataCache : Singleton<UserDataCache>
    {
        [Header("Init Config")]
        [SerializeField] private UserLevelConfigSO userLevelConfigSO;

        [Header("Debug")]
        [SerializeField] private bool enableLog = true;
        
        public ClassSkillUnlockData ClassSkillUnlock = new();
        //public HeroSkillLoadoutData HeroSkillLoadout = new();

        /// <summary>Display name của player — set bởi GameBootstrap từ player/me.</summary>
        public string DisplayName { get; set; }

        /// <summary>Uid của player — set bởi GameBootstrap từ player/me.</summary>
        public string Uid { get; set; }

        /// <summary>Đã link Google chưa — set bởi GameBootstrap từ player/me.</summary>
        public bool GoogleLinked { get; set; }

        /// <summary>Đã link Apple chưa — set bởi GameBootstrap từ player/me.</summary>
        public bool AppleLinked { get; set; }

        /// <summary>Account đã link bất kỳ social provider nào — quyết định hiển thị gg_linked/gg_no_link trong SettingView.</summary>
        public bool IsSocialLinked => GoogleLinked || AppleLinked;

        /// <summary>exp của player — set bởi GameBootstrap từ player/me.</summary>
        public long Exp { get; set; }

        /// <summary>Số lần player đã đổi tên — set bởi GameBootstrap từ player/me, cập nhật lại sau
        /// mỗi lần player/rename thành công. Dùng để tính giá đổi tên lần tới, xem RenameFeeConfig.</summary>
        public int RenameCount { get; set; }

        /// <summary>Đã nhận thưởng liên kết Google/Apple chưa — set bởi GameBootstrap từ player/me,
        /// cập nhật lại sau khi account/claim_link_reward thành công. Xem SettingManager.ClaimLinkRewardAsync.</summary>
        public bool LinkRewardClaimed { get; set; }

        /// <summary>Tổng exp của player — set bởi GameBootstrap từ player/me. Level tự tính từ exp qua UserLevelConfigSO.</summary>

        /// <summary>Hero inventory từ server — set bởi GameBootstrap từ player/me (owned + lineup + shards).</summary>
        public HeroInventory HeroList { get; set; }

        /// <summary>Summon state từ server — set bởi GameBootstrap sau login.</summary>
        public SummonStateResponse SummonState { get; set; }

        /// <summary>Skill list từ server — set bởi GameBootstrap sau login.</summary>
        [ShowInInspector]
        public SkillListResponse SkillList { get; set; }

        /// <summary>Weapon list từ server — set bởi GameBootstrap sau login.</summary>
        public WeaponListResponse WeaponList { get; set; }

        private const int BattleHeroSlotCountValue = 2;

        public int BattleHeroSlotCount => BattleHeroSlotCountValue;
        public List<int> InBattleHeroIdList { get; private set; } = new() { -1, -1 };
        public readonly HeroActor[] inBattleHeroes = new HeroActor[BattleHeroSlotCountValue];

        public event Action OnBattleLineupChanged;

        public event Action<int> OnHeroSkillChanged;
        public bool AutoSkill;

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        #region SKILL
        
        public void SetAutoSkill(bool isAutoSkill)
        {
            AutoSkill = isAutoSkill;
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                inBattleHeroes[i]?.SetAutoSkill(isAutoSkill);
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
                    list.Add(new SkillInstance { SkillId = entry.SkillId, Level = 1, Uid = entry.SkillUid });
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
        
        public int GetServerSkillLevel(int skillId)
        {
            if (SkillList?.Owned == null) return 0;
            foreach (var s in SkillList.Owned)
                if (s.SkillId == skillId) return s.Level > 0 ? s.Level : 1;
            return 0;
        }
        
        /// <summary>Trang bị skill vào slot trống đầu tiên (thật, không phải vị trí đã dồn).
        /// Trả về slot_index thật đã dùng để caller gửi đúng lên server (skill/equip), hoặc -1 nếu
        /// không trang bị được.</summary>
        public async UniTask<int> EquipSkill(int heroId, int skillId)
        {
            HeroActor currentHero = GetInBattleHeroActorById(heroId);

            if (!IsUnlocked(currentHero.HeroClass, skillId))
                return -1;

            SkillDataSO skillToEquip = DatabaseManager.Instance.GetSkillDataById(skillId);

            bool canEquip = currentHero.HeroSkillController.CanEquipClassSkill(skillToEquip, out int slotIndex);
            if (!canEquip)
                return -1;

            var prewarmTasks = new List<UniTask>
            {
                AddressableSkillSpawnService.PrewarmSkillRuntimeAssetsAsync(skillToEquip),
                SoundManager.Instance.PreloadSfxAsync(skillToEquip.GetAllNeedSound())
            };
            await UniTask.WhenAll(prewarmTasks);
            
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
                SoundManager.Instance.ReleaseCachedSfxCollection(equippedSkillData.GetAllNeedSound());
                AddressableSkillSpawnService.DisposeSkillComponent(equippedSkillData);
                OnHeroSkillChanged?.Invoke(heroId);
            }
            return equipResult;
        }
        
        public async UniTask<bool> ReplaceSkill(int heroId, int slot, int skillId)
        {
            HeroActor currentHero = GetInBattleHeroActorById(heroId);
            SkillDataSO skillData = DatabaseManager.Instance.GetSkillDataById(skillId);
            if (skillData == null || currentHero == null)
            {
                return false;
            }
            SoundManager.Instance.ReleaseCachedSfxCollection(currentHero.HeroSkillController.GetClassSkillAt(slot).GetAllNeedSound());
            AddressableSkillSpawnService.DisposeSkillComponent(currentHero.HeroSkillController.GetClassSkillAt(slot));
            
            var prewarmTasks = new List<UniTask>
            {
                AddressableSkillSpawnService.PrewarmSkillRuntimeAssetsAsync(skillData),
                SoundManager.Instance.PreloadSfxAsync(skillData.GetAllNeedSound())
            };
            await UniTask.WhenAll(prewarmTasks);

            bool equipResult = currentHero.HeroSkillController.ReplaceSkillAt(slot, skillData, true);
            OnHeroSkillChanged?.Invoke(heroId);
            return equipResult;
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
                result.Add(skillId > 0 ? DatabaseManager.Instance.GetSkillDataById(skillId) : null);
            }
            return result;
        }
        
        #endregion

        #region HERO
        public void GetPlayerDataFromServer(HeroInventory heroInventory, SkillListResponse skillListResponse, WeaponListResponse weaponListResponse)
        {
            HeroList = heroInventory;
            SkillList = skillListResponse;
            WeaponList = weaponListResponse;

            if (HeroList?.Lineup == null || HeroList.Owned == null)
                LogError("Hero inventory, lineup or owned hero list is null.");

            ApplyPendingLineupSyncIfAny();
            ApplyPendingSkillEquipSyncIfAny();

            var resolvedLineup = ResolveLineupHeroIds();
            SetBattleLineup(resolvedLineup);
            Log($"Resolved battle lineup: [{resolvedLineup[0]}, {resolvedLineup[1]}]");
        }

        /// <summary>Một lần swap hero giữa trận trước đó có thể chưa kịp gửi hero/set_lineup lên
        /// server (app bị kill/crash/Editor-Stop ngay sau khi swap — xem BattleHeroSessionController.
        /// SyncLineupToServerAsync). Nếu còn entry pending trên máy cho đúng account này, áp nó vào
        /// HeroList ngay (để lineup resolve đúng ngay từ đầu session) rồi gửi lại lên server nền.</summary>
        private void ApplyPendingLineupSyncIfAny()
        {
            string userId = NakamaClient.Instance?.Session?.UserId;
            var pendingLineup = PendingLineupSync.Load(userId);
            if (pendingLineup == null || HeroList == null) return;

            bool alreadyApplied = HeroList.Lineup != null
                && HeroList.Lineup.Length == pendingLineup.Length
                && HeroList.Lineup[0] == pendingLineup[0]
                && HeroList.Lineup[1] == pendingLineup[1];

            if (alreadyApplied)
            {
                PendingLineupSync.Clear(userId);
                return;
            }

            if (!PendingLineupResolvesToOwnedHeroes(pendingLineup))
            {
                // Entry không khớp uid nào trong Owned (hero đã đổi/mất từ lúc ghi entry này, hoặc
                // server đã từ chối request này ở phiên trước và entry chưa được dọn — xem
                // RetryPendingLineupSyncAsync). Bỏ, không ghi đè lineup ĐÚNG vừa lấy từ server bằng
                // dữ liệu rác cục bộ — nếu không, mọi lần login sau sẽ liên tục hiện lại lineup sai
                // (rơi về "2 hero đầu tiên trong Owned" do ResolveLineupHeroIds không resolve được).
                Log($"[UserDataCache] Discarding stale/invalid pending lineup sync (no uid matches an owned hero): [{pendingLineup[0]}, {pendingLineup[1]}]");
                PendingLineupSync.Clear(userId);
                return;
            }

            Log($"[UserDataCache] Found pending lineup sync from a previous session, reapplying: [{pendingLineup[0]}, {pendingLineup[1]}]");
            HeroList.Lineup = pendingLineup;
            RetryPendingLineupSyncAsync(userId, pendingLineup).Forget();
        }

        /// <summary>Một pending entry chỉ đáng tin nếu mọi uid không rỗng trong đó khớp với 1 hero
        /// đang thực sự sở hữu — nếu không, đó là dữ liệu cũ/rác (hero đã mất, tài khoản khác trên
        /// cùng máy, hoặc entry bị kẹt lại do server từ chối ở phiên trước).</summary>
        private bool PendingLineupResolvesToOwnedHeroes(string[] pendingLineup)
        {
            if (pendingLineup == null || HeroList?.Owned == null) return false;

            foreach (var heroUid in pendingLineup)
            {
                if (string.IsNullOrEmpty(heroUid)) continue;

                bool found = false;
                foreach (var ownedHero in HeroList.Owned)
                {
                    if (string.Equals(heroUid, ownedHero.Uid, StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) return false;
            }

            return true;
        }

        private async UniTask RetryPendingLineupSyncAsync(string userId, string[] pendingLineup)
        {
            try
            {
                var response = await NakamaClient.Instance.SetLineupAsync(pendingLineup);
                if (response != null && response.Updated)
                {
                    if (HeroList != null) HeroList.Lineup = response.Lineup;
                    PendingLineupSync.Clear(userId);
                }
                else
                {
                    // Server từ chối rõ ràng (không phải lỗi mạng) — retry thêm cũng sẽ luôn thất
                    // bại, giữ lại entry chỉ khiến nó tiếp tục ghi đè lineup đúng ở các lần login
                    // sau (xem ApplyPendingLineupSyncIfAny). Xoá để dừng vòng lặp.
                    Debug.LogWarning($"[UserDataCache] Server rejected pending hero/set_lineup (not a network error) — discarding stale entry: [{pendingLineup[0]}, {pendingLineup[1]}]");
                    PendingLineupSync.Clear(userId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataCache] Retry of pending hero/set_lineup failed, will retry again next launch: {e.Message}");
            }
        }

        /// <summary>Như ApplyPendingLineupSyncIfAny nhưng cho skill equip (xem SkillViewDataProvider.
        /// SyncEquipAsync/SyncUnequipAsync/SyncAutoEquipAsync — chỗ ghi entry này xuống local trước
        /// khi gửi RPC). Mỗi entry là target skillUid[] (theo slot) của 1 heroUid; nếu server chưa
        /// khớp, áp ngay vào SkillList.Equipped rồi gửi lại qua skill/auto_equip ở nền.</summary>
        private void ApplyPendingSkillEquipSyncIfAny()
        {
            string userId = NakamaClient.Instance?.Session?.UserId;
            var pendingMap = PendingSkillEquipSync.Load(userId);
            if (pendingMap == null || pendingMap.Count == 0) return;

            if (SkillList == null) SkillList = new SkillListResponse();
            if (SkillList.Equipped == null) SkillList.Equipped = new Dictionary<string, string[]>();

            foreach (var entry in pendingMap)
            {
                string heroUid = entry.Key;
                string[] pendingSkillUids = entry.Value;

                bool alreadyApplied = SkillList.Equipped.TryGetValue(heroUid, out var serverSlots)
                    && SkillUidsEqual(serverSlots, pendingSkillUids);

                if (alreadyApplied)
                {
                    PendingSkillEquipSync.Clear(userId, heroUid);
                    continue;
                }

                if (!PendingSkillEquipResolvesToOwned(heroUid, pendingSkillUids))
                {
                    // Entry không còn khớp hero/skill đang sở hữu (đã mất hero/skill từ lúc ghi entry
                    // này, hoặc server đã từ chối request này ở phiên trước và entry chưa được dọn —
                    // xem RetryPendingSkillEquipSyncAsync). Bỏ, không ghi đè loadout ĐÚNG vừa lấy từ
                    // server bằng dữ liệu rác cục bộ (cùng lỗi đã gặp ở ApplyPendingLineupSyncIfAny).
                    Log($"[UserDataCache] Discarding stale/invalid pending skill equip sync for heroUid={heroUid} (hero or skill uid no longer owned).");
                    PendingSkillEquipSync.Clear(userId, heroUid);
                    continue;
                }

                Log($"[UserDataCache] Found pending skill equip sync for heroUid={heroUid}, reapplying.");
                SkillList.Equipped[heroUid] = pendingSkillUids;
                RetryPendingSkillEquipSyncAsync(userId, heroUid, pendingSkillUids).Forget();
            }
        }

        private static bool SkillUidsEqual(string[] a, string[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            return true;
        }

        /// <summary>Như PendingLineupResolvesToOwnedHeroes nhưng cho skill equip — heroUid phải là
        /// hero đang sở hữu, và mọi skillUid không rỗng trong pendingSkillUids phải là skill đang
        /// sở hữu.</summary>
        private bool PendingSkillEquipResolvesToOwned(string heroUid, string[] pendingSkillUids)
        {
            if (string.IsNullOrEmpty(heroUid) || HeroList?.Owned == null || pendingSkillUids == null)
                return false;

            bool heroOwned = false;
            foreach (var ownedHero in HeroList.Owned)
            {
                if (string.Equals(heroUid, ownedHero.Uid, StringComparison.Ordinal))
                {
                    heroOwned = true;
                    break;
                }
            }
            if (!heroOwned) return false;

            if (SkillList?.Owned == null) return false;

            foreach (var skillUid in pendingSkillUids)
            {
                if (string.IsNullOrEmpty(skillUid)) continue;

                bool skillOwned = false;
                foreach (var ownedSkill in SkillList.Owned)
                {
                    if (string.Equals(skillUid, ownedSkill.Uid, StringComparison.Ordinal))
                    {
                        skillOwned = true;
                        break;
                    }
                }

                if (!skillOwned) return false;
            }

            return true;
        }

        private async UniTask RetryPendingSkillEquipSyncAsync(string userId, string heroUid, string[] pendingSkillUids)
        {
            try
            {
                var targetSkillUids = new List<string>();
                foreach (var skillUid in pendingSkillUids)
                    if (!string.IsNullOrEmpty(skillUid)) targetSkillUids.Add(skillUid);

                var response = await NakamaClient.Instance.SkillAutoEquipAsync(heroUid, targetSkillUids);
                if (response != null && response.Updated)
                {
                    ReconcileEquippedFromServer(response.Equipped);
                    PendingSkillEquipSync.Clear(userId, heroUid);
                }
                else
                {
                    // Server từ chối rõ ràng (không phải lỗi mạng) — retry thêm cũng sẽ luôn thất
                    // bại, giữ lại entry chỉ khiến nó tiếp tục ghi đè loadout đúng ở các lần login
                    // sau. Xoá để dừng vòng lặp.
                    Debug.LogWarning($"[UserDataCache] Server rejected pending skill equip for heroUid={heroUid} (not a network error) — discarding stale entry.");
                    PendingSkillEquipSync.Clear(userId, heroUid);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataCache] Retry of pending skill equip failed for heroUid={heroUid}, will retry again next launch: {e.Message}");
            }
        }

        /// <summary>Resolves HeroList.Lineup (uid array, the server's source of truth for the
        /// active team) against HeroList.Owned into hero_id slots — [-1, -1] (or per-slot -1)
        /// wherever a uid is empty or has no matching owned hero. Shared by the boot-time
        /// resolution above and BattleHeroSessionController.EnsureValidBattleLineup's recovery
        /// path, so a stale/unset InBattleHeroIdList re-derives the real lineup instead of
        /// falling back to an arbitrary pick from Owned.</summary>
        public List<int> ResolveLineupHeroIds()
        {
            var resolvedLineup = new List<int>(BattleHeroSlotCountValue);
            for (int i = 0; i < BattleHeroSlotCountValue; i++) resolvedLineup.Add(-1);

            if (HeroList?.Lineup == null || HeroList.Owned == null)
                return resolvedLineup;

            for (int lineupIndex = 0;
                 lineupIndex < HeroList.Lineup.Length && lineupIndex < BattleHeroSlotCountValue;
                 lineupIndex++)
            {
                string heroUid = HeroList.Lineup[lineupIndex];
                if (string.IsNullOrEmpty(heroUid))
                    continue;

                for (int ownedIndex = 0; ownedIndex < HeroList.Owned.Length; ownedIndex++)
                {
                    var ownedHero = HeroList.Owned[ownedIndex];
                    if (!string.Equals(heroUid, ownedHero.Uid, StringComparison.Ordinal))
                        continue;

                    resolvedLineup[lineupIndex] = ownedHero.HeroId;
                    break;
                }
            }

            return resolvedLineup;
        }

        public int GetBattleHeroIdAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= InBattleHeroIdList.Count)
                return -1;

            return InBattleHeroIdList[slotIndex];
        }

        public HeroActor GetInBattleHeroActorAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inBattleHeroes.Length)
                return null;

            return inBattleHeroes[slotIndex];
        }

        public void SetBattleLineup(IReadOnlyList<int> heroIds)
        {
            if (InBattleHeroIdList == null || InBattleHeroIdList.Count != BattleHeroSlotCountValue)
                InBattleHeroIdList = new List<int> { -1, -1 };

            for (int i = 0; i < BattleHeroSlotCountValue; i++)
            {
                int heroId = heroIds != null && i < heroIds.Count ? heroIds[i] : -1;
                InBattleHeroIdList[i] = heroId > 0 ? heroId : -1;
            }

            OnBattleLineupChanged?.Invoke();
        }

        public bool TrySetInBattleHeroActor(int slotIndex, HeroActor heroActor)
        {
            if (slotIndex < 0 || slotIndex >= inBattleHeroes.Length)
            {
                LogError($"Invalid battle hero slot: {slotIndex}");
                return false;
            }

            inBattleHeroes[slotIndex] = heroActor;
            return true;
        }

        public bool TryReplaceBattleHero(int slotIndex, int expectedHeroId, int newHeroId, HeroActor newHeroActor)
        {
            if (slotIndex < 0 || slotIndex >= BattleHeroSlotCountValue || newHeroId <= 0 || newHeroActor == null)
                return false;

            if (InBattleHeroIdList[slotIndex] != expectedHeroId)
                return false;

            if (ContainsBattleHero(newHeroId))
                return false;

            InBattleHeroIdList[slotIndex] = newHeroId;
            inBattleHeroes[slotIndex] = newHeroActor;
            OnBattleLineupChanged?.Invoke();
            return true;
        }

        public bool ContainsBattleHero(int heroId)
        {
            return heroId > 0 && InBattleHeroIdList.Contains(heroId);
        }

        public int FindBattleHeroSlot(int heroId)
        {
            return heroId > 0 ? InBattleHeroIdList.IndexOf(heroId) : -1;
        }

        public HeroActor GetInBattleHeroActorById(int heroId)
        {
            if (heroId <= 0)
                return null;

            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                HeroActor currentHero = inBattleHeroes[i];
                if (currentHero == null || currentHero.HeroData == null)
                    continue;

                if (currentHero.HeroData.Id == heroId)
                    return currentHero;
            }

            return null;
        }
        
        public HeroActor TryGetActiveHeroByClass(HeroClass heroClass)
        {
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                HeroActor hero = inBattleHeroes[i];

                if (hero == null || hero.IsDead)
                    continue;

                if (hero.HeroClass == heroClass)
                    return hero;
            }

            return null;
        }

        public bool HasActiveHeroOfClass(HeroClass heroClass)
        {
            return TryGetActiveHeroByClass(heroClass) != null;
        }

        #endregion

        #region CLASS UNLOCK

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

        /// <summary>Refresh HeroList.Owned từ hero/list — HeroList chỉ nạp 1 lần lúc bootstrap (player/me)
        /// nên hero vừa summon trong session hiện tại chưa có uid trong cache cho tới khi gọi hàm này.</summary>
        public async UniTask<bool> RefreshHeroListFromServerAsync()
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn) return false;

            try
            {
                var heroListResponse = await NakamaClient.Instance.GetHeroListAsync();
                if (heroListResponse?.Owned == null) return false;

                if (HeroList == null)
                    HeroList = new HeroInventory();

                HeroList.Owned = heroListResponse.Owned;
                return true;
            }
            catch (Exception e)
            {
                LogError($"RefreshHeroListFromServerAsync failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Như GetHeroUid, nhưng nếu cache local chưa có (hero vừa summon trong session này)
        /// thì refresh hero/list rồi thử lại 1 lần trước khi trả null.</summary>
        public async UniTask<string> GetHeroUidAsync(int heroId)
        {
            string uid = GetHeroUid(heroId);
            if (uid != null) return uid;

            if (await RefreshHeroListFromServerAsync())
                uid = GetHeroUid(heroId);

            return uid;
        }

        #endregion

        #region HERO LOADOUT

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