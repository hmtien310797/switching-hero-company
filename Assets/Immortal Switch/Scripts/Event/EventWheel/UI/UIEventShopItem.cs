using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.UI
{
    public class UIEventShopItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtSubtitle;

        [SerializeField]
        private TextMeshProUGUI txtPrice;

        [SerializeField]
        private TextMeshProUGUI txtLimitType;

        [SerializeField]
        private TextMeshProUGUI txtAmount;

        [SerializeField]
        private Image imgIcon;

        [SerializeField]
        private GameObject goNoti;

        [SerializeField]
        private Button btnItemInfo;

        [SerializeField]
        private Button btnBuy;

        // --- Private Fields ---
        private Action<int> _onClickBuy;
        private int _shopIdx;
        private int _itemId;

        private void Awake()
        {
            btnBuy.onClick.AddListener(OnClickBuy);
            btnItemInfo.onClick.AddListener(OnClickItemInfo);
        }

        private void OnDestroy()
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);
            btnItemInfo.onClick.RemoveListener(OnClickItemInfo);
        }

        private void OnClickItemInfo()
        {
            UIManager.Instance
                .OpenPopupAsync<PopupItemInfoView>(new PopupItemInfoArgs
                {
                    ItemId = _itemId,
                })
                .Forget();
        }

        private void OnClickBuy()
        {
            _onClickBuy?.Invoke(_shopIdx);
        }

        public void Bind(
            int itemId,
            int shopIdx,
            string title,
            string subtitle,
            EEventWheelShopLimitType limitType,
            int limitCurrentValue,
            int limitTargetValue,
            int priceValue,
            int amountValue,
            Sprite itemIcon,
            bool hasNoti,
            Action<int> onClickBuy
        )
        {
            goNoti.SetActive(hasNoti);
            BindLimitType(limitType, limitCurrentValue, limitTargetValue);

            _itemId = itemId;
            _shopIdx = shopIdx;
            _onClickBuy = onClickBuy;

            btnBuy.interactable = limitTargetValue <= 0 || limitCurrentValue < limitTargetValue;
            imgIcon.sprite = itemIcon;
            txtTitle.text = title;
            txtPrice.text = $"{priceValue:N0}";
            txtAmount.text = $"{amountValue}";
            txtSubtitle.text = subtitle;

            var hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
            txtSubtitle.gameObject.SetActive(hasSubtitle);
        }

        private void BindLimitType(
            EEventWheelShopLimitType limitType,
            int limitCurrentValue,
            int limitTargetValue
        )
        {
            switch (limitType)
            {
                case EEventWheelShopLimitType.Account:
                    txtLimitType.text = $"Tài khoản {limitCurrentValue}/{limitTargetValue}";
                    break;

                case EEventWheelShopLimitType.Daily:
                    txtLimitType.text = $"Ngày {limitCurrentValue}/{limitTargetValue}";
                    break;
            }
        }
    }
}