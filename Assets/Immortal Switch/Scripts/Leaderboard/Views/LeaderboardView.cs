using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Leaderboard.Views.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Leaderboard.Views
{
    public class LeaderboardView : AnimatedUIView
    {
        // Số record top lấy về mỗi lần refresh — đủ cho top 3 + danh sách cuộn.
        private const int TopFetchLimit    = 100;
        private const int AroundMeLimit    = 5;
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

        [Header("Countdown references")]
        [SerializeField]
        private UILeaderboardCountdown countdown;

        private List<LeaderboardRecordDto> _topRecords = new();

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
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
            UIManager.Instance.Close<LeaderboardView>();
        }

        private void OnRefreshRank()
        {
            RefreshAsync().Forget();
        }

        private async UniTask RefreshAsync()
        {
            countdown.Bind(RefreshIntervalSeconds, OnRefreshRank);

            var topRequest      = NakamaClient.Instance.GetLeaderboardStageTopAsync(TopFetchLimit);
            var aroundMeRequest = NakamaClient.Instance.GetLeaderboardStageAroundMeAsync(AroundMeLimit);

            LeaderboardStageTopResponse topResponse;
            LeaderboardStageAroundMeResponse aroundMeResponse;
            try
            {
                topResponse      = await topRequest;
                aroundMeResponse = await aroundMeRequest;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardView] Refresh failed: {e.Message}");
                UIManager.Instance.ShowToast("Không thể tải bảng xếp hạng.");
                return;
            }

            _topRecords = topResponse?.Records ?? new List<LeaderboardRecordDto>();

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
            if (hasData) top.Bind(_topRecords[index].DisplayName, _topRecords[index].Stage);
        }

        private void RefreshRankList()
        {
            // Danh sách cuộn hiện toàn bộ top (kể cả hạng 1-3, đã hiển thị riêng ở top1/top2/top3
            // phía trên) — tránh trống trơn khi leaderboard chưa đủ hơn 3 người.
            rankRecyclableView.Bind(_topRecords.Count, i => new LeaderboardRankResolver
            {
                Rank       = _topRecords[i].Rank,
                PlayerName = _topRecords[i].DisplayName,
                Stage      = _topRecords[i].Stage,
            });
        }

        private void RefreshMyRank(List<LeaderboardRecordDto> aroundMe)
        {
            var userId = NakamaClient.Instance.Session?.UserId;
            var mine   = aroundMe?.FirstOrDefault(r => string.Equals(r.UserId, userId, StringComparison.Ordinal));

            // Người chơi chưa từng thắng 1 trận nào thì chưa có record trên leaderboard.
            myRank.gameObject.SetActive(mine != null);
            if (mine == null) return;

            myRank.Bind(mine.Rank, mine.DisplayName, mine.Stage, true);
        }
    }
}