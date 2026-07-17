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

        /// <summary>Cập nhật lại toàn bộ item shop bằng dữ liệu lượt mua mới nhất.</summary>
        private void RefreshItems()
        {
            // TODO: Thay bằng số lượng currency thật trong inventory.
            var itemCount = 100;

            txtQuantity.text = $"{itemCount:N0}";

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
                    itemCount >= row.pricePoint,
                    OnClickBuy
                );
            }

            _pools.ReleaseFrom(_rows.Count);
        }

        /// <summary>Xử lý mua item theo mã slot và chỉ trao thưởng khi ghi nhận mua thành công.</summary>
        private void OnClickBuy(int shopSlotId)
        {
            var item = _rows.Find(row => row.shopSlotId == shopSlotId);

            if (item == null)
            {
                Debug.LogError($"[ShopLayout] Không tìm thấy shopSlotId: {shopSlotId}");
                return;
            }

            if (!_passManager.RecordShopPurchase(item))
            {
                Debug.LogWarning($"[ShopLayout] Shop slot {shopSlotId} đã đạt giới hạn mua.");
                return;
            }

            var rewards = new List<ItemData>
            {
                new ItemData(item.itemId, item.amount),
            };

            UIManager.Instance
                .OpenPopupAsync<PopupRewardView>(new PopupRewardArgs
                {
                    Rewards = rewards,
                })
                .Forget();
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