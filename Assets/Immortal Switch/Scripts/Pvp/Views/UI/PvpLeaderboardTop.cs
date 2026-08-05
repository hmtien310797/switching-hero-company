using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Pvp.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views.UI
{
    /// <summary>
    /// Bục podium top1/2/3 trên PvP Main. Hiển thị nickname, formation power, tier id (và 2 hero
    /// nếu có). Phần hero star/tier có thể bỏ trống nếu prefab chưa có slot hero.
    /// </summary>
    public class PvpLeaderboardTop : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtPlayerName;
        [SerializeField] private TextMeshProUGUI txtPower;
        [SerializeField] private TextMeshProUGUI txtTier;
        [SerializeField] private TextMeshProUGUI txtHeroes; // optional "heroId*:star/tier, ..."
        [SerializeField] private StarHeroDisplay[] starHeroDisplays;
        [SerializeField] private Image[] heroImages;
        [SerializeField] private Image[] heroTierImages;

        public void Bind(PvpLeaderboardEntryModel e)
        {
            if (e == null) return;

            if (txtPlayerName != null) txtPlayerName.text = e.Nickname;
            if (txtPower != null) txtPower.text = e.FormationPower;
            if (txtTier != null) txtTier.text = e.RankTierId;

            if (txtHeroes != null && e.FormationHeroes != null && e.FormationHeroes.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < e.FormationHeroes.Count; i++)
                {
                    if (i > 0) sb.Append("  ");
                    var h = e.FormationHeroes[i];
                    sb.Append($"{h.HeroId}★{h.Star} T{h.Tier}");
                    starHeroDisplays[i].SetStar(h.Star);
                    heroImages[i].sprite = HeroImageService.GetHeroIcon(h.HeroId);
                    heroTierImages[i].sprite = HeroImageService.GetHeroTierBackground(h.Tier);
                }
                txtHeroes.text = sb.ToString();
            }
        }
    }
}