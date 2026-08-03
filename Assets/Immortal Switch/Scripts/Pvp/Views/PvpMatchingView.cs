using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 05 — Find Match / Matching (Markdown §05, wireframe 05). Overlay "SEARCHING..." trong
    /// lúc FindMatchAsync chạy. <b>FLAGGED:</b> Phase-1 matchmaking instant (local) nên Cancel chủ yếu
    /// cosmetic; free-cancel-before-BattleId áp dụng khi matchmaking async (server Phase-2). Sau khi
    /// BattleId tạo, rời = surrender/defeat (DOCX §5 Markdown).
    ///
    /// PREFAB (Addressable = "PvpMatchingView", UILayer.Popup):
    ///   TMP_Text: txtDevBadge, txtStatus, txtRankRange, txtCurrentTeam
    ///   Button: btnCancel
    /// </summary>
    public class PvpMatchingView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtStatus;
        [SerializeField] private TMP_Text txtRankRange;
        [SerializeField] private TMP_Text txtCurrentTeam;
        [SerializeField] private Button btnCancel;

        private void Awake()
        {
            if (btnCancel != null)
                btnCancel.onClick.AddListener(() => UIManager.Instance.Close<PvpMatchingView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            if (txtStatus != null) txtStatus.text = "SEARCHING...";

            var facade = PvpManager.Instance?.Facade;
            var profile = facade?.Profile?.GetCurrent();
            if (txtRankRange != null && profile != null)
                txtRankRange.text = $"Rank Range: ±150 pts ({profile.RankTier})";

            var formation = facade?.Formation?.LoadFormation();
            if (txtCurrentTeam != null && formation != null)
                txtCurrentTeam.text =
                    $"Front: {PvpHeroNameResolver.Get(formation.FrontHeroId)} • Back: {PvpHeroNameResolver.Get(formation.BackHeroId)}";
        }
    }
}
