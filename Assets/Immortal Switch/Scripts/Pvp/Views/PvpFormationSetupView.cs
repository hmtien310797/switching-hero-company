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
    /// Screen 02 — Formation Setup (Markdown §02, wireframe 02). Front/Back hero + 3 buff slot mỗi
    /// vị trí (Core/Support/Trigger). Validation message. Reset/Save. Tap hero → Hero Selection;
    /// tap buff slot → Buff Loadout. Save gọi IPvPFormationService.SaveFormationAsync (validate trước).
    ///
    /// PREFAB (Addressable = "PvpFormationSetupView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtTeamPower, txtFrontHero, txtBackHero, txtValidation
    ///   Button: btnFrontHero, btnBackHero, btnReset, btnSave, btnClose
    ///   TMP_Text[3]: frontBuffTexts (Core/Support/Trigger), backBuffTexts
    ///   Button[3]: frontBuffButtons, backBuffButtons
    /// </summary>
    public class PvpFormationSetupView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtTeamPower;
        [SerializeField] private TMP_Text txtFrontHero;
        [SerializeField] private Button btnFrontHero;
        [SerializeField] private TMP_Text txtBackHero;
        [SerializeField] private Button btnBackHero;

        [SerializeField] private TMP_Text[] frontBuffTexts = new TMP_Text[3];
        [SerializeField] private Button[] frontBuffButtons = new Button[3];
        [SerializeField] private TMP_Text[] backBuffTexts = new TMP_Text[3];
        [SerializeField] private Button[] backBuffButtons = new Button[3];

        [SerializeField] private TMP_Text txtValidation;
        [SerializeField] private Button btnReset;
        [SerializeField] private Button btnSave;
        [SerializeField] private Button btnClose;

        private PvpFormationSaveData _working;

        private void Awake()
        {
            if (btnFrontHero != null) btnFrontHero.onClick.AddListener(() => OpenHeroSelect(FormationSlot.Front));
            if (btnBackHero != null) btnBackHero.onClick.AddListener(() => OpenHeroSelect(FormationSlot.Back));

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                if (frontBuffButtons != null && i < frontBuffButtons.Length && frontBuffButtons[i] != null)
                    frontBuffButtons[i].onClick.AddListener(() => OpenBuffLoadout(FormationSlot.Front, (BuffSlotType)idx));
                if (backBuffButtons != null && i < backBuffButtons.Length && backBuffButtons[i] != null)
                    backBuffButtons[i].onClick.AddListener(() => OpenBuffLoadout(FormationSlot.Back, (BuffSlotType)idx));
            }

            if (btnReset != null) btnReset.onClick.AddListener(OnReset);
            if (btnSave != null) btnSave.onClick.AddListener(() => SaveAsync().Forget());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpFormationSetupView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _working = PvpManager.Instance.Facade.Formation.LoadFormation();
            if (txtTeamPower != null) txtTeamPower.text = "Team Power: (M3)";
            Refresh();
        }

        private void Refresh()
        {
            if (_working == null) return;

            if (txtFrontHero != null)
                txtFrontHero.text = "FRONT SLOT\n" + PvpHeroNameResolver.Get(_working.FrontHeroId);
            if (txtBackHero != null)
                txtBackHero.text = "BACK SLOT\n" + PvpHeroNameResolver.Get(_working.BackHeroId);

            RenderBuffRow(_working.FrontLoadout, frontBuffTexts);
            RenderBuffRow(_working.BackLoadout, backBuffTexts);

            var facade = PvpManager.Instance.Facade;
            var result = facade.Formation.Validate(_working);
            if (txtValidation != null)
                txtValidation.text = result.IsValid ? "Valid" : result.ToString();
        }

        private void RenderBuffRow(PvpBuffSlotLoadout loadout, TMP_Text[] texts)
        {
            if (loadout == null || texts == null) return;
            SetSlotText(texts, 0, loadout.Core);
            SetSlotText(texts, 1, loadout.Support);
            SetSlotText(texts, 2, loadout.Trigger);
        }

        private void SetSlotText(TMP_Text[] texts, int index, string buffId)
        {
            if (texts == null || index >= texts.Length || texts[index] == null) return;
            if (string.IsNullOrEmpty(buffId))
            {
                texts[index].text = "Empty";
                return;
            }
            var facade = PvpManager.Instance.Facade;
            if (facade.BuffCatalog.TryGet(buffId, out var buff))
                texts[index].text = $"{buff.BuffNameKey}";
            else
                texts[index].text = buffId;
        }

        private void OpenHeroSelect(FormationSlot slot)
        {
            var args = new PvpHeroSelectionArgs
            {
                Slot = slot,
                OtherSlotHeroId = slot == FormationSlot.Front ? _working.BackHeroId : _working.FrontHeroId,
                OnSelected = id =>
                {
                    if (slot == FormationSlot.Front) _working.FrontHeroId = id;
                    else _working.BackHeroId = id;
                    Refresh();
                }
            };
            UIManager.Instance.OpenPopupAsync<PvpHeroSelectionView>(args).Forget();
        }

        private void OpenBuffLoadout(FormationSlot slot, BuffSlotType slotType)
        {
            var args = new PvpBuffLoadoutArgs
            {
                Slot = slot,
                SlotType = slotType,
                Formation = _working,
                OnEquipped = Refresh
            };
            UIManager.Instance.OpenPopupAsync<PvpBuffLoadoutView>(args).Forget();
        }

        private void OnReset()
        {
            _working = PvpManager.Instance.Facade.Formation.LoadFormation();
            Refresh();
            UIManager.Instance.ShowToast("Formation reset to saved.");
        }

        private async UniTaskVoid SaveAsync()
        {
            var facade = PvpManager.Instance.Facade;
            var result = facade.Formation.Validate(_working);
            if (!result.IsValid)
            {
                if (txtValidation != null) txtValidation.text = result.ToString();
                UIManager.Instance.ShowToast("Cannot save: formation invalid.");
                return;
            }

            await facade.Formation.SaveFormationAsync(_working, CancellationToken.None);
            GameEventManager.Trigger(GameEvents.ON_PVP_FORMATION_CHANGED);
            UIManager.Instance.ShowToast("Formation saved to ES3.");
        }
    }
}
