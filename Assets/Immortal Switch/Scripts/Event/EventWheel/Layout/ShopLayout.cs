using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventWheel.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel.Layout
{
    public class ShopLayout : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        [SerializeField]
        private RectTransform itemContainer;

        [SerializeField]
        private UIEventShopItem itemPrefab;

        // --- Private Fields ---
        private List<DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow> _rows = new();
        private SimpleUIPool<UIEventShopItem> _pools;
        private EventWheelPassManager _passManager;
        private bool _isSubscribed;

        private void Awake()
        {
            _pools = new SimpleUIPool<UIEventShopItem>(itemPrefab, itemContainer);
        }

        private void OnEnable()
        {
            SubscribeDataChanged();
        }

        private void OnDisable()
        {
            UnsubscribeDataChanged();
        }

        public void Bind(List<DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow> rows)
        {
            _rows = new List<DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow>(rows);

            SubscribeDataChanged();
            RefreshItems();
        }

        /// <summary>Cập nhật lại toàn bộ item shop bằng dữ liệu lượt mua và số dư point mới nhất.</summary>
        private void RefreshItems()
        {
            var pointBalance = _passManager.State?.Progress?.PointBalance ?? 0;

            txtQuantity.text = $"{pointBalance:N0}";

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var itemDisplay = DatabaseManager.Instance.GetDisplayData(row.itemId);
                var item = DatabaseManager.Instance.ItemDb.FindItem(row.itemId);
                var clone = _pools.Get(i);
                var purchased = _passManager.GetShopPurchasedCount(row);

                clone.Bind(
                    row.itemId, row.shopSlotId,
                    item?.itemName, string.Empty,
                    (EEventWheelShopLimitType)row.limitType,
                    purchased, row.limitValue,
                    row.pricePoint, row.amount,
                    itemDisplay?.ItemIcon,
                    pointBalance >= row.pricePoint,
                    OnClickBuy
                );
            }

            _pools.ReleaseFrom(_rows.Count);
        }

        /// <summary>Gọi eventwheel/shop_buy và chỉ trao thưởng khi server xác nhận mua thành công.</summary>
        private void OnClickBuy(int shopSlotId)
        {
            var item = _rows.Find(row => row.shopSlotId == shopSlotId);

            if (item == null)
            {
                Debug.LogError($"[ShopLayout] Không tìm thấy shopSlotId: {shopSlotId}");
                return;
            }

            BuyAsync(shopSlotId).Forget();
        }

        private async UniTaskVoid BuyAsync(int shopSlotId)
        {
            var (success, rewards, error) = await _passManager.ShopBuyAsync(shopSlotId);

            if (!success)
            {
                Debug.LogWarning($"[ShopLayout] eventwheel/shop_buy shopSlotId={shopSlotId} failed: {error}");
                UIManager.Instance.ShowToast(DescribeShopBuyError(error));
                return;
            }

            UIManager.Instance
                .OpenPopupAsync<PopupRewardView>(new PopupRewardArgs
                {
                    Rewards = rewards,
                })
                .Forget();
        }

        private static string DescribeShopBuyError(string error)
        {
            switch (error)
            {
                case "EVENT_NOT_ACTIVE":   return "Cửa hàng sự kiện đã đóng.";
                case "LIMIT_REACHED":      return "Đã đạt giới hạn mua.";
                case "INSUFFICIENT_POINT": return "Không đủ điểm để mua.";
                default:                   return "Mua thất bại, vui lòng thử lại.";
            }
        }

        /// <summary>Đăng ký cập nhật UI ngay khi dữ liệu Event Wheel Pass thay đổi.</summary>
        private void SubscribeDataChanged()
        {
            if (_isSubscribed)
            {
                return;
            }

            _passManager = EventWheelPassManager.Instance;
            _passManager.OnDataChanged += RefreshItems;
            _isSubscribed = true;
        }

        /// <summary>Hủy đăng ký cập nhật khi layout không còn hoạt động.</summary>
        private void UnsubscribeDataChanged()
        {
            if (!_isSubscribed ||
                _passManager == null)
            {
                return;
            }

            _passManager.OnDataChanged -= RefreshItems;
            _isSubscribed = false;
        }
    }
}