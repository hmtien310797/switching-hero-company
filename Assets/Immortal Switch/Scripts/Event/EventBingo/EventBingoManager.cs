using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventBingo
{
    /// <summary>
    /// Cache trạng thái Event Bingo lấy từ server (RPC eventbingo/*) — server là nguồn sự thật
    /// duy nhất, thay cho EventBingoService/Storage (ES3) cục bộ trước đây.
    /// </summary>
    public class EventBingoManager : Singleton<EventBingoManager>
    {
        /// <summary>Phát ra khi State được refresh từ server.</summary>
        public event Action OnDataChanged;

        /// <summary>Phát ra ngay khi round hoàn thành để UI bắt đầu chạy hiệu ứng kết thúc.</summary>
        public event Action OnRoundCompleted;

        /// <summary>Phát ra sau khi board, rương và pool mới đã được tạo xong.</summary>
        public event Action OnRoundRefreshed;

        /// <summary>Snapshot state gần nhất lấy từ eventbingo/state.</summary>
        public EventBingoStateResponse State { get; private set; }

        /// <summary>Cho biết manager đang chờ hiệu ứng chuyển round hoàn tất hay không.</summary>
        public bool IsChangingRound { get; private set; }

        public override UniTask InitializeAsync() => UniTask.CompletedTask;

        public int CurrentPoolId => State?.Progress?.CurrentPoolId ?? 0;

        public int CurrentProgress => State?.Progress?.CurrentProgress ?? 0;

        /// <summary>Tải lại toàn bộ state từ server. Gọi khi mở EventBingoView và sau mỗi
        /// draw/claim thành công để đồng bộ lại board/rương/progress.</summary>
        public async UniTask RefreshAsync()
        {
            try
            {
                State = await NakamaClient.Instance.EventBingoStateAsync();
                OnDataChanged?.Invoke();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventBingoManager] eventbingo/state failed: {ex.StatusCode} {ex.Message}");
            }
        }

        // ── Read-only accessors — luôn đọc từ snapshot RefreshAsync() gần nhất ──────────────

        /// <summary>Kiểm tra một tile trên board đã được mở hay chưa.</summary>
        public bool IsTileUnlocked(int trackIndex)
        {
            var entry = State?.Board?.FirstOrDefault(b => b.TrackIndex == trackIndex);
            return entry != null && entry.Unlocked;
        }

        /// <summary>Danh sách track index của các tile đã mở trong round hiện tại.</summary>
        public IReadOnlyCollection<int> GetUnlockedTrackIndices()
        {
            var board = State?.Board;

            return board == null
                ? new List<int>()
                : board.Where(b => b.Unlocked).Select(b => b.TrackIndex).ToList();
        }

        /// <summary>Kiểm tra line tương ứng đã hoàn thành và mở khóa rương hay chưa.</summary>
        public bool IsLineUnlocked(int lineId)
        {
            var entry = FindLine(lineId);
            return entry != null && entry.Unlocked;
        }

        /// <summary>Kiểm tra rương của line tương ứng đã được nhận hay chưa.</summary>
        public bool IsLineChestClaimed(int lineId)
        {
            var entry = FindLine(lineId);
            return entry != null && entry.Claimed;
        }

        /// <summary>Lấy danh sách ID milestone đã nhận.</summary>
        public IReadOnlyCollection<int> GetClaimedMilestoneIds()
        {
            var milestones = State?.Milestones;

            return milestones == null
                ? Array.Empty<int>()
                : milestones.Where(m => m.Claimed).Select(m => m.Milestone).ToList();
        }

        /// <summary>Kiểm tra milestone đã đủ progress và chưa được nhận.</summary>
        public bool CanClaimMilestone(int milestoneId)
        {
            var entry = State?.Milestones?.FirstOrDefault(m => m.Milestone == milestoneId);
            return entry != null && entry.IsEligible && !entry.Claimed;
        }

        // ── Mutating actions — gọi RPC, refresh cache, trả reward cho UI hiển thị popup ─────

        /// <summary>Mở ngẫu nhiên một ô chưa mở trên board (server random) và trả reward của ô
        /// đó. Ném lỗi qua tuple error khi server từ chối (event chưa mở, board đã mở hết...).</summary>
        public async UniTask<(int trackIndex, ItemData reward, string error)> DrawAsync()
        {
            if (IsChangingRound)
            {
                return (-1, null, "ROUND_IS_CHANGING");
            }

            EventBingoDrawResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBingoDrawAsync();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventBingoManager] eventbingo/draw error {ex.StatusCode}: {ex.Message}");
                return (-1, null, "NETWORK_ERROR");
            }

            if (!response.Success)
            {
                return (-1, null, response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            var reward = response.Reward != null
                ? new ItemData(response.Reward.ItemId, response.Reward.Amount)
                : null;

            return (response.TrackIndex, reward, null);
        }

        /// <summary>
        /// Nhận reward của một rương line. Khi đây là rương cuối cùng của round, server đã tự
        /// sang round mới trong cùng lượt gọi — manager chỉ chờ hiệu ứng UI rồi refresh lại.
        /// </summary>
        public async UniTask<(List<ItemData> rewards, string error, bool roundCompleted)> ClaimLineChestAsync(
            int lineId,
            TimeSpan roundCompleteDelay,
            CancellationToken cancellationToken
        )
        {
            if (IsChangingRound)
            {
                return (new List<ItemData>(), "ROUND_IS_CHANGING", false);
            }

            EventBingoClaimLineResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBingoClaimLineAsync(
                    new EventBingoClaimLineRequest { LineId = lineId }
                );
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventBingoManager] eventbingo/claim_line error {ex.StatusCode}: {ex.Message}");
                return (new List<ItemData>(), "NETWORK_ERROR", false);
            }

            if (!response.Success)
            {
                return (new List<ItemData>(), response.Error, false);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

            var rewards = response.Rewards?.Select(item => new ItemData(item.ItemId, item.Amount)).ToList()
                          ?? new List<ItemData>();

            if (response.RoundCompleted)
            {
                await HandleRoundCompletedAsync(roundCompleteDelay, cancellationToken);
            }
            else
            {
                await RefreshAsync();
            }

            return (rewards, null, response.RoundCompleted);
        }

        /// <summary>Nhận một milestone đủ điều kiện và trả reward cho UI hiển thị.</summary>
        public async UniTask<(List<ItemData> rewards, string error)> ClaimMilestoneAsync(int milestoneId)
        {
            if (!CanClaimMilestone(milestoneId))
            {
                return (new List<ItemData>(), "MILESTONE_NOT_CLAIMABLE");
            }

            EventBingoClaimMilestoneResponse response;

            try
            {
                response = await NakamaClient.Instance.EventBingoClaimMilestoneAsync(
                    new EventBingoClaimMilestoneRequest { Milestone = milestoneId }
                );
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventBingoManager] eventbingo/claim_milestone error {ex.StatusCode}: {ex.Message}");
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

        private async UniTask HandleRoundCompletedAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            IsChangingRound = true;
            OnRoundCompleted?.Invoke();

            try
            {
                if (delay > TimeSpan.Zero)
                {
                    await UniTask.Delay(delay, cancellationToken: cancellationToken);
                }

                await RefreshAsync();
                OnRoundRefreshed?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // View đã đóng trong lúc chờ hiệu ứng round-complete. Round mới đã được tạo
                // server-side, sẽ đồng bộ lại ở lần RefreshAsync tiếp theo (mở lại view).
            }
            finally
            {
                IsChangingRound = false;
            }
        }

        private EventBingoLineEntryDto FindLine(int lineId)
        {
            return State?.Lines?.FirstOrDefault(l => l.LineId == lineId);
        }
    }
}
