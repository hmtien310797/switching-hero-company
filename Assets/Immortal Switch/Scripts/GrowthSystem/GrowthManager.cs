﻿using System;
 using Cysharp.Threading.Tasks;
 using Immortal_Switch.Scripts.Core;
 using UnityEngine;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthManager : Singleton<GrowthManager>
    {
        [Header("Config")]
        [SerializeField] private GrowthDatabaseSO growthDatabase;
        [SerializeField] private int defaultGold = 100000;

        private const string SAVE_KEY = "GROWTH_SAVE";

        public GrowthSaveData SaveData { get; private set; }
        public GrowthSystemService Service { get; private set; }

        public event Action OnGrowthChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int, int, bool> OnTierReadyToUpgradePopup;

        public override UniTask InitializeAsync()
        {
            Load();
            return UniTask.CompletedTask;
        }

        public void Save()
        {
            ES3.Save(SAVE_KEY, SaveData);
        }

        public void Load()
        {
            if (ES3.KeyExists(SAVE_KEY))
            {
                SaveData = ES3.Load<GrowthSaveData>(SAVE_KEY);
            }
            else
            {
                SaveData = new GrowthSaveData();
            }

            Service = new GrowthSystemService(growthDatabase, SaveData);
            OnGrowthChanged?.Invoke();
        }

        /// <summary>Mua stack cho 1 stat — fire-and-forget wrapper cho UI (Button.onClick). Dùng <see cref="TryUpgradeAsync"/> nếu cần biết kết quả.</summary>
        public void TryUpgrade(StatType stat, int amount)
        {
            TryUpgradeAsync(stat, amount).Forget();
        }

        /// <summary>
        /// Mua stack cho 1 stat qua RPC growth/upgrade. Server là nguồn sự thật cho tier/stack — chỉ
        /// trả gold_spent để client tự trừ local, KHÔNG validate Gold balance thật (xem
        /// Docs/be-growth-rpc-spec.md mục 7). Client apply lại new_stack từ response, không tự tính.
        /// </summary>
        public async UniTask<bool> TryUpgradeAsync(StatType stat, int amount)
        {
            int currentTier = SaveData.CurrentUnlockedTier;
            int nextTier = currentTier + 1;
            bool wasCurrentTierFullyMaxed = Service.IsTierFullyMaxed(currentTier);

            GrowthUpgradeResponse response;
            try
            {
                response = await NakamaClient.Instance.UpgradeGrowthAsync(stat.ToString(), amount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GrowthManager] growth/upgrade RPC failed for stat={stat}: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[GrowthManager] growth/upgrade rejected for stat={stat}: {response.Error}");
                return false;
            }

            SaveData.SetStack(stat, response.NewStack);
            Save();

            CurrencyLedgerService.Instance.TrySpend(CurrencyType.gold, response.GoldSpent, CurrencyTransactionReason.GrowthUpgrade);

            bool isCurrentTierFullyMaxedNow = Service.IsTierFullyMaxed(currentTier);
            bool canOpenNextTierPopup = Service.HasTier(nextTier);

            OnGrowthChanged?.Invoke();

            if (!wasCurrentTierFullyMaxed && isCurrentTierFullyMaxedNow && canOpenNextTierPopup)
            {
                OnTierReadyToUpgradePopup?.Invoke(currentTier, nextTier, true);
            }

            return true;
        }

        /// <summary>Mở tier kế tiếp — fire-and-forget wrapper cho UI. Dùng <see cref="UnlockTierAsync"/> nếu cần biết kết quả.</summary>
        public void UnlockTier()
        {
            UnlockTierAsync().Forget();
        }

        /// <summary>
        /// Mở tier kế tiếp qua RPC growth/unlocktier — miễn phí, chỉ là gate xác nhận server-side
        /// (server tự suy next_tier = current_unlocked_tier + 1, xem Docs/be-growth-rpc-spec.md mục 5).
        /// </summary>
        public async UniTask<bool> UnlockTierAsync()
        {
            GrowthUnlockTierResponse response;
            try
            {
                response = await NakamaClient.Instance.UnlockGrowthTierAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GrowthManager] growth/unlocktier RPC failed: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[GrowthManager] growth/unlocktier rejected: {response.Error}");
                return false;
            }

            SaveData.CurrentUnlockedTier = response.NewTier;
            Save();
            OnGrowthChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Mở tier theo số tier chỉ định — LOCAL ONLY, bỏ qua RPC. Chỉ dùng cho debug tool
        /// (<see cref="GrowthDebugWindow"/>, <see cref="DebugUnlockNextTier"/>) — giá trị set ở đây sẽ
        /// bị server ghi đè lại ở lần <see cref="SyncFromServer"/> kế tiếp nếu server không có tiến
        /// trình tương ứng (hành vi mong muốn, tránh leak state debug — xem Docs/be-growth-rpc-spec.md mục 8).
        /// </summary>
        public void DebugUnlockTierLocal(int tier)
        {
            int oldTier = SaveData.CurrentUnlockedTier;

            Service.UnlockTier(tier);

            if (SaveData.CurrentUnlockedTier != oldTier)
            {
                Save();
                OnGrowthChanged?.Invoke();
            }
        }

        /// <summary>
        /// Sync toàn bộ tiến trình Growth từ server (growth/state, player/me.growth) — nguồn sự thật.
        /// Ghi đè toàn bộ state local (không merge) — tier/stack nào set ở local (qua debug tool) mà
        /// server không có sẽ tự "khoá lại" sau lần sync này, tránh leak state cheat/account khác
        /// (xem Docs/be-growth-rpc-spec.md mục 8).
        /// </summary>
        public void SyncFromServer(GrowthStateResponse response, bool autoSave = true)
        {
            if (response == null)
                return;

            int unlockedTier = Mathf.Max(1, response.CurrentUnlockedTier);

            if (unlockedTier > Service.MaxTier)
            {
                Debug.LogError(
                    $"[GrowthManager] Server current_unlocked_tier={unlockedTier} vượt quá MaxTier=" +
                    $"{Service.MaxTier} hiện có trong GrowthDatabaseSO local — GetTotalGrowthValue sẽ KHÔNG " +
                    "có segment để cộng bonus cho phần stack vượt tier local, player sẽ không thấy stat tăng " +
                    "trong combat dù server đã ghi nhận đúng. Cần cập nhật TierGrowthDataSO khớp " +
                    "game_growth_config.xlsx trước khi cho phép unlock quá tier hiện có (xem Docs/be-growth-rpc-spec.md)."
                );
            }

            SaveData.CurrentUnlockedTier = unlockedTier;

            SaveData.Stats.Clear();
            if (response.Stats != null)
            {
                foreach (var dto in response.Stats)
                {
                    if (!Enum.TryParse<StatType>(dto.Stat, true, out var stat))
                    {
                        Debug.LogWarning($"[GrowthManager] Unknown stat '{dto.Stat}' from growth/state");
                        continue;
                    }

                    SaveData.SetStack(stat, Mathf.Max(0, dto.CurrentStack));
                }
            }

            if (autoSave)
                Save();

            OnGrowthChanged?.Invoke();
        }

        /// <summary>Gọi growth/state rồi apply ngay qua <see cref="SyncFromServer"/> — dùng khi cần re-sync (mở màn hình Growth).</summary>
        public async UniTask<bool> SyncFromServerAsync()
        {
            GrowthStateResponse response;
            try
            {
                response = await NakamaClient.Instance.GetGrowthStateAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GrowthManager] growth/state RPC failed: {e.Message}");
                return false;
            }

            if (response == null)
                return false;

            SyncFromServer(response);
            return true;
        }

        public void CheckAndNotifyTierReady()
        {
            if (SaveData == null || Service == null)
                return;

            int currentTier = SaveData.CurrentUnlockedTier;
            int nextTier = currentTier + 1;

            bool isCurrentTierFullyMaxed = Service.IsTierFullyMaxed(currentTier);
            bool canOpenNextTierPopup = Service.HasTier(nextTier);

            OnTierReadyToUpgradePopup?.Invoke(currentTier, nextTier, isCurrentTierFullyMaxed && canOpenNextTierPopup);
        }

        public void ClearData()
        {
            if (ES3.KeyExists(SAVE_KEY))
                ES3.DeleteKey(SAVE_KEY);

            SaveData = new GrowthSaveData();
            Service = new GrowthSystemService(growthDatabase, SaveData);

            Debug.Log("[Growth] DATA CLEARED");
            OnGrowthChanged?.Invoke();
        }

        [ContextMenu("DEBUG / Unlock Next Tier")]
        public void DebugUnlockNextTier()
        {
            DebugUnlockTierLocal(SaveData.CurrentUnlockedTier + 1);
            Debug.Log("[Growth] Unlock Tier");
        }

        [ContextMenu("DEBUG / Save")]
        public void DebugSave()
        {
            Save();
            Debug.Log("[Growth] Saved");
        }

        [ContextMenu("DEBUG / Load")]
        public void DebugLoad()
        {
            Load();
            Debug.Log("[Growth] Loaded");
        }

        [ContextMenu("DEBUG / CLEAR ALL DATA")]
        public void DebugClearData()
        {
            ClearData();
        }
    }
}