using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 04 — Buff Inventory / Loadout (Markdown §04, wireframe 04). Tabs All/Core/Support/
    /// Trigger, target slot indicator, list owned buff, conflict message, Equip/Unequip. Equip mutate
    /// working formation (passed by ref in args); caller (PvpFormationSetupView) Save mới persist ES3.
    /// One BuffId chỉ equipped 1 vị trí (DOCX §17) — CanEquip trả conflict reason.
    ///
    /// PREFAB (Addressable = "PvpBuffLoadoutView", UILayer.Popup):
    ///   TMP_Text: txtDevBadge, txtTitle, txtTargetSlot, txtConflict
    ///   Button: tabAll, tabCore, tabSupport, tabTrigger, btnEquip, btnUnequip, btnClose
    ///   Transform: listContainer
    ///   PvpOptionItemView: itemPrefab
    /// </summary>
    public class PvpBuffLoadoutView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_Text txtTargetSlot;
        [SerializeField] private TMP_Text txtConflict;

        [SerializeField] private Button tabAll;
        [SerializeField] private Button tabCore;
        [SerializeField] private Button tabSupport;
        [SerializeField] private Button tabTrigger;

        [SerializeField] private Transform listContainer;
        [SerializeField] private PvpOptionItemView itemPrefab;

        [SerializeField] private Button btnEquip;
        [SerializeField] private Button btnUnequip;
        [SerializeField] private Button btnClose;

        private PvpBuffLoadoutArgs _args;
        private string _selectedBuffId;
        private int _tab; // 0=All,1=Core,2=Support,3=Trigger

        private void Awake()
        {
            if (tabAll != null) tabAll.onClick.AddListener(() => { _tab = 0; Refresh(); });
            if (tabCore != null) tabCore.onClick.AddListener(() => { _tab = 1; Refresh(); });
            if (tabSupport != null) tabSupport.onClick.AddListener(() => { _tab = 2; Refresh(); });
            if (tabTrigger != null) tabTrigger.onClick.AddListener(() => { _tab = 3; Refresh(); });
            if (btnEquip != null) btnEquip.onClick.AddListener(OnEquip);
            if (btnUnequip != null) btnUnequip.onClick.AddListener(OnUnequip);
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpBuffLoadoutView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _args = args as PvpBuffLoadoutArgs;
            _tab = 0;

            if (txtTitle != null) txtTitle.text = _args != null ? $"SELECT {_args.SlotType} BUFF" : "SELECT BUFF";
            if (txtTargetSlot != null && _args != null)
                txtTargetSlot.text = $"Target Slot: {_args.Slot} • {_args.SlotType}";

            // Pre-select the buff currently occupying this slot (if any).
            _selectedBuffId = GetCurrentSlotBuffId();
            Refresh();
        }

        private void Refresh()
        {
            if (listContainer != null) ClearContainer();
            if (itemPrefab == null || _args == null) return;

            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;

            var owned = facade.BuffInventory.LoadOwnedBuffs();
            foreach (var b in owned.Buffs)
            {
                if (b == null || !b.IsUnlocked) continue;

                if (!facade.BuffCatalog.TryGet(b.BuffId, out var def))
                    continue; // unknown buff — skip

                // Tab filter (Core/Support/Trigger). All = 0.
                if (_tab > 0 && (int)def.SlotType != _tab - 1)
                    continue;

                var item = Instantiate(itemPrefab, listContainer);
                string status = ResolveStatus(b.BuffId);
                if (item.label != null)
                    item.label.text = $"{def.BuffNameKey}\n{def.Rarity} • Lv.{b.Level} • {status}";

                bool isSelected = b.BuffId == _selectedBuffId;
                if (item.selectedHighlight != null) item.selectedHighlight.SetActive(isSelected);

                if (item.button != null)
                {
                    string id = b.BuffId;
                    item.button.onClick.AddListener(() => OnSelectBuff(id));
                }
            }

            UpdateConflictAndButtons();
        }

        private string ResolveStatus(string buffId)
        {
            var facade = PvpManager.Instance.Facade;
            string current = GetCurrentSlotBuffId();
            if (buffId == current) return "Current (Equipped)";
            if (facade.BuffInventory.IsEquipped(_args.Formation, buffId)) return "Equipped on other position";
            return "Available";
        }

        private void OnSelectBuff(string buffId)
        {
            _selectedBuffId = buffId;
            Refresh();
        }

        private void UpdateConflictAndButtons()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null || _args == null) return;

            bool canEquip = false;
            string reason = null;

            if (!string.IsNullOrEmpty(_selectedBuffId))
                canEquip = facade.BuffInventory.CanEquip(_args.Formation, _selectedBuffId,
                    _args.Slot, _args.SlotType, out reason);

            if (txtConflict != null)
                txtConflict.text = string.IsNullOrEmpty(reason) ? string.Empty : reason;

            if (btnEquip != null) btnEquip.interactable = canEquip;
            if (btnUnequip != null)
                btnUnequip.interactable = !string.IsNullOrEmpty(GetCurrentSlotBuffId());
        }

        private void OnEquip()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null || _args == null || string.IsNullOrEmpty(_selectedBuffId)) return;

            if (!facade.BuffInventory.CanEquip(_args.Formation, _selectedBuffId,
                    _args.Slot, _args.SlotType, out var reason))
            {
                UIManager.Instance.ShowToast(string.IsNullOrEmpty(reason) ? "Cannot equip." : reason);
                return;
            }

            facade.BuffInventory.Equip(_args.Formation, _selectedBuffId, _args.Slot, _args.SlotType);
            _args.OnEquipped?.Invoke();
            UIManager.Instance.Close<PvpBuffLoadoutView>();
        }

        private void OnUnequip()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null || _args == null) return;

            if (facade.BuffInventory.Unequip(_args.Formation, _args.Slot, _args.SlotType))
            {
                _args.OnEquipped?.Invoke();
            }
            UIManager.Instance.Close<PvpBuffLoadoutView>();
        }

        private string GetCurrentSlotBuffId()
        {
            if (_args?.Formation == null) return null;
            var loadout = _args.Slot == FormationSlot.Front
                ? _args.Formation.FrontLoadout
                : _args.Formation.BackLoadout;
            return loadout?.Get(_args.SlotType);
        }

        private void ClearContainer()
        {
            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);
        }
    }
}
