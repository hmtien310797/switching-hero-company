using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pvp.Models;
using RecyclableScrollRect;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views.UI
{
    /// <summary>
    /// Một dòng trong danh sách rankings PvP (dùng chung cho scroll list lẫn row "my rank" cố định).
    /// Hiển thị rank, nickname, rank points, formation power, tier id; highlight khi isMyRank.
    /// </summary>
    public class PvpLeaderboardRankItem : BaseItem
    {
        [SerializeField] private Image imageTier;
        [SerializeField] private Image imageRank;
        [SerializeField] private TextMeshProUGUI txtRank;
        [SerializeField] private TextMeshProUGUI txtPlayerName;
        [SerializeField] private TextMeshProUGUI txtRankPoints;
        [SerializeField] private TextMeshProUGUI txtPower;
        [SerializeField] private TextMeshProUGUI txtTier;
        [SerializeField] private TextMeshProUGUI txtTierLevel;

        public void Bind(PvpLeaderboardEntryModel e, PvpRankInfoSo pvpRankInfoSo)
        {
            if (e == null) return;
            Sprite rankImage = pvpRankInfoSo.GetIconAtRank(e.Rank);
            if (imageRank != null && rankImage != null)
            {
                imageRank.enabled = true;
                imageRank.sprite = rankImage;
            }
            else
            {
                imageRank.enabled = false;
            }

            imageTier.sprite = pvpRankInfoSo.GetIconAtTier(e.RankTierId);
            
            if (txtRank != null)
                txtRank.text = e.IsRanked ? $"Hạng Tôi\n{e.Rank}" : $"{e.Rank}";
            if (txtPlayerName != null) txtPlayerName.text = e.Nickname;
            if (txtRankPoints != null) txtRankPoints.text = $"{e.RankPoints:N0}";
            if (txtPower != null) txtPower.text = e.FormationPower.ToBigNumber().ToString();
            if (txtTier != null) txtTier.text = e.RankTierId;
            txtTierLevel.text = e.RankTierLevel.ToString();
        }
    }
}