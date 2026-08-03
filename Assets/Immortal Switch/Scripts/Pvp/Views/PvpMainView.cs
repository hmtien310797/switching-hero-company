using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 01 — PvP Main (Markdown §01, wireframe 01). Hiển thị season/rank/ticket/token,
    /// formation summary + các nút điều hướng. LOCAL/MOCK dev badge (DOCX §37).
    ///
    /// PREFAB (Addressable address = "PvpMainView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtSeason, txtRank, txtTickets, txtTokens, txtFrontHero, txtBackHero
    ///   Button:   btnFindMatch, btnFormation, btnBuffs, btnRankSeason, btnHistory, btnClose
    /// </summary>
    public class PvpMainView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtSeason;
        [SerializeField] private TMP_Text txtRank;
        [SerializeField] private TMP_Text txtTickets;
        [SerializeField] private TMP_Text txtTokens;
        [SerializeField] private TMP_Text txtFrontHero;
        [SerializeField] private TMP_Text txtBackHero;

        [SerializeField] private Button btnFindMatch;
        [SerializeField] private Button btnFormation;
        [SerializeField] private Button btnBuffs;
        [SerializeField] private Button btnRankSeason;
        [SerializeField] private Button btnHistory;
        [SerializeField] private Button btnClose;

        private void Awake()
        {
            if (btnFindMatch != null) btnFindMatch.onClick.AddListener(() => OnFindMatchAsync().Forget());
            if (btnFormation != null) btnFormation.onClick.AddListener(() => OpenAsync<PvpFormationSetupView>());
            if (btnBuffs != null) btnBuffs.onClick.AddListener(() => OpenAsync<PvpBuffInventoryView>());
            if (btnRankSeason != null) btnRankSeason.onClick.AddListener(() => OpenAsync<PvpRankSeasonView>());
            if (btnHistory != null) btnHistory.onClick.AddListener(() => OpenAsync<PvpHistoryView>());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpMainView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            Refresh();
        }

        private void Refresh()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;

            var profile = facade.Profile?.GetCurrent();
            if (profile != null)
            {
                if (txtSeason != null) txtSeason.text = profile.SeasonId;
                if (txtRank != null) txtRank.text = $"{profile.RankTier} • {profile.RankPoint} pts";
                if (txtTickets != null) txtTickets.text = $"{profile.ArenaTicket}/{PvpDefaults.MaxTicketsCap}";
                if (txtTokens != null) txtTokens.text = $"{profile.ArenaToken}";
            }

            var formation = facade.Formation?.LoadFormation();
            if (formation != null)
            {
                if (txtFrontHero != null) txtFrontHero.text = "FRONT\n" + PvpHeroNameResolver.Get(formation.FrontHeroId);
                if (txtBackHero != null) txtBackHero.text = "BACK\n" + PvpHeroNameResolver.Get(formation.BackHeroId);
            }
        }

        private async UniTaskVoid OnFindMatchAsync()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade?.Matchmaking == null)
            {
                Toast("Matchmaking not ready.");
                return;
            }

            var formation = facade.Formation.LoadFormation();
            var result = facade.Formation.Validate(formation);
            if (!result.IsValid)
            {
                Toast($"Formation invalid: {result}");
                return;
            }

            var profile = facade.Profile.GetCurrent();
            if (profile == null || profile.ArenaTicket <= 0)
            {
                Toast("No Arena tickets.");
                return;
            }

            // Show matching overlay, run FindMatchAsync (creates BattleId + PendingBattle), then preview.
            UIManager.Instance.OpenPopupAsync<PvpMatchingView>().Forget();
            try
            {
                var snapshot = await facade.Matchmaking.FindMatchAsync(CancellationToken.None);
                UIManager.Instance.Close<PvpMatchingView>();
                UIManager.Instance.OpenPopupAsync<PvpBattlePreviewView>(snapshot).Forget();
            }
            catch (Exception e)
            {
                UIManager.Instance.Close<PvpMatchingView>();
                Toast($"Match failed: {e.Message}");
            }
        }

        private static void OpenAsync<T>() where T : UIView
        {
            UIManager.Instance.OpenPopupAsync<T>().Forget();
        }

        private static void Toast(string msg) => UIManager.Instance.ShowToast(msg);
    }
}
