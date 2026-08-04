using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventDice
{
    /// <summary>
    /// Cache trạng thái Event Dice lấy từ server (RPC eventdice/*) — server là nguồn sự thật duy
    /// nhất, thay cho EventDiceService/Storage (ES3) cục bộ trước đây.
    /// </summary>
    public class EventDiceManager : Singleton<EventDiceManager>
    {
        /// <summary>Phát ra khi State được refresh từ server.</summary>
        public event Action OnDataChanged;

        public EventDiceStateResponse State { get; private set; }

        public override UniTask InitializeAsync() => UniTask.CompletedTask;

        /// <summary>Tải lại toàn bộ state từ server. Gọi khi mở EventDiceView và sau mỗi
        /// roll/claim thành công để đồng bộ tiến trình/pending rewards/số dư.</summary>
        public async UniTask RefreshAsync()
        {
            try
            {
                State = await NakamaClient.Instance.EventDiceStateAsync();
                OnDataChanged?.Invoke();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventDiceManager] eventdice/state failed: {ex.StatusCode} {ex.Message}");
            }
        }

        // ── Read-only accessors — luôn đọc từ snapshot RefreshAsync() gần nhất ──────────────

        public int CurrentIndex => State?.Progress?.CurrentIndex ?? 0;

        public int CurrentProgress => State?.Progress?.CurrentProgress ?? 0;

        public List<ItemData> GetPendingRewards()
        {
            var pending = State?.Progress?.PendingRewards;

            return pending == null
                ? new List<ItemData>()
                : pending.Select(item => new ItemData(item.ItemId, item.Amount)).ToList();
        }

        public IReadOnlyCollection<int> GetClaimedMilestoneIds()
        {
            var milestones = State?.Milestones;

            return milestones == null
                ? Array.Empty<int>()
                : milestones.Where(m => m.Claimed).Select(m => m.Milestone).ToList();
        }

        /// <summary>Kiểm tra milestone đã đủ progress và chưa được nhận.</summary>
        public bool CanClaimMilestone(DynamicHeroesGlobalSpecificationsEventDiceMilestoneRow row)
        {
            var dto = row == null ? null : FindMilestone(row.milestone);
            return dto != null && dto.IsEligible && !dto.Claimed;
        }

        // ── Mutating actions — gọi RPC, refresh cache, trả reward cho UI hiển thị popup ─────

        /// <summary>Trừ 1 vé, server random 1-6 và cộng thưởng ô đáp xuống vào pending. Ném
        /// exception khi server từ chối (event chưa mở, hết vé, board rỗng) — caller
        /// (UIEventDiceRollPanel.RollAsync) đã bắt Exception và log.</summary>
        public async UniTask<int> RollAsync(CancellationToken cancellationToken)
        {
            EventDiceRollResponse response;

            try
            {
                response = await NakamaClient.Instance.EventDiceRollAsync();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventDiceManager] eventdice/roll error {ex.StatusCode}: {ex.Message}");
                throw;
            }

            if (!response.Success)
            {
                throw new InvalidOperationException(response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return response.Result;
        }

        /// <summary>Nhận thưởng một milestone đủ điều kiện.</summary>
        public async UniTask<(List<ItemData> rewards, string error)> ClaimMilestoneAsync(
            DynamicHeroesGlobalSpecificationsEventDiceMilestoneRow row
        )
        {
            if (!CanClaimMilestone(row))
            {
                return (new List<ItemData>(), "MILESTONE_NOT_CLAIMABLE");
            }

            EventDiceClaimMilestoneResponse response;

            try
            {
                response = await NakamaClient.Instance.EventDiceClaimMilestoneAsync(
                    new EventDiceClaimMilestoneRequest { Milestone = row.milestone }
                );
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventDiceManager] eventdice/claim_milestone error {ex.StatusCode}: {ex.Message}");
                return (new List<ItemData>(), "NETWORK_ERROR");
            }

            if (!response.Success)
            {
                return (new List<ItemData>(), response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            var rewards = response.Item != null
                ? new List<ItemData> { new ItemData(response.Item.ItemId, response.Item.Amount) }
                : new List<ItemData>();

            return (rewards, null);
        }

        /// <summary>Nhận và xóa toàn bộ pending rewards.</summary>
        public async UniTask<List<ItemData>> ClaimPendingRewardsAsync()
        {
            EventDiceClaimPendingResponse response;

            try
            {
                response = await NakamaClient.Instance.EventDiceClaimPendingAsync();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventDiceManager] eventdice/claim_pending error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return response.Rewards?.Select(item => new ItemData(item.ItemId, item.Amount)).ToList()
                   ?? new List<ItemData>();
        }

        // ── lookups ──────────────────────────────────────────────────────────────────────

        private EventDiceMilestoneEntryDto FindMilestone(int milestone)
        {
            var milestones = State?.Milestones;

            if (milestones == null)
            {
                return null;
            }

            for (var i = 0; i < milestones.Count; i++)
            {
                if (milestones[i].Milestone == milestone)
                {
                    return milestones[i];
                }
            }

            return null;
        }
    }
}
