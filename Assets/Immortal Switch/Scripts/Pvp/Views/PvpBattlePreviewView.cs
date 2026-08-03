using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 06 — Battle Preview (Markdown §06, wireframe 06). Hiển thị 2 team (player + opponent)
    /// Front/Back hero, power, BattleId/RandomSeed/RulesVersion, ticket cost. Start Battle →
    /// <see cref="PvpRealBattleController.RunAsync"/> (real-time spawned battle, screen 07).
    ///
    /// PREFAB (Addressable = "PvpBattlePreviewView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtYourPower, txtOpponentPower, txtYourFront, txtYourBack,
    ///             txtOpponentFront, txtOpponentBack, txtBattleId, txtSeed, txtRules, txtTicketCost
    ///   Button: btnStartBattle, btnClose
    /// </summary>
    public class PvpBattlePreviewView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtYourPower;
        [SerializeField] private TMP_Text txtOpponentPower;
        [SerializeField] private TMP_Text txtYourFront;
        [SerializeField] private TMP_Text txtYourBack;
        [SerializeField] private TMP_Text txtOpponentFront;
        [SerializeField] private TMP_Text txtOpponentBack;
        [SerializeField] private TMP_Text txtBattleId;
        [SerializeField] private TMP_Text txtSeed;
        [SerializeField] private TMP_Text txtRules;
        [SerializeField] private TMP_Text txtTicketCost;

        [SerializeField] private Button btnStartBattle;
        [SerializeField] private Button btnClose;

        private HeroVsHeroBattleSnapshot _snapshot;

        private void Awake()
        {
            if (btnStartBattle != null) btnStartBattle.onClick.AddListener(() => StartBattleAsync().Forget());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpBattlePreviewView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _snapshot = args as HeroVsHeroBattleSnapshot;
            if (_snapshot == null) return;

            if (txtBattleId != null) txtBattleId.text = $"BattleId: {_snapshot.BattleId}";
            if (txtSeed != null) txtSeed.text = $"RandomSeed: {_snapshot.RandomSeed}";
            if (txtRules != null)
                txtRules.text = $"Rules v{_snapshot.BattleRulesVersion} • Snapshot v{_snapshot.SnapshotVersion}";
            if (txtTicketCost != null) txtTicketCost.text = "Ticket Cost: 1";

            RenderTeam(txtYourPower, txtYourFront, txtYourBack, _snapshot.Attacker);
            RenderTeam(txtOpponentPower, txtOpponentFront, txtOpponentBack, _snapshot.Defender);
        }

        private static void RenderTeam(TMP_Text power, TMP_Text front, TMP_Text back, TeamBattleSnapshot team)
        {
            if (team == null) return;
            if (power != null) power.text = $"Power: {team.TeamPower}";
            if (front != null)
                front.text = "FRONT\n" + PvpHeroNameResolver.Get(team.FrontHero?.HeroId ?? -1);
            if (back != null)
                back.text = "BACK\n" + PvpHeroNameResolver.Get(team.BackHero?.HeroId ?? -1);
        }

        private async UniTaskVoid StartBattleAsync()
        {
            if (_snapshot == null)
            {
                UIManager.Instance.ShowToast("No battle snapshot.");
                return;
            }

            var real = PvpRealBattleController.Instance;
            if (real == null)
            {
                UIManager.Instance.ShowToast("Battle controller not ready.");
                return;
            }

            // Close preview; real controller spawns 4 heroes + opens Battle HUD (screen 07).
            UIManager.Instance.Close<PvpBattlePreviewView>();
            try
            {
                await real.RunAsync(_snapshot, CancellationToken.None);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowToast($"Battle start failed: {e.Message}");
            }
        }
    }
}
