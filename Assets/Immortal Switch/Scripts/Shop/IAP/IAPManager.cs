using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;
using UnityEngine.Purchasing;

#pragma warning disable CS0618 // Unity IAP classic (IStoreListener/UnityPurchasing) API — marked
// Obsolete từ IAP 5.x để hướng migrate sang UnityIAPServices, nhưng vẫn được support đầy đủ.

namespace Immortal_Switch.Scripts.Shop.IAP
{
    /// <summary>
    /// Khởi tạo Unity IAP, đăng ký catalog product từ product_id (DatabaseManager), thực hiện mua
    /// hàng và gửi receipt lên Nakama để verify. KHÔNG tự cộng thưởng — server có hook riêng cộng
    /// thưởng sau khi validate thành công; client chỉ confirm pending purchase khi response hợp lệ.
    /// </summary>
    public class IAPManager : Singleton<IAPManager>, IDetailedStoreListener
    {
        [Serializable]
        private class UnifiedReceipt
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        private const int InitTimeoutSeconds = 15;

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        private UniTaskCompletionSource<bool> _initTcs;

        private readonly Dictionary<string, Action<bool, string>> _pendingCallbacks = new();
        // pack_id (từ config pack_diamond) — cần gửi kèm receipt lên RPC iap/purchase để server biết
        // cộng thưởng theo pack nào; Unity IAP chỉ biết storeProductId (google/apple product id).
        private readonly Dictionary<string, int> _pendingPackIds = new();

        /// <summary>True khi IAP đã init xong và store sẵn sàng nhận mua hàng. UI (shop) phải kiểm
        /// tra cờ này trước khi cho mở màn mua hàng — false khi thiết bị không có store khả dụng
        /// (không có Google Play/App Store, sandbox lỗi...) hoặc khi init bị timeout.</summary>
        public bool IsAvailable => _storeController != null;

        protected override void OnSingletonAwake()
        {
            // Instance được tạo động (không có prefab đặt sẵn trong scene để tick
            // DontDestroyOnLoadEnabled), nên phải tự gọi thẳng ở đây — nếu không, 1 lần
            // SceneManager.LoadScene bất kỳ sau khi IAPManager đã init xong sẽ destroy GameObject
            // này, và lần gọi IAPManager.Instance kế tiếp sẽ âm thầm tạo instance mới chưa từng
            // InitializeAsync() (mua hàng sẽ luôn báo "IAP chưa khởi tạo xong" mà không có log lỗi).
            DontDestroyOnLoad(gameObject);
        }

        public override async UniTask InitializeAsync()
        {
            if (_storeController != null)
                return;

            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                var products = DatabaseManager.Instance.GetAllProducts();

                foreach (var product in products)
                {
                    string storeProductId = GetStoreProductId(product);

                    if (string.IsNullOrEmpty(storeProductId))
                        continue;

                    ProductType type = product.subscribe == 1 ? ProductType.Subscription : ProductType.Consumable;
                    builder.AddProduct(storeProductId, type);
                }

                _initTcs = new UniTaskCompletionSource<bool>();
                UnityPurchasing.Initialize(this, builder);

                // Một số thiết bị (không có Google Play/App Store, sandbox lỗi...) không bao giờ gọi
                // OnInitialized lẫn OnInitializeFailed — nếu await thẳng _initTcs.Task, bootstrap sẽ
                // treo vĩnh viễn ở đây. Timeout để đảm bảo flow game luôn tiếp tục được.
                (bool hasResult, bool success) = await UniTask.WhenAny(
                    _initTcs.Task, UniTask.Delay(TimeSpan.FromSeconds(InitTimeoutSeconds)));

                if (!hasResult)
                    Debug.LogWarning($"[IAPManager] Initialize timeout sau {InitTimeoutSeconds}s — store không phản hồi, tiếp tục flow game không có IAP.");
                else if (!success)
                    Debug.LogWarning("[IAPManager] Initialize failed — tiếp tục flow game không có IAP.");
            }
            catch (Exception ex)
            {
                // Không để lỗi khởi tạo IAP (thiết bị không hỗ trợ store, exception từ plugin...) chặn
                // luôn bootstrap — shop sẽ chỉ báo "không khả dụng" thay vì crash toàn bộ game.
                Debug.LogWarning($"[IAPManager] Initialize exception: {ex.Message}");
            }
        }

        private static string GetStoreProductId(DynamicHeroesGlobalSpecificationsProductIdRow product)
        {
#if UNITY_IOS
            return product.appleID;
#elif UNITY_ANDROID
            return product.googleID;
#else
            return product.googleID;
#endif
        }

