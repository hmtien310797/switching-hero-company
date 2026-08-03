using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 10 — Gacha 3 Choices (Markdown §10, wireframe 10). 3 cards, rarity, owned/duplicate,
    /// confirm exactly one. Idempotent re-select returns stored result (DOCX §24).
    ///
    /// PREFAB (Addressable = "PvpGachaChoicesView", UILayer.Popup):
    ///   TMP_Text: txtDevBadge, txtRollId, txtPity
    ///   PvpOptionItemView[3]: cards (label + button + selectedHighlight)
    ///   Button: btnConfirm, btnClose
    /// </summary>
    public class PvpGachaChoicesView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtRollId;
        [SerializeField] private TMP_Text txtPity;
        [SerializeField] private PvpOptionItemView[] cards;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnClose;

        private FormationBuffRollSession _session;
        private string _selectedBuffId;

        private void Awake()
        {
            if (btnConfirm != null) btnConfirm.onClick.AddListener(() => ConfirmAsync().Forget());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpGachaChoicesView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _session = args as FormationBuffRollSession;
            _selectedBuffId = null;
            if (_session == null) return;

            if (txtRollId != null) txtRollId.text = $"RollId: {_session.RollId}";
            if (txtPity != null) txtPity.text = _session.IsResolved ? "Already resolved — re-select is idempotent." : "Choose one buff.";
            RenderCards();
        }

        private void RenderCards()
        {
            if (cards == null || _session?.Choices == null) return;
            var facade = PvpManager.Instance?.Facade;

            for (int i = 0; i < cards.Length && i < _session.Choices.Length; i++)
            {
                var card = cards[i];
                var choice = _session.Choices[i];
                if (card == null || choice == null) continue;

                string name = facade != null && facade.BuffCatalog.TryGet(choice.BuffId, out var def)
                    ? def.BuffNameKey : choice.BuffId;
                string status = choice.IsAlreadyOwned
                    ? $"Duplicate → {choice.DuplicateShardValue} shards"
                    : "New Buff";

                if (card.label != null) card.label.text = $"{choice.Rarity}\n{name}\n{status}";
                if (card.selectedHighlight != null)
                    card.selectedHighlight.SetActive(choice.BuffId == _selectedBuffId);

                if (card.button != null)
                {
                    string id = choice.BuffId;
                    card.button.onClick.RemoveAllListeners();
                    card.button.onClick.AddListener(() => SelectCard(id));
                }
            }
        }

        private void SelectCard(string buffId)
        {
            _selectedBuffId = buffId;
            RenderCards();
        }

        private async UniTaskVoid ConfirmAsync()
        {
            if (_session == null || string.IsNullOrEmpty(_selectedBuffId))
            {
                UIManager.Instance.ShowToast("Select a buff first.");
                return;
            }

            var facade = PvpManager.Instance?.Facade;
            if (facade?.Gacha == null) return;

            try
            {
                var result = await facade.Gacha.SelectAsync(_session.RollId, _selectedBuffId, CancellationToken.None);
                UIManager.Instance.ShowToast(result.Message);
                UIManager.Instance.Close<PvpGachaChoicesView>();
            }
            catch (Exception e) { UIManager.Instance.ShowToast($"Select failed: {e.Message}"); }
        }
    }
}
