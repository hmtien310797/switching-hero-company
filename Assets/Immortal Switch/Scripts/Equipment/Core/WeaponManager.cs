using System;
using System.Collections.Generic;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Equipment.Runtime;
using Immortal_Switch.Scripts.Equipment.Services;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Core
{
    public class WeaponManager : Singleton<WeaponManager>
    {

        private WeaponSaveData saveData;
        private WeaponInventoryService inventory;
        private WeaponEquipService equip;
        private WeaponAutoEquipService autoEquip;
        private WeaponUpgradeService upgrade;
        private WeaponFuseService fuse;

        private WeaponDatabaseSO database;
        public WeaponSaveData SaveData => saveData;
        public WeaponInventoryService Inventory => inventory;
        public WeaponEquipService Equip => equip;
        public WeaponAutoEquipService AutoEquipService => autoEquip;
        public WeaponUpgradeService Upgrade => upgrade;
        public WeaponFuseService Fuse => fuse;

        public event Action<int> OnHeroWeaponChanged;
        public event Action<int> OnStandardWeaponStateChanged;
        public event Action<int> OnExclusiveWeaponStateChanged;
        
        private UserDataCache userDataCache;

        protected override void Awake()
        {
            base.Awake();
            Load();
            userDataCache = UserDataCache.Instance;
            BuildServices();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void BuildServices()
        {
            inventory = new WeaponInventoryService(database, saveData);
            equip = new WeaponEquipService(database, inventory);
            autoEquip = new WeaponAutoEquipService(database, inventory, equip);
            upgrade = new WeaponUpgradeService(database, inventory);
            fuse = new WeaponFuseService(database, inventory);
        }


        private void Load()
        {
            database = DatabaseManager.Instance.GetWeaponDatabase();
            saveData = new WeaponSaveData();
        }

        /// <summary>
        /// Sync toàn bộ weapon collection từ server (weapon/list, player/me.weapons) — nguồn sự thật.
        /// Ghi đè toàn bộ state local (không merge) — weapon nào unlock ở local (qua debug tool) mà
        /// server không có sẽ tự "khoá lại" sau lần sync này, tránh leak state cheat/account khác
        /// (xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 3 &amp; 9).
        /// </summary>
        public void SyncFromServer(WeaponListResponse response, bool autoSave = true)
        {
            if (response == null)
                return;

            saveData.StandardWeapons.Clear();
            if (response.Standard != null)
            {
                foreach (var dto in response.Standard)
                {
                    saveData.StandardWeapons.Add(new StandardWeaponState
                    {
                        WeaponId = dto.WeaponId,
                        IsUnlocked = dto.IsUnlocked,
                        Level = Mathf.Max(1, dto.Level),
                        LimitBreakStage = Mathf.Max(0, dto.LimitBreakStage),
                        CurrentShard = Mathf.Max(0, dto.Shard)
                    });
                }
            }

            saveData.ExclusiveWeapons.Clear();
            if (response.Exclusive != null)
            {
                foreach (var dto in response.Exclusive)
                {
                    saveData.ExclusiveWeapons.Add(new ExclusiveWeaponState
                    {
                        ExclusiveWeaponId = dto.ExclusiveWeaponId,
                        HeroId = dto.HeroId,
                        IsUnlocked = dto.IsUnlocked,
                        Level = Mathf.Max(1, dto.Level),
                        LimitBreakStage = Mathf.Max(0, dto.LimitBreakStage),
                        CurrentShard = Mathf.Max(0, dto.Shard),
                        CurrentStar = Mathf.Max(1, dto.Star)
                    });
                }
            }

            saveData.HeroEquips.Clear();
            if (response.HeroEquip != null)
            {
                foreach (var dto in response.HeroEquip)
                {
                    saveData.HeroEquips.Add(new HeroWeaponEquipEntry
                    {
                        HeroId = dto.HeroId,
                        EquippedStandardWeaponId = dto.EquippedStandardWeaponId,
                        EquippedExclusiveWeaponId = dto.EquippedExclusiveWeaponId,
                        UseExclusive = dto.UseExclusive
                    });
                }
            }
        }

        /// <summary>Gọi weapon/list rồi apply ngay qua <see cref="SyncFromServer"/> — dùng khi cần re-sync (mở màn hình Trang Bị).</summary>
        public async UniTask<bool> SyncFromServerAsync()
        {
            WeaponListResponse response;
            try
            {
                response = await NakamaClient.Instance.GetWeaponListAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/list RPC failed: {e.Message}");
                return false;
            }

            if (response == null)
                return false;

            SyncFromServer(response);
            return true;
        }

        public void UnlockStandard(int weaponId, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
            {
                state.IsUnlocked = true;
                state.Level = Mathf.Max(1, state.Level);
            }

            NotifyStandardWeaponChanged(weaponId);
        }

        public void AddStandardShard(int weaponId, int amount, bool autoSave = true)
        {
            if (amount <= 0)
                return;

            var state = inventory.GetOrCreateStandardState(weaponId);
            state.CurrentShard += amount;

            NotifyStandardWeaponChanged(weaponId);
        }

        public void UnlockExclusive(int heroId, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
            {
                state.IsUnlocked = true;
                state.Level = Mathf.Max(1, state.Level);
            }
            
            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }
        
        public bool AddStandardShardFromSummon(
            int weaponId,
            int amount,
            out bool isNewWeapon,
            out int totalShardAfter,
            bool autoSave = true)
        {
            isNewWeapon = false;
            totalShardAfter = 0;

            if (weaponId <= 0 || amount <= 0)
                return false;

            var state = inventory.GetOrCreateStandardState(weaponId);
            if (state == null)
                return false;

            isNewWeapon = !state.IsUnlocked;

            // Rule weapon summon giống skill:
            // roll ra shard là unlock luôn, shard vẫn giữ nguyên.
            state.IsUnlocked = true;
            state.Level = Mathf.Max(1, state.Level);
            state.CurrentShard = Mathf.Max(0, state.CurrentShard + amount);

            totalShardAfter = state.CurrentShard;

            NotifyStandardWeaponChanged(weaponId);
            return true;
        }

        /// <summary>
        /// Apply 1 weapon/summon RPC roll result locally — weaponId is already resolved server-side
        /// to the correct target (see be summon.js addWeaponOrShardByClass: duplicate-of-lower-tier
        /// rolls redirect to the currently held weapon_id, rolls ahead jump the held record forward).
        /// Each class chain has at most 1 local <see cref="StandardWeaponState"/> too (same identity
        /// rule as the server's owned[] — weapon/fuse mutates in place, never duplicates), so this
        /// must never create a 2nd parallel local state for a class already unlocked. Returns the
        /// shard balance after applying, for the summon result UI (was previously hard-coded to 0
        /// since nothing actually synced summoned weapons into local inventory until the next
        /// weapon/list call).
        /// </summary>
        public int ApplyStandardSummonEntry(int weaponId, int shardGained, bool autoSave = false)
        {
            var rolledDef = database.GetStandard(weaponId);
            if (rolledDef == null)
                return 0;

            StandardWeaponState existing = null;
            foreach (var state in saveData.StandardWeapons)
            {
                if (!state.IsUnlocked) continue;
                var def = database.GetStandard(state.WeaponId);
                if (def != null && def.WeaponClass == rolledDef.WeaponClass)
                {
                    existing = state;
                    break;
                }
            }

            StandardWeaponState target;

            if (existing == null)
            {
                target = inventory.GetOrCreateStandardState(weaponId);
                target.IsUnlocked = true;
                target.Level = Mathf.Max(1, target.Level);
            }
            else if (weaponId <= existing.WeaponId)
            {
                target = existing;
            }
            else
            {
                // Roll landed ahead of the player's current chain position — jump the existing
                // record forward (free upgrade), same mutation weapon/fuse already does.
                existing.WeaponId = weaponId;
                target = existing;
            }

            target.CurrentShard = Mathf.Max(0, target.CurrentShard + shardGained);

            NotifyStandardWeaponChanged(target.WeaponId);
            return target.CurrentShard;
        }

        public void AddExclusiveShard(int heroId, int amount, bool autoSave = true)
        {
            if (amount <= 0)
                return;

            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.CurrentShard += amount;

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        /// <summary>
        /// Equip standard weapon cho hero qua RPC weapon/equip. Server là nguồn sự thật — validate
        /// class/ownership + set use_exclusive=false, client chỉ apply lại kết quả trả về
        /// (xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 3).
        /// </summary>
        public async UniTask<bool> EquipStandardAsync(int heroId, int weaponId, bool autoSave = true)
        {
            WeaponEquipResponse response;
            try
            {
                response = await NakamaClient.Instance.EquipWeaponAsync(heroId, "standard", weaponId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/equip RPC failed for heroId={heroId}, weaponId={weaponId}: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[WeaponManager] weapon/equip rejected for heroId={heroId}, weaponId={weaponId}: {response.Error}");
                return false;
            }

            ApplyEquipResult(response);

            NotifyHeroWeaponChanged(heroId);
            return true;
        }

        /// <summary>Equip exclusive weapon của hero qua RPC weapon/equip (server tự suy weapon từ heroId).</summary>
        public async UniTask<bool> EquipExclusiveAsync(int heroId, bool autoSave = true)
        {
            WeaponEquipResponse response;
            try
            {
                response = await NakamaClient.Instance.EquipWeaponAsync(heroId, "exclusive");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/equip RPC failed for heroId={heroId} (exclusive): {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[WeaponManager] weapon/equip rejected for heroId={heroId} (exclusive): {response.Error}");
                return false;
            }

            ApplyEquipResult(response);

            NotifyHeroWeaponChanged(heroId);
            return true;
        }

        private void ApplyEquipResult(WeaponEquipResponse response)
        {
            var equipEntry = inventory.GetOrCreateHeroEquip(response.HeroId);
            equipEntry.EquippedStandardWeaponId = response.EquippedStandardWeaponId;
            equipEntry.EquippedExclusiveWeaponId = response.EquippedExclusiveWeaponId;
            equipEntry.UseExclusive = response.UseExclusive;
        }

        /// <summary>
        /// Tự động chọn vũ khí tốt nhất đang sở hữu rồi equip qua cùng RPC weapon/equip với nút
        /// Equip thủ công (server là nguồn sự thật) — phần "chọn vũ khí nào" vẫn tính local
        /// (<see cref="WeaponAutoEquipService.GetBestStandardForClass"/>) vì chỉ xét trong số đã
        /// sở hữu/đã sync, không cần round-trip riêng. Trước đây gọi local-only qua
        /// <c>WeaponEquipService</c> nên lựa chọn auto-equip không persist lên server và bị
        /// <see cref="SyncFromServer"/> ghi đè mất ở lần sync kế tiếp — nay đã fix.
        /// </summary>
        public async UniTask<bool> TryAutoEquipAsync(int heroId, HeroClass heroClass, bool autoSave = true)
        {
            var exclusive = database.GetExclusiveByHeroId(heroId);
            if (exclusive != null)
            {
                var exState = inventory.GetOrCreateExclusiveState(exclusive.ExclusiveWeaponId, heroId);
                if (exState.IsUnlocked)
                    return await EquipExclusiveAsync(heroId, autoSave);
            }

            var best = autoEquip.GetBestStandardForClass(heroClass);
            if (best == null)
                return false;

            return await EquipStandardAsync(heroId, best.WeaponId, autoSave);
        }

        public async UniTask<bool> TryAutoEquipForHeroesAsync(IEnumerable<HeroActor> heroes, bool autoSave = true)
        {
            if (heroes == null)
                return false;

            bool changedAny = false;

            foreach (var hero in heroes)
            {
                if (hero == null || !hero.gameObject.activeInHierarchy)
                    continue;

                bool changed = await TryAutoEquipAsync(hero.GetHeroId(), hero.HeroClass, autoSave);
                changedAny |= changed;
            }

            return changedAny;
        }
        
        /// <summary>
        /// Fuse toàn bộ vũ khí standard đang sở hữu — mỗi vũ khí đi tiếp theo chuỗi (qua RPC weapon/fuse,
        /// xem TryFuseStandardAsync) cho tới khi không đủ shard hoặc gặp max node.
        /// </summary>
        public async UniTask<WeaponFuseAllResult> TryFuseAllStandardWeaponsAsync()
        {
            var result = new WeaponFuseAllResult();

            if (database == null || inventory == null || database.StandardWeapons == null)
                return result;

            var ownedWeaponIds = new List<int>();
            foreach (var def in database.StandardWeapons)
            {
                if (def == null) continue;

                var state = inventory.GetOrCreateStandardState(def.WeaponId);
                if (state != null && state.IsUnlocked)
                    ownedWeaponIds.Add(def.WeaponId);
            }

            bool changedAny = false;

            foreach (var startWeaponId in ownedWeaponIds)
            {
                int currentWeaponId = startWeaponId;

                while (true)
                {
                    var fuseResult = await TryFuseStandardAsync(currentWeaponId, false);
                    if (!fuseResult.Success)
                        break;

                    changedAny = true;

                    if (fuseResult.TargetStandardWeaponId > 0)
                    {
                        AddStandardReward(result, fuseResult.TargetStandardWeaponId, 1);
                        currentWeaponId = fuseResult.TargetStandardWeaponId;
                    }
                    else if (fuseResult.TargetExclusiveWeaponId > 0)
                    {
                        AddExclusiveReward(result, fuseResult.TargetExclusiveWeaponId, 1);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (changedAny)
            {
                NotifyAllRelevantWeaponChangesAfterFuseAll(result);
            }

            return result;
        }

        private void AddStandardReward(WeaponFuseAllResult result, int weaponId, int amount)
        {
            if (result == null || weaponId <= 0 || amount <= 0)
                return;

            var def = database.GetStandard(weaponId);
            if (def == null)
                return;

            var existing = result.Rewards.Find(x => !x.IsExclusive && x.WeaponId == weaponId);
            if (existing != null)
            {
                existing.Amount += amount;
                return;
            }

            result.Rewards.Add(new WeaponFuseAllRewardEntry
            {
                WeaponId = def.WeaponId,
                WeaponName = def.WeaponName,
                HeroClass = def.WeaponClass,
                Tier = def.Tier,
                Star = def.Star,
                IsExclusive = false,
                Amount = amount,
                Icon = def.Icon
            });
        }
        
        private void AddExclusiveReward(WeaponFuseAllResult result, int exclusiveWeaponId, int amount)
        {
            if (result == null || exclusiveWeaponId <= 0 || amount <= 0)
                return;

            var def = database.GetExclusive(exclusiveWeaponId);
            if (def == null)
                return;

            var existing = result.Rewards.Find(x => x.IsExclusive && x.WeaponId == exclusiveWeaponId);
            if (existing != null)
            {
                existing.Amount += amount;
                return;
            }

            result.Rewards.Add(new WeaponFuseAllRewardEntry
            {
                WeaponId = def.ExclusiveWeaponId,
                WeaponName = def.WeaponName,
                HeroClass = def.HeroClass,
                Tier = WeaponTier.SS,
                Star = def.StartingStar,
                IsExclusive = true,
                Amount = amount,
                Icon = def.Icon
            });
        }
        
        private void NotifyAllRelevantWeaponChangesAfterFuseAll(WeaponFuseAllResult result)
        {
            if (result == null || result.Rewards == null)
                return;

            // refresh runtime heroes
            if (PvEBattleController.Instance != null)
            {
                var activeHeroes = userDataCache.inBattleHeroes;
                for (int i = 0; i < activeHeroes.Length; i++)
                {
                    var hero = activeHeroes[i];
                    if (hero == null)
                        continue;

                    NotifyHeroWeaponChanged(hero.GetHeroId());
                }
            }

            // refresh changed weapon states for UI/data listeners
            for (int i = 0; i < result.Rewards.Count; i++)
            {
                var reward = result.Rewards[i];
                if (reward == null)
                    continue;

                if (reward.IsExclusive)
                {
                    var exDef = database.GetExclusive(reward.WeaponId);
                    if (exDef != null)
                        NotifyExclusiveWeaponChanged(exDef.ExclusiveWeaponId, exDef.HeroId);
                }
                else
                {
                    NotifyStandardWeaponChanged(reward.WeaponId);
                }
            }
        }
        
        /// <summary>
        /// Fuse liên tiếp tối đa <paramref name="count"/> lần qua RPC weapon/fuse. Mỗi lần thành công
        /// server đổi identity sang weapon_id kế tiếp, nên lần fuse sau sẽ tiếp tục từ id mới đó.
        /// </summary>
        public async UniTask<WeaponFuseAllResult> TryFusionForSelectedWeaponAsync(int weaponId, int count)
        {
            var uiResult = new WeaponFuseAllResult();

            if (weaponId <= 0 || count <= 0)
                return uiResult;

            int currentWeaponId = weaponId;
            for (int i = 0; i < count; i++)
            {
                var result = await TryFuseStandardAsync(currentWeaponId, false);
                if (!result.Success)
                    break;

                if (result.TargetStandardWeaponId > 0)
                {
                    AddStandardReward(uiResult, result.TargetStandardWeaponId, 1);
                    currentWeaponId = result.TargetStandardWeaponId;
                }
                else if (result.TargetExclusiveWeaponId > 0)
                {
                    AddExclusiveReward(uiResult, result.TargetExclusiveWeaponId, 1);
                    break;
                }
                else
                {
                    break;
                }
            }

            if (uiResult.HasAnyReward)
            {
                NotifyAllRelevantWeaponChangesAfterFuseAll(uiResult);
            }

            return uiResult;
        }

        /// <summary>
        /// Level up standard weapon qua RPC weapon/upgrade. Server giờ validate/trừ thật
        /// weapon_ore qua bag.items và trả về stone_balance tuyệt đối sau khi trừ — client chỉ
        /// Set() lại CurrencyManager theo đúng số đó, không tự trừ cục bộ nữa (tránh lệch số).
        /// </summary>
        public async UniTask<bool> TryLevelUpStandardAsync(int weaponId, bool autoSave = true)
        {
            WeaponUpgradeResponse response;
            try
            {
                response = await NakamaClient.Instance.UpgradeWeaponAsync("standard", weaponId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/upgrade RPC failed for weaponId={weaponId}: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[WeaponManager] weapon/upgrade rejected for weaponId={weaponId}: {response.Error}");
                return false;
            }

            inventory.GetOrCreateStandardState(weaponId).Level = response.NewLevel;
            CurrencyManager.Instance.Set(CurrencyType.weapon_ore, response.StoneBalance);

            NotifyStandardWeaponChanged(weaponId);
            return true;
        }

        /// <summary>Level up exclusive weapon của hero qua RPC weapon/upgrade (server tự suy weapon từ heroId).</summary>
        public async UniTask<bool> TryLevelUpExclusiveAsync(int heroId, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return false;

            WeaponUpgradeResponse response;
            try
            {
                response = await NakamaClient.Instance.UpgradeWeaponAsync("exclusive", 0, heroId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/upgrade RPC failed for heroId={heroId} (exclusive): {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[WeaponManager] weapon/upgrade rejected for heroId={heroId} (exclusive): {response.Error}");
                return false;
            }

            inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId).Level = response.NewLevel;
            CurrencyManager.Instance.Set(CurrencyType.weapon_ore, response.StoneBalance);

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
            return true;
        }

        /// <summary>
        /// Limit break standard weapon qua RPC weapon/limitbreak. RNG pass/fail nằm server-side —
        /// client chỉ apply lại stage trả về (xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 5).
        /// </summary>
        public async UniTask<WeaponLimitBreakResult> TryLimitBreakStandardAsync(int weaponId, bool autoSave = true)
        {
            WeaponLimitBreakResponse response;
            try
            {
                response = await NakamaClient.Instance.LimitBreakWeaponAsync("standard", weaponId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/limitbreak RPC failed for weaponId={weaponId}: {e.Message}");
                return WeaponLimitBreakResult.Invalid;
            }

            if (response == null)
                return WeaponLimitBreakResult.Invalid;

            if (!response.Success)
            {
                Debug.LogWarning($"[WeaponManager] weapon/limitbreak rejected for weaponId={weaponId}: {response.Error}");
                return MapLimitBreakError(response.Error);
            }

            inventory.GetOrCreateStandardState(weaponId).LimitBreakStage = response.NewStage;
            SpendStoneForLimitBreak(response.StoneCost, CurrencyTransactionReason.LimitBreakStandardWeapon);

            NotifyStandardWeaponChanged(weaponId);

            return response.Result == "success" ? WeaponLimitBreakResult.Success : WeaponLimitBreakResult.Failed;
        }

        /// <summary>Limit break exclusive weapon của hero qua RPC weapon/limitbreak (server tự suy weapon từ heroId).</summary>
        public async UniTask<WeaponLimitBreakResult> TryLimitBreakExclusiveAsync(int heroId, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return WeaponLimitBreakResult.Invalid;

            WeaponLimitBreakResponse response;
            try
            {
                response = await NakamaClient.Instance.LimitBreakWeaponAsync("exclusive", 0, heroId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/limitbreak RPC failed for heroId={heroId} (exclusive): {e.Message}");
                return WeaponLimitBreakResult.Invalid;
            }

            if (response == null)
                return WeaponLimitBreakResult.Invalid;

            if (!response.Success)
            {
                Debug.LogWarning($"[WeaponManager] weapon/limitbreak rejected for heroId={heroId} (exclusive): {response.Error}");
                return MapLimitBreakError(response.Error);
            }

            inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId).LimitBreakStage = response.NewStage;
            SpendStoneForLimitBreak(response.StoneCost, CurrencyTransactionReason.LimitBreakExclusiveWeapon);

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);

            return response.Result == "success" ? WeaponLimitBreakResult.Success : WeaponLimitBreakResult.Failed;
        }

        private static WeaponLimitBreakResult MapLimitBreakError(string error)
        {
            switch (error)
            {
                case "MAXED": return WeaponLimitBreakResult.Maxed;
                case "REQUIRED_LEVEL_NOT_REACHED": return WeaponLimitBreakResult.RequiredLevelNotReached;
                default: return WeaponLimitBreakResult.Invalid;
            }
        }

        /// <summary>
        /// Limit break vẫn dùng WeaponBreakThroughStone local-only (server chưa validate/trừ
        /// currency cho limit break — chỉ trả RNG result + stage, xem rpcWeaponLimitBreak).
        /// Level up KHÔNG còn dùng hàm này — xem TryLevelUpStandardAsync/TryLevelUpExclusiveAsync,
        /// giờ Set() trực tiếp theo stone_balance tuyệt đối server trả về.
        /// </summary>
        private static void SpendStoneForLimitBreak(int stoneCost, CurrencyTransactionReason reason)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.WeaponBreakThroughStone, stoneCost, reason);
        }

        /// <summary>Fuse local-only (không gọi server) — dùng cho Editor debug và nhánh ToRandomExclusive (spec server chưa cover).</summary>
        public WeaponFuseResult TryFuseStandard(int weaponId, bool autoSave = true)
        {
            var result = fuse.TryFuseStandard(weaponId);
            if (!result.Success)
                return result;

            NotifyStandardWeaponChanged(weaponId);

            if (result.TargetStandardWeaponId > 0)
                NotifyStandardWeaponChanged(result.TargetStandardWeaponId);

            if (result.TargetExclusiveWeaponId > 0)
            {
                NotifyExclusiveWeaponChanged(result.TargetExclusiveWeaponId, result.TargetHeroId);

                // auto equip exclusive ngay khi có
                EquipExclusiveAsync(result.TargetHeroId, autoSave).Forget();
            }

            return result;
        }

        /// <summary>
        /// Fuse 1 lần qua RPC weapon/fuse (nhánh ToNextStandard) — server là nguồn sự thật, trừ shard
        /// + đổi weapon_id/grade/star, client chỉ apply lại kết quả (xem Docs/be-weapon-fuse-rpc-spec.md).
        /// Nhánh ToRandomExclusive vẫn rơi về <see cref="TryFuseStandard"/> local vì spec server chưa cover
        /// (server trả MAX_NODE_REACHED cho nhánh này).
        /// </summary>
        public async UniTask<WeaponFuseResult> TryFuseStandardAsync(int weaponId, bool autoSave = true)
        {
            var def = database.GetStandard(weaponId);
            if (def == null)
                return new WeaponFuseResult { SourceWeaponId = weaponId, Success = false };

            if (def.FuseMode == WeaponFuseMode.ToRandomExclusive)
                return TryFuseStandard(weaponId, autoSave);

            var result = new WeaponFuseResult { SourceWeaponId = weaponId, Success = false };

            WeaponFuseResponse response;
            try
            {
                response = await NakamaClient.Instance.FuseWeaponAsync(weaponId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/fuse RPC failed for weaponId={weaponId}: {e.Message}");
                return result;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[WeaponManager] weapon/fuse rejected for weaponId={weaponId}: {response.Error}");
                return result;
            }

            ApplyFuseResult(response);
            
            NotifyStandardWeaponChanged(response.OldWeaponId);
            NotifyStandardWeaponChanged(response.NewWeaponId);

            result.Success = true;
            result.TargetStandardWeaponId = response.NewWeaponId;
            return result;
        }

        private void ApplyFuseResult(WeaponFuseResponse response)
        {
            var oldState = inventory.GetOrCreateStandardState(response.OldWeaponId);
            oldState.CurrentShard = Mathf.Max(0, response.ShardBalance);

            var newState = inventory.GetOrCreateStandardState(response.NewWeaponId);
            if (!newState.IsUnlocked)
            {
                newState.IsUnlocked = true;
                newState.Level = Mathf.Max(1, oldState.Level);
                newState.LimitBreakStage = 0;
            }

            // Server carries any leftover shard (banked beyond FuseShardRequired) forward to the
            // new id so Fuse All can keep chaining — apply that here instead of leaving it at 0,
            // otherwise local state would desync from server right after the first fuse in a batch.
            newState.CurrentShard = Mathf.Max(0, response.NewWeaponShardBalance);
        }

        public void NotifyHeroWeaponChanged(int heroId)
        {
            OnHeroWeaponChanged?.Invoke(heroId);
            HeroEquipmentRuntimeRegistry.RefreshHero(heroId);
        }

        public void NotifyStandardWeaponChanged(int weaponId)
        {
            OnStandardWeaponStateChanged?.Invoke(weaponId);
            HeroEquipmentRuntimeRegistry.RefreshHeroesUsingStandard(weaponId);
        }

        public void NotifyExclusiveWeaponChanged(int exclusiveWeaponId, int heroId)
        {
            OnExclusiveWeaponStateChanged?.Invoke(exclusiveWeaponId);
            NotifyHeroWeaponChanged(heroId);
        }

        public void DebugSetStandardLevel(int weaponId, int level, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            state.Level = Mathf.Max(1, level);

            NotifyStandardWeaponChanged(weaponId);
        }

        public void DebugSetStandardLimitBreakStage(int weaponId, int stage, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            state.LimitBreakStage = Mathf.Max(0, stage);

            NotifyStandardWeaponChanged(weaponId);
        }

        public void DebugSetStandardShard(int weaponId, int shard, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            state.CurrentShard = Mathf.Max(0, shard);

            NotifyStandardWeaponChanged(weaponId);
        }

        public void DebugSetExclusiveLevel(int heroId, int level, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.Level = Mathf.Max(1, level);

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public void DebugSetExclusiveLimitBreakStage(int heroId, int stage, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.LimitBreakStage = Mathf.Max(0, stage);

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public void DebugSetExclusiveShard(int heroId, int shard, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.CurrentShard = Mathf.Max(0, shard);

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public void DebugSetExclusiveStar(int heroId, int star, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.CurrentStar = Mathf.Max(1, star);

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        /// <summary>
        /// Debug-only: cấp tạm currency cho test account cũ. weapon_ore (Level Up) giờ đã có nguồn
        /// cấp thật (player_defaults.js lúc tạo account + server trừ/trả balance thật qua
        /// weapon/upgrade) — hàm này chỉ còn cần cho account tạo trước migration hoặc đã xài hết.
        /// WeaponBreakThroughStone (Limit Break) vẫn chưa có nguồn cấp thật nào (shop/reward).
        /// </summary>
        public void DebugAddWeaponCurrency(CurrencyType currencyType, int amount)
        {
            if (amount <= 0)
                return;

            CurrencyLedgerService.Instance?.AddOrMergeIncome(currencyType, amount, CurrencyTransactionReason.DebugGrant);
        }
    }
}