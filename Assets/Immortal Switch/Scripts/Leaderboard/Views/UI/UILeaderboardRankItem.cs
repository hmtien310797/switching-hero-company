using RecyclableScrollRect;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Leaderboard.Views.UI
{
    public class UILeaderboardRankItem : BaseItem
    {
        [SerializeField]
        private TextMeshProUGUI txtRank;

        [SerializeField]
        private TextMeshProUGUI txtPlayerName;

        [SerializeField]
        private TextMeshProUGUI txtScore;

        [SerializeField]
        private UILeaderboardReward rewardSlot;

        // Chưa có config phần thưởng theo hạng — ẩn slot reward cho tới khi có.
        public void Bind(int rank, string playerName, int stage, bool isMyRank)
        {
            txtRank.text = isMyRank ? $"Hạng Tôi\n{rank}th" : $"{rank}th";
            txtPlayerName.text = playerName;
            txtScore.text = $"{stage:N0}";

            rewardSlot.gameObject.SetActive(false);
        }
    }
}