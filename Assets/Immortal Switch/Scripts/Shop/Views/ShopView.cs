using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Shared;
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
            tabVertical.Bind(tab, OnClickBuyProduct, OnChangeTab, OnClickClaim);
            tabHorizontal.Bind(tab, OnClickBuyProduct, OnChangeTab, OnClickClaim);

            // Re-sync điểm/milestone GloryPass mỗi lần mở Shop — tránh lệch nếu tích nạp ở phiên khác.
            ShopManager.Instance.SyncRechargeStateAsync().Forget();
        }

        private void OnChangeTab(EShopTab tab)
        {
            tabVertical.ChangeTab(tab);
            tabHorizontal.ChangeTab(tab);
        }

        /// <summary>
        /// item id nhan qua
        /// </summary>
        private void OnClickClaim(int shopPackId, EShopTab shopTab)
        {
            if (shopTab != EShopTab.GloryPass)
            {
                return;
            }

            var pack = DatabaseManager.Instance.GetShopPacksGloryPass()
                .Find(v => v.iD == shopPackId);

            if (pack == null)
            {
                Debug.LogWarning($"[ShopView] GloryPass milestone not found: {shopPackId}");
                return;
            }

            if (ShopManager.Instance.IsGloryPassClaimed(shopPackId) ||
                ShopManager.Instance.GetTopupCount() < pack.requirePoints)
            {
                return;
            }

            ClaimGloryPassAsync(shopPackId).Forget();
        }

        /// <summary>Gọi recharge/claim — server tự kiểm tra lại số lượt tích nạp trong tháng hiện tại
        /// (nguồn sự thật, có thể lệch với bộ đếm cache dùng để hiện nút) rồi cộng thưởng thật; sau khi
        /// thành công, sync lại recharge/state để points/claimed trong ShopManager luôn khớp server.</summary>
        private async UniTaskVoid ClaimGloryPassAsync(int milestoneId)
        {
            try
            {
                var response = await NakamaClient.Instance.RechargeClaimAsync(milestoneId);

                await ShopManager.Instance.SyncRechargeStateAsync();
                CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

                Debug.Log($"[ShopView] GloryPass claimed: {milestoneId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopView] Claim GloryPass thất bại -> milestone={milestoneId}: {ex.Message}");
            }
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
                    // gói IAP special (có limit)
                    ShopManager.Instance.RecordPurchase(packId);
                }
                else
                {
                    // gói kim cương (topup) — server đã cộng điểm tích nạp trong iap/purchase,
                    // sync lại recharge/state để GloryPass phản ánh đúng ngay
                    ShopManager.Instance.SyncRechargeStateAsync().Forget();
                }
            });
        }
    }
}