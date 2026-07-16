using System;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using RecyclableScrollRect;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.UI
{
    public class UIEventWheelPassItem : BaseItem
    {
        [Header("Normal references")]
        [SerializeField]
        private UIItemSlot normalSlot;

        [SerializeField]
        private TextMeshProUGUI normalQuantity;

        [SerializeField]
        private GameObject goNormalClaimedStatus;

        [Header("Premium references")]
        [SerializeField]
        private UIItemSlot premiumSlot;

        [SerializeField]
        private TextMeshProUGUI premiumQuantity;

        [SerializeField]
        private GameObject goPremiumClaimedStatus;

        [SerializeField]
        private GameObject goPremiumLockStatus;

        [Header("View references")]
        [SerializeField]
        private TextMeshProUGUI txtTarget;

        [SerializeField]
        private Button btn;

        // --- Private Fields ---
        /// <summary>
        /// claim reward
        /// 1: milestone id reward
        /// </summary>
        private Action<int> _onClickClaim;

        private EventWheelPassManager _manager;
        private DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow _row;
        private int _eventId;
        private bool _isSubscribed;

        private void Awake()
        {
            btn.onClick.AddListener(OnClickClaim);
        }

        private void OnEnable()
        {
            SubscribeDataChanged();
        }

        private void OnDisable()
        {
            UnsubscribeDataChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeDataChanged();
            btn.onClick.RemoveListener(OnClickClaim);
        }

        private void OnClickClaim()
        {
            if (_row != null)
            {
                _onClickClaim?.Invoke(_row.milestoneId);
            }
        }

        public void Bind(
            int eventId,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row,
            ItemData normalItem,
            ItemData premiumItem,
            Action<int> onClickClaim
        )
        {
            _eventId = eventId;
            _row = row;
            _onClickClaim = onClickClaim;

            BindReward(normalSlot, normalQuantity, normalItem);
            BindReward(premiumSlot, premiumQuantity, premiumItem);
            SubscribeDataChanged();
            RefreshState();
        }

        /// <summary>Cập nhật trạng thái claim, Premium và nút bấm bằng dữ liệu mới nhất.</summary>
        private void RefreshState()
        {
            if (_row == null ||
                _manager == null)
            {
                return;
            }

            txtTarget.text = $"{_row.spinRequired:N0}";
            goNormalClaimedStatus.SetActive(_manager.IsFreeClaimed(_eventId, _row.milestoneId));
            goPremiumClaimedStatus.SetActive(_manager.IsPremiumClaimed(_eventId, _row.milestoneId));
            goPremiumLockStatus.SetActive(!_manager.IsPremiumPurchased(_eventId));
            btn.interactable = _manager.CanClaim(_eventId, _row);
        }

        /// <summary>Lắng nghe thay đổi dữ liệu khi item đang được hiển thị.</summary>
        private void SubscribeDataChanged()
        {
            if (_isSubscribed)
            {
                return;
            }

            _manager = EventWheelPassManager.Instance;
            _manager.OnDataChanged += RefreshState;
            _isSubscribed = true;
        }

        /// <summary>Ngừng lắng nghe khi item bị ẩn hoặc được đưa về pool recycle.</summary>
        private void UnsubscribeDataChanged()
        {
            if (!_isSubscribed ||
                _manager == null)
            {
                return;
            }

            _manager.OnDataChanged -= RefreshState;
            _isSubscribed = false;
        }

        private void BindReward(UIItemSlot itemSlot, TextMeshProUGUI txtQuantity, ItemData item)
        {
            if (item == null)
            {
                txtQuantity.SetText("0");
                return;
            }

            var itemDisplay = DatabaseManager.Instance.GetDisplayData(item.ItemId);

            if (itemDisplay != null)
            {
                itemSlot.Bind(
                    itemDisplay.ItemIcon,
                    itemDisplay.TierInfo.border,
                    itemDisplay.TierInfo.background,
                    itemDisplay.TierInfo.tierIcon
                );
            }

            txtQuantity.SetText(item.Quantity.ToInputString());
        }
    }
}