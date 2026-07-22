using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shop.IAP;
using Immortal_Switch.Scripts.UI;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong
{
    /// <summary>
    /// Cache trạng thái event Lễ Hội Băng Long lấy từ server (RPC eventbl/*) — server là nguồn sự
    /// thật duy nhất, thay cho EventLeHoiBangLongService/Storage (ES3) cục bộ trước đây, vốn chỉ
    /// hiển thị popup mà không hề cấp vật phẩm thật (xem project memory
    /// "EventBL rewards not granted"). Cùng kiến trúc với EventWheelPassManager.
    /// </summary>
    public class EventLeHoiBangLongManager : Singleton<EventLeHoiBangLongManager>
    {
        public event Action OnDataChanged;

        public EventBLStateResponse State { get; private set; }

        public override UniTask InitializeAsync()
        {
            SubscribeEvents();
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }

        /// <summary>Tải lại toàn bộ state từ server. Gọi khi mở EventLeHoiBangLongView và sau mỗi
        /// claim/summon thành công để đồng bộ tiến trình/số dư.</summary>
        public async UniTask RefreshAsync()
        {
            try
            {
                State = await NakamaClient.Instance.GetEventBLStateAsync();
                OnDataChanged?.Invoke();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/state failed: {ex.StatusCode} {ex.Message}");
            }
        }

        // ── Read-only accessors — luôn đọc từ snapshot RefreshAsync() gần nhất ──────────────

        public EventBLMissionDto FindMission(string missionId)
        {
            return State?.Missions?.FirstOrDefault(m => m.MissionId == missionId);
        }

        public bool IsMissionMilestoneClaimed(int milestone)
        {
            return State?.MissionMilestones?.FirstOrDefault(m => m.Milestone == milestone)?.IsClaimed ?? false;
        }

        public bool IsSummonMilestoneClaimed(int milestone)
        {
            return State?.SummonMilestones?.FirstOrDefault(m => m.Milestone == milestone)?.IsClaimed ?? false;
        }

        public bool IsFreeLoginRewardClaimed(int day)
        {
            return State?.CheckIn?.FirstOrDefault(c => c.Day == day)?.Claimed ?? false;
        }

        private EventBLCheckInBonusDto FindBonus(int day)
        {
            return State?.CheckInBonus?.FirstOrDefault(c => c.Day == day);
        }

        public bool IsFreeBonusClaimed(int day) => FindBonus(day)?.ClaimedFreeBonus ?? false;
        public bool IsBonusPurchased(int day) => FindBonus(day)?.PurchasedBonus ?? false;
        public bool IsBonusClaimed(int day) => FindBonus(day)?.ClaimedBonus ?? false;

        public bool CanClaimFreeLoginReward(int day)
        {
            var loginDay = State?.Progress?.LoginDay ?? 0;
            return day >= 1 && day <= 7 && day <= loginDay && !IsFreeLoginRewardClaimed(day);
        }

        public bool CanClaimFreeBonus(int day)
        {
            var loginDay = State?.Progress?.LoginDay ?? 0;
            return day >= 1 && day <= 7 && day <= loginDay && !IsFreeBonusClaimed(day);
        }

        public bool CanPurchaseBonus(int day)
        {
            return day >= 1 && day <= 7 && IsFreeBonusClaimed(day) && !IsBonusPurchased(day);
        }

        public bool CanClaimBonus(int day)
        {
            return day >= 1 && day <= 7 && IsBonusPurchased(day) && !IsBonusClaimed(day);
        }

        // ── Mutating actions — gọi RPC, apply balances, refresh cache, trả reward cho UI ───

        /// <summary>Nhận thưởng đăng nhập miễn phí ngày N.</summary>
        public async UniTask<List<ItemData>> ClaimFreeLoginReward(int day)
        {
            EventBLClaimLoginResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBLClaimLoginAsync(day);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/claim_login error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLeHoiBangLongManager] eventbl/claim_login day={day} failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAndMaybeGrantHeroAsync(response.Reward);

            return ToItemDataList(response.Reward);
        }

        /// <summary>Nhận track bonus miễn phí ("instant") của ngày N — mở khoá nút mua track trả phí.</summary>
        public async UniTask<List<ItemData>> ClaimFreeBonus(int day)
        {
            EventBLClaimBonusFreeResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBLClaimBonusFreeAsync(day);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/claim_bonus_free error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLeHoiBangLongManager] eventbl/claim_bonus_free day={day} failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            var hasHero = response.Rewards?.Any(r => r.IsHero) ?? false;
            await RefreshAsync();

            if (hasHero)
                await UserDataCache.Instance.RefreshHeroListFromServerAsync();

            return ToItemDataList(response.Rewards);
        }

        /// <summary>
        /// Yêu cầu mua gói bonus trả phí bằng IAP sau khi lượt bonus miễn phí đã nhận. Vật phẩm
        /// được cấp thật ngay khi iap/pack_purchase xác nhận thành công (packId 35-41 resolve qua
        /// pack_event, được buildPackIapIndex gộp chung với pack_iap — xem handler/iap.js) — 2 RPC
        /// gọi ngay sau đó (confirm_bonus_purchase/claim_bonus_paid) chỉ ghi lại trạng thái nút
        /// mua/nhận, không cấp thêm gì (xem EventBLCheckInBonusDto).
        /// </summary>
        public bool RequestBonusPurchase(
            int day,
            Action<IReadOnlyList<ItemData>> onSuccess = null)
        {
            if (!CanPurchaseBonus(day))
            {
                return false;
            }

            var config = DatabaseManager.Instance.GetEventLHBLCheckInBonusRewards(day);
            var storeProductId = IAPManager.GetStoreProductId(config.product);

            if (config.packId <= 0 ||
                string.IsNullOrWhiteSpace(storeProductId))
            {
                Debug.LogError($"[EventLeHoiBangLong] Không tìm thấy cấu hình IAP cho bonus ngày {day}.");
                return false;
            }

            IAPManager.Instance.BuyPackProduct(config.packId, storeProductId,
                (success, error) => { OnBonusPurchaseComplete(day, success, error, config.bonusRewards, onSuccess).Forget(); });

            return true;
        }

        private async UniTaskVoid OnBonusPurchaseComplete(
            int day,
            bool success,
            string error,
            IReadOnlyList<ItemData> bonusRewards,
            Action<IReadOnlyList<ItemData>> onSuccess)
        {
            if (!success)
            {
                Debug.LogWarning($"[EventLeHoiBangLong] Mua bonus ngày {day} thất bại: {error}");
                return;
            }

            var confirmed = await ConfirmBonusPurchase(day);
            var claimed = confirmed && await ClaimBonusPaid(day);

            if (claimed)
            {
                onSuccess?.Invoke(bonusRewards);
            }
        }

        public async UniTask<bool> ConfirmBonusPurchase(int day)
        {
            try
            {
                var response = await NakamaClient.Instance.EventBLConfirmBonusPurchaseAsync(day);

                if (response.Success)
                    await RefreshAsync();

                return response.Success;
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/confirm_bonus_purchase error {ex.StatusCode}: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> ClaimBonusPaid(int day)
        {
            try
            {
                var response = await NakamaClient.Instance.EventBLClaimBonusPaidAsync(day);

                if (response.Success)
                    await RefreshAsync();

                return response.Success;
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/claim_bonus_paid error {ex.StatusCode}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Báo tiến độ nhiệm vụ theo trigger trong config lên server (gọi khi gameplay event
        /// tương ứng xảy ra — xem SubscribeEvents). Cập nhật lạc quan (optimistic) vào State cục
        /// bộ trước để UI phản hồi ngay, giống hệt cảm giác của bản client-local trước đây; RPC
        /// chạy nền, không chặn UI. Server vẫn là nguồn sự thật cho lần RefreshAsync() kế tiếp.
        /// </summary>
        public void ChangeMissionProgress(string trigger, int value)
        {
            if (string.IsNullOrWhiteSpace(trigger) ||
                value <= 0 ||
                State?.Missions == null)
            {
                return;
            }

            var changed = false;

            foreach (var mission in State.Missions)
            {
                if (mission.Trigger != trigger ||
                    mission.IsClaimed)
                {
                    continue;
                }

                mission.Progress = Mathf.Min(mission.Target, mission.Progress + value);
                changed = true;
            }

            if (changed)
            {
                OnDataChanged?.Invoke();
            }

            ReportMissionProgressAsync(trigger, value).Forget();
        }

        private async UniTaskVoid ReportMissionProgressAsync(string trigger, int value)
        {
            try
            {
                await NakamaClient.Instance.EventBLMissionProgressAsync(trigger, value);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/mission_progress error {ex.StatusCode}: {ex.Message}");
            }
        }

        /// <summary>Nhận thưởng một nhiệm vụ hoàn thành và cộng tổng điểm.</summary>
        public async UniTask<List<ItemData>> ClaimMission(string missionId)
        {
            EventBLClaimMissionResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBLClaimMissionAsync(missionId);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/claim_mission error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLeHoiBangLongManager] eventbl/claim_mission {missionId} failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return response.Reward != null
                ? new List<ItemData> { new ItemData(response.Reward.ItemId, response.Reward.Amount) }
                : new List<ItemData>();
        }

        /// <summary>Nhận một milestone điểm nhiệm vụ.</summary>
        public async UniTask<List<ItemData>> ClaimMissionMilestone(int milestone)
        {
            EventBLMilestoneResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBLClaimMissionMilestoneAsync(milestone);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError(
                    $"[EventLeHoiBangLongManager] eventbl/claim_mission_milestone error {ex.StatusCode}: {ex.Message}");

                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning(
                    $"[EventLeHoiBangLongManager] eventbl/claim_mission_milestone {milestone} failed: {response.Error}");

                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return response.Reward != null
                ? new List<ItemData> { new ItemData(response.Reward.ItemId, response.Reward.Amount) }
                : new List<ItemData>();
        }

        /// <summary>Nhận tất cả milestone nhiệm vụ đang đủ điều kiện, tuần tự (không song song) để
        /// tránh 2 request cùng ghi player_event_bl storage — cùng cách EventWheelPassManager.ClaimAllAsync làm.</summary>
        public async UniTask<List<ItemData>> ClaimAvailableMissionMilestones()
        {
            var rewards = new List<ItemData>();

            var claimable = (State?.MissionMilestones ?? new List<EventBLMilestoneDto>())
                .Where(m => !m.IsClaimed && (State.Progress?.MissionPoints ?? 0) >= m.PointsRequired)
                .Select(m => m.Milestone)
                .ToList();

            foreach (var milestone in claimable)
            {
                rewards.AddRange(await ClaimMissionMilestoneNoRefresh(milestone));
            }

            await RefreshAsync();
            return rewards;
        }

        private async UniTask<List<ItemData>> ClaimMissionMilestoneNoRefresh(int milestone)
        {
            try
            {
                var response = await NakamaClient.Instance.EventBLClaimMissionMilestoneAsync(milestone);

                if (!response.Success)
                    return new List<ItemData>();

                CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

                return response.Reward != null
                    ? new List<ItemData> { new ItemData(response.Reward.ItemId, response.Reward.Amount) }
                    : new List<ItemData>();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError(
                    $"[EventLeHoiBangLongManager] eventbl/claim_mission_milestone error {ex.StatusCode}: {ex.Message}");

                return new List<ItemData>();
            }
        }

        /// <summary>Nhận một milestone tích lũy triệu hồi.</summary>
        public async UniTask<List<ItemData>> ClaimSummonMilestone(int milestone)
        {
            EventBLMilestoneResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBLClaimSummonMilestoneAsync(milestone);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/claim_summon_milestone error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning(
                    $"[EventLeHoiBangLongManager] eventbl/claim_summon_milestone {milestone} failed: {response.Error}");

                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return response.Reward != null
                ? new List<ItemData> { new ItemData(response.Reward.ItemId, response.Reward.Amount) }
                : new List<ItemData>();
        }

        /// <summary>Nhận tất cả milestone triệu hồi đang đủ điều kiện, tuần tự.</summary>
        public async UniTask<List<ItemData>> ClaimAvailableSummonMilestones()
        {
            var rewards = new List<ItemData>();

            var claimable = (State?.SummonMilestones ?? new List<EventBLMilestoneDto>())
                .Where(m => !m.IsClaimed && (State.Progress?.SummonPoints ?? 0) >= m.PointsRequired)
                .Select(m => m.Milestone)
                .ToList();

            foreach (var milestone in claimable)
            {
                try
                {
                    var response = await NakamaClient.Instance.EventBLClaimSummonMilestoneAsync(milestone);

                    if (response.Success &&
                        response.Reward != null)
                    {
                        CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
                        rewards.Add(new ItemData(response.Reward.ItemId, response.Reward.Amount));
                    }
                }
                catch (ApiResponseException ex)
                {
                    Debug.LogError(
                        $"[EventLeHoiBangLongManager] eventbl/claim_summon_milestone error {ex.StatusCode}: {ex.Message}");
                }
            }

            await RefreshAsync();
            return rewards;
        }

        /// <summary>
        /// Quay Băng Long Summon — server random có trọng số, trừ 1x summon_ticket_hero_banner
        /// (item 47) mỗi lượt, và cấp thưởng thật (bao gồm cả hero jackpot, xem
        /// EventBLRewardDto.IsHero). Trả về danh sách rỗng nếu thất bại (không đủ vé, xem
        /// EventBLSummonResponse.Error) — UI hiện tại chỉ log cảnh báo, chưa có toast báo lỗi
        /// riêng cho người chơi. Dòng reward loại HERO không có ItemData tương ứng để hiển thị
        /// dạng thẻ item trong popup hiện tại — vẫn được cấp thật vào roster, chỉ chưa có UI thẻ
        /// hero riêng cho gacha popup (việc đó cần thêm asset/UI, ngoài phạm vi port logic này).
        /// </summary>
        public async UniTask<List<ItemData>> SummonAsync(int times)
        {
            EventBLSummonResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBLSummonAsync(times);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLeHoiBangLongManager] eventbl/summon error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLeHoiBangLongManager] eventbl/summon failed: {response.Error}");
                UIManager.Instance.ShowToast(DescribeEventError(response.Error));
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

            var result = new List<ItemData>();
            var hasHero = false;

            foreach (var entry in response.Entries)
            {
                if (entry.Reward.IsHero)
                {
                    hasHero = true;

                    Debug.Log(
                        $"[EventLeHoiBangLongManager] Summon granted hero_id={entry.Reward.HeroId} ({entry.Reward.HeroName})");

                    continue;
                }

                result.Add(new ItemData(entry.Reward.Item.ItemId, entry.Reward.Item.Amount));
            }

            await RefreshAsync();

            if (hasHero)
                await UserDataCache.Instance.RefreshHeroListFromServerAsync();

            return result;
        }

        private static string DescribeEventError(string error)
        {
            return error switch
            {
                "EVENT_NOT_ACTIVE" => "Sự kiện không còn hoạt động.",
                "INSUFFICIENT_TICKET" => "Không đủ vé để quay.",
                _ => "Quay thất bại, vui lòng thử lại.",
            };
        }

        // ── Gameplay event subscriptions (báo tiến độ nhiệm vụ) ─────────────────────────────

        private void SubscribeEvents()
        {
            if (PlayerSystemManager.Instance != null)
            {
                PlayerSystemManager.Instance.OnLoginNewDay += OnLoginNewDay;
            }

            GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Subscribe(GameEvents.ON_ENHANCE_GEAR, OnEnhanceGear);
            GameEventManager.Subscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnClaimIdleReward);
        }

        private void UnsubscribeEvents()
        {
            if (PlayerSystemManager.Instance != null)
            {
                PlayerSystemManager.Instance.OnLoginNewDay -= OnLoginNewDay;
            }

            GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Unsubscribe(GameEvents.ON_ENHANCE_GEAR, OnEnhanceGear);
            GameEventManager.Unsubscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnClaimIdleReward);
        }

        private void OnLoginNewDay()
        {
            RefreshAsync().Forget();
        }

        private void OnEnemyDead(int count)
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_KILL_MONSTER, count > 0 ? 1 : 0);
        }

        private void OnStageCleared(int stage)
        {
            ChangeMissionProgress("CLEAR_STAGE_COUNT", 1);
        }

        private void OnSummonHero(int times)
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_HERO_SUMMON, times);
        }

        private void OnClaimIdleReward()
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_CLAIM_IDLE, 1);
        }

        private void OnEnhanceGear()
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_ENHANCE_GEAR, 1);
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────

        private async UniTask RefreshAndMaybeGrantHeroAsync(EventBLRewardDto reward)
        {
            await RefreshAsync();

            if (reward != null &&
                reward.IsHero)
            {
                await UserDataCache.Instance.RefreshHeroListFromServerAsync();
            }
        }

        private static List<ItemData> ToItemDataList(EventBLRewardDto reward)
        {
            var list = new List<ItemData>();

            if (reward != null &&
                !reward.IsHero &&
                reward.Item != null)
            {
                list.Add(new ItemData(reward.Item.ItemId, reward.Item.Amount));
            }

            return list;
        }

        private static List<ItemData> ToItemDataList(List<EventBLRewardDto> rewards)
        {
            var list = new List<ItemData>();

            if (rewards == null)
                return list;

            foreach (var reward in rewards)
            {
                if (!reward.IsHero &&
                    reward.Item != null)
                {
                    list.Add(new ItemData(reward.Item.ItemId, reward.Item.Amount));
                }
            }

            return list;
        }
    }
}