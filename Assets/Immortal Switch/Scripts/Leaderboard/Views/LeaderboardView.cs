using Common;
using Immortal_Switch.Scripts.Leaderboard.Views.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.Leaderboard.Views
{
    public class LeaderboardView : AnimatedUIView
    {
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

        private void Awake()
        {
            // just test UI
            Bind();

            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<LeaderboardView>();
        }

        private void OnDestroy()
        {
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        public void Bind()
        {
            countdown.Bind(5 * 60, OnRefreshRank);

            OnRefreshRank();
            RefreshMyRank();
            RefreshTop();
        }

        private void RefreshTop()
        {
            top1.Bind("A1", 1000, 1, Random.Range(100, 50000));
            top2.Bind("A2", 100, 2, Random.Range(100, 50000));
            top3.Bind("A3", 10, 3, Random.Range(100, 50000));
        }

        private void RefreshMyRank()
        {
            var playerName = UserDataCache.Instance.DisplayName;
            var stage = 1;
            var rewardQuantity = Random.Range(100, 50000);

            myRank.Bind(200, playerName, stage, true, rewardQuantity);
        }

        private void OnRefreshRank()
        {
            rankRecyclableView.Bind(100, OnRankResolver);
        }

        private LeaderboardRankResolver OnRankResolver(int arg)
        {
            return new LeaderboardRankResolver
            {
                PlayerName = $"{arg + 1} user",
                Rank = arg,
                Stage = (arg + 1) * Random.Range(100, 500),
                RewardQuantity = Random.Range(100, 500),
            };
        }
    }
}