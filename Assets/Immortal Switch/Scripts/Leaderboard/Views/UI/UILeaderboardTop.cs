using Immortal_Switch.Scripts.Shared.UI;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Leaderboard.Views.UI
{
    public class UILeaderboardTop : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtPlayerName;

        [SerializeField]
        private TextMeshProUGUI txtScore;

        [SerializeField]
        private UILeaderboardReward rewardSlot;

        [SerializeField]
        private SkeletonGraphic heroSkeleton;

        // --- Private Fields ---

        // Chưa có config phần thưởng theo hạng — ẩn slot reward cho tới khi có (xem
        // UILeaderboardRankItem.Bind cùng lý do). heroId/RefreshSpine cũng chưa có nguồn dữ
        // liệu (leaderboard record không mang theo hero showcase của người chơi).
        public void Bind(string playerName, int score)
        {
            txtPlayerName.text = playerName;
            txtScore.text = $"{score:N0}";

            rewardSlot.gameObject.SetActive(false);
        }
    }
}