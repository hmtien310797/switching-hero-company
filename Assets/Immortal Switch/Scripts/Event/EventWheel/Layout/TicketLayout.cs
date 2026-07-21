using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.Layout
{
    public class TicketLayout : MonoBehaviour
    {
        [SerializeField]
        private GameObject goBonusRewardNotPurchased;

        [SerializeField]
        private GameObject goBonusRewardPurchased;

        [SerializeField]
        private GameObject goBtnBuyNotPurchased;

        [SerializeField]
        private GameObject goBtnBuyPurchased;

        [SerializeField]
        private TextMeshProUGUI txtBuyPrice;

        [SerializeField]
        private Button btnBuy;

        [SerializeField]
        private Button btnClaimAll;

        [SerializeField]
        private PassRecyclableView passRecyclableView;

        // --- Private Fields ---
        private List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> _rows = new();
        private EventWheelPassManager _manager;

        private int _eventId;
        private bool _isBound;
        private bool _isSubscribed;
        private bool _isBuying;

        private void Awake()
        {
            btnClaimAll.onClick.AddListener(OnClickClaimAll);
            btnBuy.onClick.AddListener(OnClickBuyProduct);
        }

        private void OnEnable()
        {
            SubscribeDataChanged();

            if (_isBound)
            {
                RefreshStatus();
            }
        }

        private void OnDisable()
        {
            UnsubscribeDataChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeDataChanged();
            btnClaimAll.onClick.RemoveListener(OnClickClaimAll);
            btnBuy.onClick.RemoveListener(OnClickBuyProduct);
        }

        private void OnClickBuyProduct()
        {
            if (_isBuying) return;
            BuyPremiumAsync().Forget();
        }

        private async UniTaskVoid BuyPremiumAsync()
        {
            _isBuying = true;
            btnBuy.interactable = false;

            try
            {
                var (success, error) = await _manager.BuyPremiumAsync();

                if (!success)
                {
                    UIManager.Instance.ShowToast(DescribeBuyPremiumError(error));
                    return;
                }

                UIManager.Instance.ShowToast("Mua Premium Pass thành công!");
            }
            finally
            {
                _isBuying = false;
                RefreshStatus();
            }
        }

        private static string DescribeBuyPremiumError(string error)
        {
            switch (error)
            {
                case "EVENT_NOT_ACTIVE":       return "Sự kiện không còn hoạt động.";
                case "ALREADY_PURCHASED":      return "Bạn đã mua Premium Pass rồi.";
                case "PASS_NOT_CONFIGURED":
                case "PRODUCT_NOT_CONFIGURED": return "Gói chưa mở bán trên thiết bị này.";
                default:                       return string.IsNullOrEmpty(error) ? "Mua thất bại, vui lòng thử lại." : error;
            }
        }

        private void OnClickClaimAll()
        {
            ClaimAllAsync().Forget();
        }

        private async UniTaskVoid ClaimAllAsync()
        {
            var (rewards, error) = await _manager.ClaimAllAsync(_rows);

            if (rewards.Count == 0 && error != null)
            {
                UIManager.Instance.ShowToast(DescribeClaimError(error));
                return;
            }

            ShowRewards(rewards);
        }

        private static string DescribeClaimError(string error)
        {
            switch (error)
            {
                case "EVENT_NOT_ACTIVE": return "Sự kiện không còn hoạt động.";
                default:                 return "Nhận thưởng thất bại, vui lòng thử lại.";
            }
        }

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows,
            int eventId,
            string price
        )
        {
            _rows = rows ?? new List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow>();
            _eventId = eventId;
            _manager = EventWheelPassManager.Instance;
            _isBound = true;
            txtBuyPrice.text = price;

            SubscribeDataChanged();
            RefreshStatus();
            passRecyclableView.Bind(rows, _eventId, OnResolveItem);
        }

        /// <summary>Cập nhật các trạng thái Premium và nút Claim All bằng dữ liệu mới nhất.</summary>
        private void RefreshStatus()
        {
            if (!_isBound ||
                _manager == null)
            {
                return;
            }

            var isPremiumPurchased = _manager.IsPremiumPurchased(_eventId);

            btnClaimAll.interactable = _manager.HasClaimable(_eventId, _rows);
            btnBuy.interactable = !isPremiumPurchased;

            goBonusRewardNotPurchased.SetActive(!isPremiumPurchased);
            goBonusRewardPurchased.SetActive(isPremiumPurchased);
            goBtnBuyNotPurchased.SetActive(!isPremiumPurchased);
            goBtnBuyPurchased.SetActive(isPremiumPurchased);
        }

        /// <summary>Lắng nghe thay đổi dữ liệu để cập nhật trạng thái Premium ngay sau khi mua.</summary>
        private void SubscribeDataChanged()
        {
            if (_isSubscribed)
            {
                return;
            }

            _manager = EventWheelPassManager.Instance;
            _manager.OnDataChanged += RefreshStatus;
            _isSubscribed = true;
        }

        /// <summary>Ngừng lắng nghe thay đổi dữ liệu khi layout không còn hiển thị.</summary>
        private void UnsubscribeDataChanged()
        {
            if (!_isSubscribed ||
                _manager == null)
            {
                return;
            }

            _manager.OnDataChanged -= RefreshStatus;
            _isSubscribed = false;
        }

        private static void ShowRewards(List<ItemData> rewards)
        {
            if (rewards == null ||
                rewards.Count == 0)
            {
                return;
            }

            UIManager.Instance
                .OpenPopupAsync<PopupRewardView>(new PopupRewardArgs
                {
                    Rewards = rewards,
                })
                .Forget();
        }

        private DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow OnResolveItem(int itemIdx)
        {
            if (itemIdx < 0 ||
                itemIdx >= _rows.Count)
            {
                return null;
            }

            var item = _rows[itemIdx];
            return item;
        }
    }
}