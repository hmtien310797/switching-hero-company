using System;
using System.Collections.Generic;
using Common;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 03 — Hero Selection (Markdown §03, wireframe 03). List hero player sở hữu, tabs
    /// All/Owned/Power, search, chọn 1 → callback OnSelected(heroId). Hero đã gắn ở slot kia bị
    /// disable (DOCX §3). Đọc từ UserDataCache (server-owned), không gọi ES3.
    ///
    /// PREFAB (Addressable = "PvpHeroSelectionView", UILayer.Popup):
    ///   TMP_Text: txtDevBadge, txtTitle
    ///   TMP_InputField: inpSearch
    ///   Button: tabAll, tabOwned, tabPower, btnClose
    ///   Transform: listContainer
    ///   PvpOptionItemView: itemPrefab
    /// </summary>
    public class PvpHeroSelectionView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_InputField inpSearch;
        [SerializeField] private Button tabAll;
        [SerializeField] private Button tabOwned;
        [SerializeField] private Button tabPower;
        [SerializeField] private Transform listContainer;
        [SerializeField] private PvpOptionItemView itemPrefab;
        [SerializeField] private Button btnClose;

        private PvpHeroSelectionArgs _args;
        private int _tab; // 0=All, 1=Owned, 2=Power

        private void Awake()
        {
            if (tabAll != null) tabAll.onClick.AddListener(() => { _tab = 0; Refresh(); });
            if (tabOwned != null) tabOwned.onClick.AddListener(() => { _tab = 1; Refresh(); });
            if (tabPower != null) tabPower.onClick.AddListener(() => { _tab = 2; Refresh(); });
            if (inpSearch != null) inpSearch.onValueChanged.AddListener(_ => Refresh());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpHeroSelectionView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _args = args as PvpHeroSelectionArgs;
            _tab = 0;
            if (txtTitle != null) txtTitle.text = _args != null ? $"SELECT {_args.Slot} HERO" : "SELECT HERO";
            Refresh();
        }

        private void Refresh()
        {
            if (listContainer == null || itemPrefab == null) return;
            ClearContainer();

            var owned = UserDataCache.Instance?.HeroList?.Owned;
            if (owned == null) return;

            var list = new List<HeroInstance>(owned);

            string q = inpSearch != null ? inpSearch.text : null;
            if (!string.IsNullOrEmpty(q))
                list = list.FindAll(h => h != null && (h.Name ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

            // Owned == All ở đây (server list toàn owned). Power = sort by Star desc (proxy).
            if (_tab == 2)
                list.Sort((a, b) => (b != null ? b.Star : 0).CompareTo(a != null ? a.Star : 0));

            foreach (var h in list)
            {
                if (h == null) continue;
                var item = Instantiate(itemPrefab, listContainer);
                if (item.label != null)
                    item.label.text = $"{h.Name}\nStar {h.Star} • {h.Rarity} • Lv.{h.Level}";

                bool isOtherSlot = h.HeroId == (_args?.OtherSlotHeroId ?? -1);
                if (item.disabledOverlay != null) item.disabledOverlay.SetActive(isOtherSlot);
                if (item.button != null)
                {
                    item.button.interactable = !isOtherSlot;
                    int heroId = h.HeroId;
                    item.button.onClick.AddListener(() => OnSelectHero(heroId));
                }
            }
        }

        private void OnSelectHero(int heroId)
        {
            _args?.OnSelected?.Invoke(heroId);
            UIManager.Instance.Close<PvpHeroSelectionView>();
        }

        private void ClearContainer()
        {
            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);
        }
    }
}
