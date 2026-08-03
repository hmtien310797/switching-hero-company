using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 12 — Rank &amp; Season (Markdown §12, wireframe 12). Rank tier/progress, season time,
    /// milestone rewards, claim. Phase-1 local simulation (DOCX §32 — rank/reward config tách riêng).
    ///
    /// PREFAB (Addressable = "PvpRankSeasonView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtTier, txtProgress, txtSeasonTime, txtRewards
    ///   Button: btnClaim, btnClose
    /// </summary>
    public class PvpRankSeasonView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtTier;
        [SerializeField] private TMP_Text txtProgress;
        [SerializeField] private TMP_Text txtSeasonTime;
        [SerializeField] private TMP_Text txtRewards;
        [SerializeField] private Button btnClaim;
        [SerializeField] private Button btnClose;

        private void Awake()
        {
            if (btnClaim != null) btnClaim.onClick.AddListener(OnClaim);
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpRankSeasonView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            Refresh();
        }

        private void Refresh()
        {
            var facade = PvpManager.Instance?.Facade;
            var profile = facade?.Profile?.GetCurrent();
            if (profile == null) return;

            if (txtTier != null) txtTier.text = profile.RankTier.ToString().ToUpper();
            if (txtProgress != null) txtProgress.text = $"Rank Points: {profile.RankPoint}";

            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remain = profile.SeasonEndsAtUnix - now;
            if (txtSeasonTime != null)
                txtSeasonTime.text = remain > 0 ? $"Season ends in {remain / 86400}d {(remain % 86400) / 3600}h" : "Season ended";

            if (txtRewards != null)
                txtRewards.text = $"Claimed milestones: {profile.ClaimedRankMilestoneRewards.Count}";

            if (btnClaim != null)
                btnClaim.interactable = profile.RankPoint >= 100 && !profile.ClaimedRankMilestoneRewards.Contains("bronze");
        }

        private void OnClaim()
        {
            var facade = PvpManager.Instance?.Facade;
            var profile = facade?.Profile?.GetCurrent();
            if (profile == null) return;

            if (profile.RankPoint >= 100 && !profile.ClaimedRankMilestoneRewards.Contains("bronze"))
            {
                profile.ClaimedRankMilestoneRewards.Add("bronze");
                profile.ArenaToken += 200; // reward (FLAGGED: Phase-1 balance, §32)
                facade.Profile.SaveAsync(profile, CancellationToken.None).Forget();
                UIManager.Instance.ShowToast("Claimed Bronze milestone: +200 Arena Token.");
                Refresh();
            }
        }
    }
}
