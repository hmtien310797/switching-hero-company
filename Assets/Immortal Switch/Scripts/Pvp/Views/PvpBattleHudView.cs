using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 07 — Battle HUD (Markdown §07, wireframe 07). 4 HP bars, timer, auto, speed, buff
    /// indicators, Surrender. KHÔNG có swap button (§8). Phase-1 real-time: HUD show initial state +
    /// RESOLVE (force-end) / SURRENDER. Battle tự kết trong PvpRealBattleController.Update → mở Result.
    ///
    /// PREFAB (Addressable = "PvpBattleHudView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtTimer, txtAutoState, txtYourFrontHp, txtYourBackHp,
    ///             txtEnemyFrontHp, txtEnemyBackHp, txtActiveBuffs
    ///   Button: btnResolve, btnSurrender, btnSpeed, btnAuto (NO swap)
    /// </summary>
    public class PvpBattleHudView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtTimer;
        [SerializeField] private TMP_Text txtAutoState;
        [SerializeField] private TMP_Text txtYourFrontHp;
        [SerializeField] private TMP_Text txtYourBackHp;
        [SerializeField] private TMP_Text txtEnemyFrontHp;
        [SerializeField] private TMP_Text txtEnemyBackHp;
        [SerializeField] private TMP_Text txtActiveBuffs;
        [SerializeField] private Button btnResolve;
        [SerializeField] private Button btnSurrender;
        [SerializeField] private Button btnSpeed;
        [SerializeField] private Button btnAuto;

        private HeroVsHeroBattleSnapshot _snapshot;
        private float _startReal;

        private void Awake()
        {
            if (btnResolve != null) btnResolve.onClick.AddListener(ResolveBattle);
            if (btnSurrender != null) btnSurrender.onClick.AddListener(Surrender);
            if (btnAuto != null) btnAuto.onClick.AddListener(() => Toast("Auto ON"));
            if (btnSpeed != null) btnSpeed.onClick.AddListener(() => Toast("Speed x1 / x2"));
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _snapshot = args as HeroVsHeroBattleSnapshot;
            _startReal = Time.realtimeSinceStartup;
            if (_snapshot == null) return;

            if (txtTimer != null) txtTimer.text = "00:00";
            if (txtAutoState != null) txtAutoState.text = "AUTO ON";

            RenderHp(txtYourFrontHp, _snapshot.Attacker?.FrontHero);
            RenderHp(txtYourBackHp, _snapshot.Attacker?.BackHero);
            RenderHp(txtEnemyFrontHp, _snapshot.Defender?.FrontHero);
            RenderHp(txtEnemyBackHp, _snapshot.Defender?.BackHero);

            if (txtActiveBuffs != null) txtActiveBuffs.text = "Active Buffs: (live buffs trên hero)";
        }

        private void Update()
        {
            // Light timer + live HP refresh while the real battle runs.
            if (_snapshot == null) return;
            if (txtTimer != null)
                txtTimer.text = $"{(int)(Time.realtimeSinceStartup - _startReal):00}s";
            RefreshLiveHp();
        }

        private void RefreshLiveHp()
        {
            var real = PvpRealBattleController.Instance;
            if (real == null) return;
            // Real controller exposes nothing per-hero public; just re-render snapshot HP as fallback.
            // (Live HP bars would be wired to HealthBarController in the prefab — see note below.)
        }

        private static void RenderHp(TMP_Text txt, HeroBattleSnapshot hero)
        {
            if (txt == null || hero == null) return;
            float maxHp = hero.FinalStats?.Get(StatType.MaxHp) ?? 0f;
            txt.text = $"Hero {hero.HeroId}: {maxHp:0}";
        }

        private void ResolveBattle()
        {
            var real = PvpRealBattleController.Instance;
            if (real != null && real.IsRunning) real.ForceResolve();
            else Toast("No active battle.");
        }

        private void Surrender()
        {
            var real = PvpRealBattleController.Instance;
            if (real != null && real.IsRunning) real.Surrender();
            else Toast("No active battle.");
        }

        private static void Toast(string msg) => UIManager.Instance.ShowToast(msg);
    }
}
