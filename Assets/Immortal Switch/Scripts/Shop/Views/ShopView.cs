using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.Views;
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

            tabVertical.Bind(OnClickBuyProduct, OnClickBuyBundleProduct, OnClickBuyEventProduct, OnChangeTab, OnClickClaim);
            tabHorizontal.Bind(OnClickBuyProduct, OnClickBuyBundleProduct, OnClickBuyEventProduct, OnChangeTab, OnClickClaim);

            var tab = args is ShopArgs data ? data.DefaultTab : defaultTab;
            OnChangeTab(tab);

            // Re-sync điểm/milestone GloryPass và ngày/claimed Monthly Pass mỗi lần mở Shop — tránh
            // lệch nếu tích nạp/qua ngày mới ở phiên khác.
            ShopManager.Instance.SyncRechargeStateAsync().Forget();
            ShopManager.Instance.SyncMonthlyPassStateAsync().Forget();
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

            ClaimMonthlyPassAsync(packId).Forget();
        }

        /// <summary>Gọi monthlypass/claim — server tự suy ngày từ purchased_at (nguồn sự thật, có
        /// thể lệch với bộ đếm cache dùng để hiện nút) rồi cộng thưởng thật; sau khi thành công,
        /// sync lại monthlypass/state để current_day/claimed trong ShopManager luôn khớp server.</summary>
        private async UniTaskVoid ClaimMonthlyPassAsync(int packId)
        {
            try
            {
                var response = await NakamaClient.Instance.MonthlyPassClaimAsync(packId);

                await ShopManager.Instance.SyncMonthlyPassStateAsync();
                CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
                PopupRewardService.Show(DatabaseManager.Instance.GetPackMonthly(packId, response.Day));

                Debug.Log($"[ShopView] MonthlyPass claimed: pack={packId} day={response.Day}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopView] Claim MonthlyPass thất bại -> pack={packId}: {ex.Message}");
            }
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

                // Callback này chỉ dùng cho pack_diamond. Sau khi server xác thực thành công,
                // đánh dấu riêng từng pack đã dùng ưu đãi x2 lần đầu rồi refresh Topup UI.
                ShopManager.Instance.RecordDiamondFirstPurchase(packId);
                ShopManager.Instance.SyncRechargeStateAsync().Forget();
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

                if (packId is PackIdConstants.ID_MONTHLY_NORMAL or PackIdConstants.ID_MONTHLY_PREMIUM)
                {
                    // gói Monthly Pass (pack_iap subscription) — server vừa cộng thưởng tức thời +
                    // (re)bắt đầu chu kỳ 30 ngày, sync lại monthlypass/state để current_day/claimed
                    // trong ShopManager phản ánh đúng ngay.
                    ShopManager.Instance.SyncMonthlyPassStateAsync().Forget();
                }
            });
        }

        /// <summary>
        /// Mua gói Event: gói miễn phí nhận ngay, gói trả phí đi qua IAP và đều cập nhật limit riêng của Event.
        /// </summary>
        private void OnClickBuyEventProduct(string storeProductId, int packId)
        {
            var runtime = DatabaseManager.Instance.GetShopPacksEvent()
                .Find(entry => entry.Pack.iD == packId);

            if (runtime?.Pack == null ||
                runtime.Product == null)
            {
                Debug.LogWarning($"[ShopView] Event pack not found: {packId}");
                return;
            }

            if (ShopManager.Instance.GetEventRemaining(packId, runtime.Pack.limit) <= 0)
            {
                UIManager.Instance.ShowToast("Đã đạt giới hạn mua của gói này.");
                return;
            }

            if (Mathf.Approximately(runtime.Product.price, 0f))
            {
                if (ShopManager.Instance.TryRecordEventPurchase(packId, runtime.Pack.limit))
                {
                    PopupRewardService.Show(runtime.Rewards);
                }

                return;
            }

            IAPManager.Instance.BuyEventPackProduct(
                packId,
                storeProductId,
                runtime.Rewards,
                (success, error) =>
                {
                    if (!success)
                    {
                        Debug.LogWarning($"[ShopView] Event purchase failed -> product={storeProductId}, error={error}");
                        return;
                    }

                    ShopManager.Instance.TryRecordEventPurchase(packId, runtime.Pack.limit);
                    ShopManager.Instance.SyncRechargeStateAsync().Forget();
                }
            );
        }
    }
}