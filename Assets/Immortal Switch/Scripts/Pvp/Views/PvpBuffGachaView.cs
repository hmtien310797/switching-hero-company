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
    /// Screen 09 — Buff Gacha Home (Markdown §09, wireframe 09). Pool/cost/currency/pity + Roll.
    /// Pending roll restore (DOCX §24 — "A pending roll must survive application restart").
    ///
    /// PREFAB (Addressable = "PvpBuffGachaView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtPool, txtCost, txtToken, txtPity, txtPending
    ///   Button: btnRoll, btnRates, btnClose
    /// </summary>
    public class PvpBuffGachaView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtPool;
        [SerializeField] private TMP_Text txtCost;
        [SerializeField] private TMP_Text txtToken;
        [SerializeField] private TMP_Text txtPity;
        [SerializeField] private TMP_Text txtPending;
        [SerializeField] private Button btnRoll;
        [SerializeField] private Button btnRates;
        [SerializeField] private Button btnClose;

        private void Awake()
        {
            if (btnRoll != null) btnRoll.onClick.AddListener(() => OnRollOrResumeAsync().Forget());
            if (btnRates != null) btnRates.onClick.AddListener(() => UIManager.Instance.ShowToast("Rates config-driven (DOCX §25)."));
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpBuffGachaView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            RefreshAsync().Forget();
        }

        private async UniTaskVoid RefreshAsync()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;

            var profile = facade.Profile?.GetCurrent();
            if (txtToken != null && profile != null) txtToken.text = $"Arena Token: {profile.ArenaToken}";
            if (txtCost != null) txtCost.text = "Roll Cost: 500";
            if (txtPool != null) txtPool.text = "Standard Buff Pool";

            var state = await facade.Gacha.LoadStateAsync(CancellationToken.None);
            bool hasPending = !string.IsNullOrEmpty(state?.PendingRollId);
            if (txtPity != null)
                txtPity.text = $"Pity: {state?.PityByPool?.Count ?? 0} pool(s) tracked";
            if (txtPending != null)
                txtPending.text = hasPending ? "Pending roll found — resume to choose." : string.Empty;
            if (btnRoll != null)
            {
                var label = btnRoll.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null) label.text = hasPending ? "RESUME PENDING" : "ROLL 3 CHOICES";
            }
        }

        private async UniTaskVoid OnRollOrResumeAsync()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade?.Gacha == null) return;

            // Restore pending roll nếu có (DOCX §24 — survive restart).
            var pending = facade.Gacha.GetPendingRoll();
            if (pending != null && !pending.IsResolved)
            {
                UIManager.Instance.OpenPopupAsync<PvpGachaChoicesView>(pending).Forget();
                return;
            }

            try
            {
                var session = await facade.Gacha.CreateRollAsync("standard", CancellationToken.None);
                UIManager.Instance.OpenPopupAsync<PvpGachaChoicesView>(session).Forget();
            }
            catch (System.Exception e)
            {
                UIManager.Instance.ShowToast($"Roll failed: {e.Message}");
            }
        }
    }
}
