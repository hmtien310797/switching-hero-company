using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Views.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 01 — PvP Main (Markdown §01, wireframe 01). Hiển thị season/rank/ticket/token, formation
    /// summary + các nút điều hướng + LEADERBOARD (top1/2/3 podium + 50-rank RecyclableScrollRect +
    /// my rank + weekly-reset countdown). LOCAL/MOCK dev badge (DOCX §37).
    ///
    /// PREFAB (Addressable address = "PvpMainView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtSeason, txtRank, txtTickets, txtTokens, txtFrontHero, txtBackHero,
    ///             txtWeeklyReset
    ///   PvpLeaderboardTop: top1, top2, top3
    ///   PvpLeaderboardRecyclableView: rankRecyclableView (chứa RSR + item prefab PvpLeaderboardRankItem)
    ///   PvpLeaderboardRankItem: myRank (row cố định)
    ///   Button: btnFindMatch, btnFormation, btnBuffs, btnRankSeason, btnHistory, btnClose
    ///
    /// DATA: leaderboard từ <see cref="IPvPLeaderboardService"/> (Phase-1 = LocalPvPLeaderboardService
    /// mock 50 record — server chưa làm). TODO server: thay impl service.
    /// </summary>
    public class PvpMainView : BouncePopupUIView
    {
        [SerializeField] private Button btnFindMatch;
        [SerializeField] private Button btnFormation;
        [SerializeField] private Button btnBuffs;
        [SerializeField] private Button btnRankSeason;
        [SerializeField] private Button btnHistory;
        [SerializeField] private Button btnClose;

        // ── Leaderboard ───────────────────────────────────────────────────────────
        [Header("Leaderboard")]
        [SerializeField] private TMP_Text txtWeeklyReset;
        [SerializeField] private PvpLeaderboardTop top1;
        [SerializeField] private PvpLeaderboardTop top2;
        [SerializeField] private PvpLeaderboardTop top3;
        [SerializeField] private PvpLeaderboardRecyclableView rankRecyclableView;
        [SerializeField] private PvpLeaderboardRankItem myRank;
        
        [SerializeField] private PvpRankInfoSo pvpRankInfo;

        private PvpLeaderboardResponseModel _leaderboard;
        private long _weeklyResetAtUtc;

        private void Awake()
        {
            if (btnFindMatch != null) btnFindMatch.onClick.AddListener(() => OnFindMatchAsync().Forget());
            if (btnHistory != null) btnHistory.onClick.AddListener(OpenAsync<PvpHistoryView>);
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpMainView>());
            if (btnRankSeason != null) btnRankSeason.onClick.AddListener(OpenAsync<PvpRewardView>);
        }

        public override void OnShow(object args)
        {
            Refresh();
            LoadLeaderboardAsync().Forget();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || txtWeeklyReset == null || _weeklyResetAtUtc <= 0) return;
            long remain = _weeklyResetAtUtc - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            txtWeeklyReset.text = "Weekly reset: " + FormatCountdown(remain);
        }

        private void Refresh()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;

            var profile = facade.Profile?.GetCurrent();
            var formation = facade.Formation?.LoadFormation();
        }

        private async UniTaskVoid LoadLeaderboardAsync()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade?.Leaderboard == null) return;

            try
            {
                _leaderboard = await facade.Leaderboard.GetLeaderboardAsync(CancellationToken.None);
                RenderLeaderboard(_leaderboard);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvP] LoadLeaderboard failed: {e.Message}");
                Toast("Không thể tải bảng xếp hạng.");
            }
        }

        private void RenderLeaderboard(PvpLeaderboardResponseModel data)
        {
            if (data == null) return;

            _weeklyResetAtUtc = data.WeeklyResetAtUtc;
            if (txtWeeklyReset != null)
                txtWeeklyReset.text = "Weekly reset: " + FormatCountdown(data.WeeklyResetAtUtc - data.ServerTimeUtc);

            if (top1 != null) { top1.gameObject.SetActive(data.Top1 != null); if (data.Top1 != null) top1.Bind(data.Top1, pvpRankInfo); }
            if (top2 != null) { top2.gameObject.SetActive(data.Top2 != null); if (data.Top2 != null) top2.Bind(data.Top2, pvpRankInfo); }
            if (top3 != null) { top3.gameObject.SetActive(data.Top3 != null); if (data.Top3 != null) top3.Bind(data.Top3, pvpRankInfo); }

            if (rankRecyclableView != null && data.Rankings != null)
                rankRecyclableView.Bind(data.Rankings.Count, i => data.Rankings[i]);

            if (myRank != null)
            {
                bool hasMyRank = data.MyRank != null && data.MyRank.IsRanked;
                myRank.gameObject.SetActive(hasMyRank);
                if (hasMyRank) myRank.Bind(data.MyRank, pvpRankInfo);
            }
        }

        private static string FormatCountdown(long seconds)
        {
            if (seconds < 0) seconds = 0;
            long d = seconds / 86400;
            long h = (seconds % 86400) / 3600;
            long m = (seconds % 3600) / 60;
            long s = seconds % 60;
            return $"{d}d {h:D2}:{m:D2}:{s:D2}";
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
            //UIManager.Instance.OpenPopupAsync<PvpMatchingView>().Forget();
            try
            {
                //find match from server
                var snapshot = await facade.Matchmaking.FindMatchAsync(CancellationToken.None);
                //UIManager.Instance.Close<PvpMatchingView>();
                //UIManager.Instance.OpenPopupAsync<PvpBattlePreviewView>(snapshot).Forget();
                //run local battle, snapshot will be get from server to make preview info for ui
                PvpQuickBattle.StartAsync().Forget();
                UIManager.Instance.Close<PvpMainView>();
            }
            catch (Exception e)
            {
                //UIManager.Instance.Close<PvpMatchingView>();
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