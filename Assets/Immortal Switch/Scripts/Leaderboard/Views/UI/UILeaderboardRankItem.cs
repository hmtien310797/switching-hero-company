using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.Constants;
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

        public void Bind(int rank, string playerName, int stage, bool isMyRank, BigNumber rewardQuantity)
        {
            txtRank.text = isMyRank ? $"Hạng Tôi\n{rank}th" : $"{rank}th";
            txtPlayerName.text = playerName;
            txtScore.text = $"{stage:N0}";

            rewardSlot.Bind(ItemIdConstants.DIAMOND, rewardQuantity);
        }
    }
}