        /// <summary>Bắt đầu mua 1 product theo pack_id (config pack_diamond) + store product id
        /// (đã resolve theo platform — xem UIShopProductDiamond.OnClickBuy). onComplete báo
        /// (success, errorMessage).</summary>
        public void BuyProduct(int packId, string storeProductId, Action<bool, string> onComplete)
        {
            if (_storeController == null)
            {
                onComplete?.Invoke(false, "IAP chưa khởi tạo xong.");
                return;
            }

            Product product = _storeController.products.WithID(storeProductId);

            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(false, $"Product không khả dụng: {storeProductId}");
                return;
            }

            _pendingCallbacks[storeProductId] = onComplete;
            _pendingPackIds[storeProductId] = packId;
            _storeController.InitiatePurchase(product);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            _initTcs?.TrySetResult(true);
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[IAPManager] Initialize failed: {error} {message}");
            _initTcs?.TrySetResult(false);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // Trả Pending — chỉ ConfirmPendingPurchase sau khi Nakama validate thành công, để không
            // mất giao dịch nếu mạng rớt giữa chừng (Unity IAP sẽ tự redeliver purchase pending này).
            ValidateAndConfirmAsync(args.purchasedProduct).Forget();
            return PurchaseProcessingResult.Pending;
        }

        private async UniTaskVoid ValidateAndConfirmAsync(Product product)
        {
            string storeProductId = product.definition.id;

            _pendingCallbacks.TryGetValue(storeProductId, out Action<bool, string> callback);
            _pendingCallbacks.Remove(storeProductId);

            _pendingPackIds.TryGetValue(storeProductId, out int packId);
            _pendingPackIds.Remove(storeProductId);

            string payload = ExtractReceiptPayload(product.receipt);

            if (string.IsNullOrEmpty(payload))
            {
                Debug.LogError($"[IAPManager] Receipt rỗng cho product={storeProductId}, không thể validate.");
                callback?.Invoke(false, "Receipt rỗng, không thể validate.");
                return;
            }

#if UNITY_IOS
            const string store = "apple";
#else
            const string store = "google";
#endif

            try
            {
                // RPC iap/purchase: server tự verify receipt với store rồi cộng thưởng theo pack_id
                // (BagLib.add) — không dùng NakamaClient.ValidatePurchaseApple/GoogleAsync (API
                // built-in của Nakama) vì nó chỉ verify + chống replay, không cộng được gì cả.
                var response = await NakamaClient.Instance.IapPurchaseAsync(packId, store, payload);

                _storeController.ConfirmPendingPurchase(product);
                await RefreshCurrencyAsync();

                Debug.Log($"[IAPManager] Purchase validated & confirmed -> product={storeProductId} pack={packId} gems={response.GemsGranted}");
                callback?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                // Không confirm khi validate lỗi (mạng/timeout/receipt sai) — giữ nguyên pending để
                // Unity IAP redeliver, người chơi có thể bấm mua lại hoặc app tự retry lúc khởi động.
                Debug.LogError($"[IAPManager] Validate purchase thất bại -> product={storeProductId}: {ex.Message}");
                callback?.Invoke(false, ex.Message);
            }
        }

        private static string ExtractReceiptPayload(string rawReceipt)
        {
            if (string.IsNullOrEmpty(rawReceipt))
                return null;

            try
            {
                var unified = JsonUtility.FromJson<UnifiedReceipt>(rawReceipt);
                return unified?.Payload;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPManager] Không parse được unified receipt: {ex.Message}");
                return null;
            }
        }

        private static async UniTask RefreshCurrencyAsync()
        {
            try
            {
                var player = await NakamaClient.Instance.GetPlayerMeAsync();

                if (player == null)
                    return;

                CurrencyManager.Instance.Set(CurrencyType.gold, player.coins);
                CurrencyManager.Instance.Set(CurrencyType.diamond, player.gems);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IAPManager] Refresh currency sau khi mua thất bại: {ex.Message}");
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[IAPManager] Purchase failed (legacy callback) -> product={product?.definition.id}, reason={failureReason}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            string storeProductId = product?.definition.id;

            if (storeProductId != null && _pendingCallbacks.TryGetValue(storeProductId, out Action<bool, string> callback))
            {
                _pendingCallbacks.Remove(storeProductId);
                _pendingPackIds.Remove(storeProductId);
                callback?.Invoke(false, failureDescription.message);
            }

            Debug.LogWarning(
                $"[IAPManager] Purchase failed -> product={storeProductId}, reason={failureDescription.reason}, message={failureDescription.message}");
        }
    }
}

#pragma warning restore CS0618
