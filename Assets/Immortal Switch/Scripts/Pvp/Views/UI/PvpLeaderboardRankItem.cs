using Immortal_Switch.Scripts.Pvp.Models;
using RecyclableScrollRect;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Views.UI
{
    /// <summary>
    /// Một dòng trong danh sách rankings PvP (dùng chung cho scroll list lẫn row "my rank" cố định).
    /// Hiển thị rank, nickname, rank points, formation power, tier id; highlight khi isMyRank.
    /// </summary>
    public class PvpLeaderboardRankItem : BaseItem
    {
        [SerializeField] private TextMeshProUGUI txtRank;
        [SerializeField] private TextMeshProUGUI txtPlayerName;
        [SerializeField] private TextMeshProUGUI txtRankPoints;
        [SerializeField] private TextMeshProUGUI txtPower;
        [SerializeField] private TextMeshProUGUI txtTier;

        public void Bind(PvpLeaderboardEntryModel e)
        {
            if (e == null) return;

            if (txtRank != null)
                txtRank.text = e.IsRanked ? $"Hạng Tôi\n{e.Rank}" : $"{e.Rank}";
            if (txtPlayerName != null) txtPlayerName.text = e.Nickname;
            if (txtRankPoints != null) txtRankPoints.text = $"{e.RankPoints:N0}";
            if (txtPower != null) txtPower.text = e.FormationPower;
            if (txtTier != null) txtTier.text = e.RankTierId;
        }
    }
}