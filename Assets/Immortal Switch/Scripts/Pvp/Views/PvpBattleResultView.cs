using System;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 08 — Battle Result (Markdown §08, wireframe 08). Win/Loss/Draw, rank change, rewards,
    /// per-hero summary, Continue/History. Result applied exactly once qua result service (§15).
    ///
    /// PREFAB (Addressable = "PvpBattleResultView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtResult, txtRankChange, txtReward, txtHeroA, txtHeroB, txtSummary
    ///   Button: btnContinue, btnHistory
    /// </summary>
    public class PvpBattleResultView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtResult;
        [SerializeField] private TMP_Text txtRankChange;
        [SerializeField] private TMP_Text txtReward;
        [SerializeField] private TMP_Text txtHeroA;
        [SerializeField] private TMP_Text txtHeroB;
        [SerializeField] private TMP_Text txtSummary;
        [SerializeField] private Button btnContinue;
        [SerializeField] private Button btnHistory;

        private PvPBattleResultRequest _request;

        private void Awake()
        {
            if (btnContinue != null)
                btnContinue.onClick.AddListener(() => UIManager.Instance.Close<PvpBattleResultView>());
            if (btnHistory != null)
                btnHistory.onClick.AddListener(() => UIManager.Instance.ShowToast("History — available in M8."));
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _request = args as PvPBattleResultRequest;
            if (_request == null) return;

            if (txtResult != null) txtResult.text = _request.Result.ToString().ToUpper();

            int rankChange = RankChangeFor(_request.Result);
            if (txtRankChange != null)
                txtRankChange.text = $"Rank {(rankChange >= 0 ? "+" : "")}{rankChange}";
            if (txtReward != null)
                txtReward.text = $"Arena Token +{TokenRewardFor(_request.Result)}";

            RenderHero(txtHeroA, _request.AttackerHeroes, 0);
            RenderHero(txtHeroB, _request.AttackerHeroes, 1);

            if (txtSummary != null)
            {
                string hash = _request.CombatLogHash ?? string.Empty;
                string hashShort = hash.Length > 8 ? hash.Substring(0, 8) : hash;
                txtSummary.text =
                    $"Duration {_request.DurationMs}ms • DMG {_request.TotalDamageDealt} • " +
                    $"Heal {_request.TotalHealingDone} • Hash {hashShort}";
            }
        }

        private static void RenderHero(TMP_Text txt, HeroBattleResultSummary[] heroes, int index)
        {
            if (txt == null || heroes == null || index >= heroes.Length) return;
            var h = heroes[index];
            if (h == null) return;
            txt.text = $"Hero {h.HeroId}: DMG {h.DamageDealt} • Ult {h.UltimateCastCount} • " +
                       $"Skill {h.ClassSkillCastCount} • BuffProcs {BuffProcs(h)}";
        }

        private static int RankChangeFor(PvPBattleResult r) => r switch
        {
            PvPBattleResult.Victory => PvpDefaults.VictoryRankGain,
            PvPBattleResult.Defeat or PvPBattleResult.Surrender => -PvpDefaults.DefeatRankLoss,
            _ => 0
        };

        private static long TokenRewardFor(PvPBattleResult r) => r switch
        {
            PvPBattleResult.Victory => PvpDefaults.VictoryArenaToken,
            PvPBattleResult.Defeat or PvPBattleResult.Surrender => PvpDefaults.DefeatArenaToken,
            _ => 0
        };

        private static int BuffProcs(HeroBattleResultSummary s)
        {
            if (s?.BuffTriggerCounts == null) return 0;
            int total = 0;
            foreach (var v in s.BuffTriggerCounts.Values) total += v;
            return total;
        }
    }
}
