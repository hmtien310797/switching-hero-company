using System;
using Immortal_Switch.Scripts.Shop.IAP;
using Immortal_Switch.Scripts.Shop.Views.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Views
{
    public enum EShopTab
    {
        /// <summary>
        /// shop kim cuong
        /// </summary>
        Topup = 0,

        /// <summary>
        /// goi thang
        /// </summary>
        MonthlyPass = 1,

        /// <summary>
        /// goi danh vong
        /// </summary>
        GloryPass = 2,

        /// <summary>
        /// goi dac biet
        /// </summary>
        Special = 3,
    }

    [Serializable]
    public class ShopTabData
    {
        public EShopTab tab;
        public GameObject go;
    }

    public class ShopArgs
    {
        public EShopTab DefaultTab;

        public ShopArgs(EShopTab defaultTab)
        {
            DefaultTab = defaultTab;
        }
    }

    public class ShopView : AnimatedUIView
    {
        [Header("Tab config")]
        [SerializeField]
        private EShopTab defaultTab;

        [Header("Orientation references")]
        [SerializeField]
        private GameObject goVertical;

        [SerializeField]
        private GameObject goHorizontal;

        [Header("View references")]
        [SerializeField]
        private UIShopTabController tabVertical;

        [SerializeField]
        private UIShopTabController tabHorizontal;

        // --- Private Fields ---
        private GameObject _selectedLayout;
        private UIShopTabItem _selectedTab;

        private void Awake()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
        }

        private void OnDestroy()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
        }

        private void OnOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
        {
            switch (obj)
            {
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    goVertical.SetActive(true);
                    goHorizontal.SetActive(false);
                    break;

                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    goVertical.SetActive(false);
                    goHorizontal.SetActive(true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);

            tabVertical.Initialize();
            tabHorizontal.Initialize();

            var tab = args is ShopArgs data ? data.DefaultTab : defaultTab;
            tabVertical.Bind(tab, OnClickBuyProduct, OnChangeTab);
            tabHorizontal.Bind(tab, OnClickBuyProduct, OnChangeTab);
        }

        private void OnChangeTab(EShopTab tab)
        {
            tabVertical.ChangeTab(tab);
            tabHorizontal.ChangeTab(tab);
        }

        private void OnClickBuyProduct(string storeProductId, int packId)
        {
            IAPManager.Instance.BuyProduct(packId, storeProductId, (success, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[ShopView] Purchase failed -> product={storeProductId}, error={error}");
                    return;
                }

                if (packId > 0)
                {
                    ShopManager.Instance.RecordPurchase(packId);
                }
            });
        }
    }
}