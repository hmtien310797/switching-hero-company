using System;
using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopProductLimit : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtPrice;

        [SerializeField]
        private TextMeshProUGUI txtBonus;

        [SerializeField]
        private TextMeshProUGUI txtLimit;

        [SerializeField]
        private Button btnBuy;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIShopProductItem rewardPrefab;

        [SerializeField]
        private Image imgChest;

        // --- Private Fields ---
        private List<UIShopProductItem> _rewards = new();
        private Action<string, int> _onClickBuy;

        private DynamicHeroesGlobalSpecificationsProductIdRow _product;
        private DynamicHeroesGlobalSpecificationsPackIapRow _iap;

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

        private void OnPurchaseDataChanged()
        {
            if (_iap != null)
            {
                RefreshLimit();
            }
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

            _onClickBuy?.Invoke(storeProductId, _iap.iD);
        }

        public void Bind(string key, string price,
            DynamicHeroesGlobalSpecificationsProductIdRow product,
            DynamicHeroesGlobalSpecificationsPackIapRow iap,
            Action<string, int> onClickBuy,
            IReadOnlyList<ItemRewardData> rewards
        )
        {
            txtPrice.text = price;
            txtTitle.text = LocalizationManager.GetText(key);

            _onClickBuy = onClickBuy;
            _product = product;
            _iap = iap;

            RefreshLimit();
            RefreshBonus();

            if (iap.iD is PackIdConstants.ID_MONTHLY_NORMAL
                or PackIdConstants.ID_MONTHLY_PREMIUM
                or PackIdConstants.ID_DAILY_SPECIAL
                or PackIdConstants.ID_WEEKLY_SPECIAL)
            {
                rewardContainer.gameObject.SetActive(true);
                RefreshRewards(rewards);
            }
            else
            {
                rewardContainer.gameObject.SetActive(false);
                imgChest.gameObject.SetActive(true);
                imgChest.sprite = ShopManager.Instance.Atlas.LoadIcon(iap.iconId);
            }
        }

        private void RefreshLimit()
        {
            var remaining = ShopManager.Instance.GetRemaining(_iap.iD, _iap.limit);
            var purchased = ShopManager.Instance.GetPurchasedCount(_iap.iD);

            txtLimit.text = $"Giới Hạn ({purchased}/{_iap.limit})";
            btnBuy.interactable = remaining > 0;
        }

        private void RefreshBonus()
        {
            txtBonus.text = $"Giá trị <size=30>{_iap.bonus}</size>";
        }

        private void RefreshRewards(IReadOnlyList<ItemRewardData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];

                if (_rewards.Count > i)
                {
                    var clone = _rewards[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(reward.ItemIcon, reward.Quantity);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);
                    clone.Bind(reward.ItemIcon, reward.Quantity);
                    _rewards.Add(clone);
                }
            }
        }
    }
}