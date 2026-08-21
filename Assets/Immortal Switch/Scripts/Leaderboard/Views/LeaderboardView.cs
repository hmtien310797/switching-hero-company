using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Leaderboard.Views.UI;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Leaderboard.Views
{
    public class LeaderboardView : AnimatedUIView
    {
        // Số record top lấy về mỗi lần refresh — đủ cho top 3 + danh sách cuộn.
        private const int TopFetchLimit = 100;
        private const int AroundMeLimit = 5;
        private const float RefreshIntervalSeconds = 5 * 60;

        [Header("View references")]
        [SerializeField]
        private Button btnClose;

        [Header("Top references")]
        [SerializeField]
        private UILeaderboardTop top1;

        [SerializeField]
        private UILeaderboardTop top2;

        [SerializeField]
        private UILeaderboardTop top3;

        [Header("Rank references")]
        [SerializeField]
        private LeaderboardRankRecyclableView rankRecyclableView;

        [Header("My rank references")]
        [SerializeField]
        private UILeaderboardRankItem myRank;

        [SerializeField]
        private Button btnClaim;

        [Header("Countdown references")]
        [SerializeField]
        private UILeaderboardCountdown countdown;

        // Text "Hoàn tất xếp hạng trong 03d15h" có sẵn trong prefab nhưng chưa được gán vào script
        // nào — kéo GameObject đó vào field này trong Inspector để bật countdown mùa thật (server
        // trả season_end_at qua leaderboard/stage/top). Để trống thì tính năng chỉ là no-op.
        [SerializeField]
        private TextMeshProUGUI txtSeasonEndCountdown;

        private List<LeaderboardRecordDto> _topRecords = new();

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);

            // Thưởng cuối mùa giờ gửi thẳng qua mailbox (không cần chờ/claim ở đây nữa) — xem
            // handler/leaderboard.js sendSeasonRewardMailIfNeeded. Ẩn hẳn nút, không còn RPC nào gọi tới nó.
            btnClaim.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            RefreshAsync().Forget();
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<LeaderboardView>().Forget();
        }

        private void OnRefreshRank()
        {
            RefreshAsync().Forget();
        }

        private async UniTask RefreshAsync()
        {
            countdown.Bind(RefreshIntervalSeconds, OnRefreshRank);

            var topRequest = NakamaClient.Instance.GetLeaderboardStageTopAsync(TopFetchLimit);
            var aroundMeRequest = NakamaClient.Instance.GetLeaderboardStageAroundMeAsync(AroundMeLimit);

            LeaderboardStageTopResponse topResponse;
            LeaderboardStageAroundMeResponse aroundMeResponse;

            try
            {
                topResponse = await topRequest;
                aroundMeResponse = await aroundMeRequest;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardView] Refresh failed: {e.Message}");
                UIManager.Instance.ShowToast("Không thể tải bảng xếp hạng.");
                return;
            }

            _topRecords = topResponse?.Records ?? new List<LeaderboardRecordDto>();

            UpdateSeasonCountdownText(topResponse?.SeasonEndAt);
            RefreshTop();
            RefreshRankList();
            RefreshMyRank(aroundMeResponse?.Records);
        }

        private void RefreshTop()
        {
            BindTop(top1, 0);
            BindTop(top2, 1);
            BindTop(top3, 2);
        }

        private void BindTop(UILeaderboardTop top, int index)
        {
            var hasData = index < _topRecords.Count;
            top.gameObject.SetActive(hasData);

            if (hasData)
                top.Bind(_topRecords[index].DisplayName, _topRecords[index].Stage);
        }

        private void RefreshRankList()
        {
            // Danh sách cuộn hiện toàn bộ top (kể cả hạng 1-3, đã hiển thị riêng ở top1/top2/top3
            // phía trên) — tránh trống trơn khi leaderboard chưa đủ hơn 3 người.
            rankRecyclableView.Bind(_topRecords.Count, i => new LeaderboardRankResolver
            {
                Rank = _topRecords[i].Rank,
                PlayerName = _topRecords[i].DisplayName,
                Stage = _topRecords[i].Stage,
            });
        }

        private void RefreshMyRank(List<LeaderboardRecordDto> aroundMe)
        {
            var userId = NakamaClient.Instance.Session?.UserId;
            var mine = aroundMe?.FirstOrDefault(r => string.Equals(r.UserId, userId, StringComparison.Ordinal));

            // Người chơi chưa từng thắng 1 trận nào thì chưa có record trên leaderboard.
            myRank.gameObject.SetActive(mine != null);

            if (mine == null)
                return;

            var leaderboardReward = DatabaseManager.Instance.GetLeaderboardRewardByRank(mine.Rank);
            myRank.Bind(mine.Rank, mine.DisplayName, mine.Stage, true, leaderboardReward?.amount ?? 0);
        }

        private void UpdateSeasonCountdownText(long? seasonEndAtUnix)
        {
            if (txtSeasonEndCountdown == null)
                return;

            if (seasonEndAtUnix == null)
            {
                txtSeasonEndCountdown.text = string.Empty;
                return;
            }

            var remaining = DateTimeOffset.FromUnixTimeSeconds(seasonEndAtUnix.Value) - DateTimeOffset.UtcNow;

            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            txtSeasonEndCountdown.text = LocalizationManager.GetText(
                LocalizationKeys.UI_RANKING_END,
                $"{remaining.Days:00}",
                $"{remaining.Hours:00}"
            );
        }
    }
}