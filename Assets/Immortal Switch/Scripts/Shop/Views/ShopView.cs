using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
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
        Topup = 4,

        /// <summary>
        /// goi thang
        /// </summary>
        MonthlyPass = 2,

        /// <summary>
        /// goi danh vong
        /// </summary>
        GloryPass = 3,

        /// <summary>
        /// goi dac biet
        /// </summary>
        Special = 1,

        /// <summary>
        /// goi event
        /// </summary>
        Event = 5,
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

            // IAP có thể chưa init xong (thiết bị không có Google Play/App Store, timeout...) —
            // không cho mở Shop trong trường hợp đó thay vì để người chơi bấm mua rồi mới báo lỗi.
            if (!IAPManager.Instance.IsAvailable)
            {
                UIManager.Instance.ShowToast("Cửa hàng hiện không khả dụng trên thiết bị này.");
                UIManager.Instance.Close<ShopView>();
                return;
            }

            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);

            tabVertical.Initialize();
            tabHorizontal.Initialize();

            var tab = args is ShopArgs data ? data.DefaultTab : defaultTab;
            tabVertical.Bind(tab, OnClickBuyProduct, OnClickBuyBundleProduct, OnChangeTab, OnClickClaim);
            tabHorizontal.Bind(tab, OnClickBuyProduct, OnClickBuyBundleProduct, OnChangeTab, OnClickClaim);

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
            switch (shopTab)
            {
                case EShopTab.GloryPass:
                    ClaimGloryPass(shopPackId);
                    break;

                case EShopTab.MonthlyPass:
                    ClaimMonthlyPass(shopPackId);
                    break;
            }
        }

        private void ClaimGloryPass(int shopPackId)
        {
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

        private void ClaimMonthlyPass(int packId)
        {
            if (!ShopManager.Instance.IsMonthlyPassPurchased(packId))
            {
                return;
            }

            var currentDay = ShopManager.Instance.GetMonthlyPassCurrentDay(packId);

            if (currentDay <= 0 ||
                ShopManager.Instance.IsMonthlyPassDayClaimed(packId, currentDay))
            {
                return;
            }

            var rewards = DatabaseManager.Instance.GetPackMonthly(packId, currentDay);

            // TODO: show popup reward

            ShopManager.Instance.ClaimMonthlyPassDay(packId, currentDay);
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
                else if (packId is PackIdConstants.ID_MONTHLY_NORMAL or PackIdConstants.ID_MONTHLY_PREMIUM)
                {
                    ShopManager.Instance.PurchaseMonthlyPass(packId);
                }
                else
                {
                    // gói kim cương (topup) — server đã cộng điểm tích nạp trong iap/purchase,
                    // sync lại recharge/state để GloryPass phản ánh đúng ngay
                    ShopManager.Instance.SyncRechargeStateAsync().Forget();
                }
            });
        }

        /// <summary>Mua gói Special (pack_iap, nhiều item/gói) — khác OnClickBuyProduct vì phải đi
        /// qua RPC iap/pack_purchase (BuyPackProduct) để server cộng đủ cả 3 slot item thay vì chỉ 1.</summary>
        private void OnClickBuyBundleProduct(string storeProductId, int packId)
        {
            IAPManager.Instance.BuyPackProduct(packId, storeProductId, (success, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[ShopView] Bundle purchase failed -> product={storeProductId}, error={error}");
                    return;
                }

                ShopManager.Instance.RecordPurchase(packId);
                ShopManager.Instance.SyncRechargeStateAsync().Forget();
            });
        }
    }
}