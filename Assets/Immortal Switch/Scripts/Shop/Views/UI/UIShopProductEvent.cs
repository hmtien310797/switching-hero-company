using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopProductEvent : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtPrice;

        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        [Header("Limit references")]
        [SerializeField]
        private TextMeshProUGUI txtLimit;

        [SerializeField]
        private GameObject goLimit;

        [SerializeField]
        private TextMeshProUGUI txtType;

        [SerializeField]
        private Image imgIcon;

        [SerializeField]
        private Button btnBuy;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIShopProductItem rewardPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIShopProductItem> _pools;
        private Action<string, int> _onClickBuy;
        private DynamicHeroesGlobalSpecificationsProductIdRow _product;
        private int _packId;
        private int _limit;

        private void Awake()
        {
            btnBuy.onClick.AddListener(OnClickBuy);
        }

        private void OnDestroy()
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);
            ShopManager.Instance.OnDataChanged -= OnPurchaseDataChanged;
        }

        private void OnEnable()
        {
            ShopManager.Instance.OnDataChanged += OnPurchaseDataChanged;
        }

        private void OnDisable()
        {
            ShopManager.Instance.OnDataChanged -= OnPurchaseDataChanged;
        }

        private void OnClickBuy()
        {
            if (_product == null)
            {
                return;
            }

#if UNITY_IOS
            string storeProductId = _product.appleID;
#elif UNITY_ANDROID
            string storeProductId = _product.googleID;
#else
            string storeProductId = _product.googleID;
#endif

            _onClickBuy?.Invoke(storeProductId, _packId);
        }

        public void Bind(
            string title, float price,
            DynamicHeroesGlobalSpecificationsProductIdRow product,
            int packId, string type,
            int currentValue, int limit,
            DateTime endTime,
            Action<string, int> onClickBuy, string iconId,
            IReadOnlyList<ItemData> rewards
        )
        {
            txtPrice.text = Mathf.Approximately(price, 0f) ? "Free" : price.ToString(CultureInfo.InvariantCulture);
            txtTitle.text = title;
            txtLimit.text = $"{currentValue:00}/{limit:00}";
            txtCountdown.text = $"Sale time: {endTime.Day:00}d {endTime.Hour:00}h";

            _onClickBuy = onClickBuy;
            _product = product;
            _packId = packId;
            _limit = limit;

            RefreshLimitReset(type, limit);
            RefreshPurchaseState();
            RefreshRewards(rewards);

            var icon = ShopManager.Instance.Atlas.LoadIcon(iconId);

            if (icon != null)
            {
                imgIcon.sprite = icon;
            }
        }

        private void OnPurchaseDataChanged()
        {
            RefreshPurchaseState();
        }

        private void RefreshPurchaseState()
        {
            var purchased = ShopManager.Instance.GetEventPurchasedCount(_packId);
            var remaining = ShopManager.Instance.GetEventRemaining(_packId, _limit);

            txtLimit.text = $"{purchased:00}/{_limit:00}";
            btnBuy.interactable = remaining > 0;
        }

        private void RefreshLimitReset(string limitReset, int limit)
        {
            var type = limitReset switch
            {
                "daily" => "Ngày",
                "weekly" => "Tuần",
                _ => string.Empty,
            };

            if (limit <= 0 || string.IsNullOrEmpty(type))
            {
                goLimit.SetActive(false);
            }
            else
            {
                txtType.text = type;
                goLimit.SetActive(true);
            }
        }

        private void RefreshRewards(IReadOnlyList<ItemData> rewards)
        {
            _pools ??= new SimpleUIPool<UIShopProductItem>(rewardPrefab, rewardContainer);

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var clone = _pools.Get(i);

                clone.Bind(reward.ItemId, reward.Quantity);
            }

            _pools.ReleaseFrom(rewards.Count);
        }
    }
}
