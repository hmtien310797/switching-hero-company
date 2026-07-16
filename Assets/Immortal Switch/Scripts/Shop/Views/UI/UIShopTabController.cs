using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopTabController : MonoBehaviour
    {
        [Header("Tab config")]
        [SerializeField]
        private List<ShopTabData> tabs = new();

        [Header("View references")]
        [SerializeField]
        private Button btnBack;

        [SerializeField]
        private Transform shopTabContainer;

        [SerializeField]
        private UIShopTabItem shopTabPrefab;

        [SerializeField]
        private UIShopTabItem shopTabHighlight;

        // --- Private Fields ---
        private List<UIShopTabItem> _tabsItem = new();
        private EShopTab? _selectedShopTab;
        private GameObject _selectedLayout;
        private UIShopTabItem _selectedTab;
        private RectTransform _rtShopTabHighlight;
        private Vector2 _orgShopTabHighlightAnchoredPos;

        private Action<string, int> _onBuyProduct;
        private Action<string, int> _onBuyBundleProduct;
        private Action<int, EShopTab> _onClaim;
        private Action<EShopTab> _onChangeTab;

        private void DisableLayouts()
        {
            foreach (var entry in tabs)
            {
                if (entry.go != null)
                {
                    entry.go.SetActive(false);
                }
            }
        }

        public void Initialize()
        {
            _rtShopTabHighlight = shopTabHighlight.transform as RectTransform;

            if (_rtShopTabHighlight != null)
            {
                _orgShopTabHighlightAnchoredPos = _rtShopTabHighlight.anchoredPosition;
            }

            btnBack.onClick.AddListener(OnClickClose);
            DisableLayouts();
            RefreshTab();
        }

        public void Bind(EShopTab defaultTab, Action<string, int> onBuyProduct, Action<string, int> onBuyBundleProduct,
            Action<EShopTab> onChangeTab, Action<int, EShopTab> onClaim)
        {
            _onClaim = onClaim;
            _onBuyProduct = onBuyProduct;
            _onBuyBundleProduct = onBuyBundleProduct;
            _onChangeTab = onChangeTab;

            DisableHighlight();
            ChangeTab(defaultTab);
        }

        public void ChangeTab(EShopTab tab)
        {
            if (_selectedShopTab == tab)
            {
                return;
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].tab == tab)
                {
                    OnClickTab(i);
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            btnBack.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<ShopView>();
        }

        private void ShowHighlight(UIShopTabItem parent)
        {
            shopTabHighlight.BindTitle(parent.Title);
            _rtShopTabHighlight.SetParent(parent.transform);
            _rtShopTabHighlight.SetAsLastSibling();

            // set position
            var newAnchoredPos = _orgShopTabHighlightAnchoredPos;
            newAnchoredPos.y = 0;
            _rtShopTabHighlight.anchoredPosition = newAnchoredPos;

            shopTabHighlight.gameObject.SetActive(true);
        }

        private void DisableHighlight()
        {
            shopTabHighlight.gameObject.SetActive(false);
            _rtShopTabHighlight.SetParent(shopTabContainer);
        }

        private void RefreshTab()
        {
            var tabsDb = DatabaseManager.Instance.GetAllTabs();

            for (int i = 0; i < tabs.Count; i++)
            {
                var cfg = tabsDb.FirstOrDefault(v => v.tabId == (int)tabs[i].tab);

                if (cfg == null)
                {
                    Debug.LogWarning($"[UIShopTab] cfg {tabs[i].tab} not found");
                    continue;
                }

                if (_tabsItem.Count > i)
                {
                    var clone = _tabsItem[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(i, cfg.uiNameVi, OnClickTab);
                }
                else
                {
                    var clone = Instantiate(shopTabPrefab, shopTabContainer);
                    clone.Bind(i, cfg.uiNameVi, OnClickTab);
                    _tabsItem.Add(clone);
                }
            }
        }

        private void OnClickTab(int tabIdx)
        {
            if (_selectedLayout != null)
            {
                _selectedLayout.gameObject.SetActive(false);
            }

            var selected = tabs[tabIdx];

            _selectedLayout = selected.go;
            _selectedShopTab = selected.tab;
            _selectedLayout.SetActive(true);

            // refresh giao diện layout
            RefreshLayout();

            if (_selectedTab != null)
            {
                _selectedTab.SetSelected(false);
            }

            _selectedTab = _tabsItem[tabIdx];
            _selectedTab.SetSelected(true);

            // set highlight cho tab đã chọn
            ShowHighlight(_selectedTab);

            if (_selectedShopTab != null)
            {
                _onChangeTab?.Invoke(_selectedShopTab.Value);
            }
        }

        private void RefreshLayout()
        {
            if (_selectedShopTab == null)
            {
                return;
            }

            switch (_selectedShopTab)
            {
                case EShopTab.Topup:
                {
                    var packs = DatabaseManager.Instance.GetShopPacksTopup();
                    _selectedLayout.GetComponent<UIShopTopupLayout>().Bind(packs, _onBuyProduct);
                    break;
                }

                case EShopTab.MonthlyPass:
                {
                    // pack_iap subscription rows — cùng RPC iap/pack_purchase với Special, khác
                    // iap/purchase (pack_diamond) chỉ biết cộng 1 loại tiền.
                    var packs = DatabaseManager.Instance.GetShopPacksSpecial();
                    _selectedLayout.GetComponent<UIShopMonthlyLayout>().Bind(packs, _onBuyBundleProduct, _onClaim);
                    break;
                }

                case EShopTab.GloryPass:
                {
                    var packs = DatabaseManager.Instance.GetShopPacksGloryPass();
                    _selectedLayout.GetComponent<UIShopGloryPassLayout>().Bind(packs, _onChangeTab, _onClaim);
                    break;
                }

                case EShopTab.Special:
                {
                    var packs = DatabaseManager.Instance.GetShopPacksSpecial();
                    _selectedLayout.GetComponent<UIShopSpecialLayout>().Bind(packs, _onBuyBundleProduct);
                    break;
                }

                case null:
                default:
                    break;
            }
        }
    }
}