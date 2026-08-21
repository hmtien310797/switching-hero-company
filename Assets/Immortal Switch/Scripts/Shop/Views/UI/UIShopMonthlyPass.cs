using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shop.IAP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopMonthlyPass : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtBonus;

        [SerializeField]
        private TextMeshProUGUI txtPrice;

        [SerializeField]
        private TextMeshProUGUI txtDay;

        [SerializeField]
        private Button btnBuy;

        [SerializeField]
        private GameObject instantReward;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardDailyContainer;

        [SerializeField]
        private RectTransform rewardInstantContainer;

        [SerializeField]
        private RectTransform rewardMonthlyContainer;

        [SerializeField]
        private UIShopProductItem rewardPrefab;

        // --- Private Fields ---
        private Action<string, int> _onClickBuy;

        private List<UIShopProductItem> _instantPools = new();
        private List<UIShopProductItem> _dailyPools = new();
        private List<UIShopProductItem> _monthlyPools = new();

        private DynamicHeroesGlobalSpecificationsProductIdRow _product;
        private DynamicHeroesGlobalSpecificationsPackIapRow _iap;

        private bool _isBought;

        private void Awake()
        {
            btnBuy.onClick.AddListener(OnClickBuy);

            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchased += OnIapPurchased;
            }
        }

        private void OnIapPurchased(int packId)
        {
            if (_iap.iD == packId)
            {
                RefreshStatus();
            }
        }

        private void OnDestroy()
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);

            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchased -= OnIapPurchased;
            }
        }

        private void OnClickBuy()
        {
            // Đã mua thì nút chỉ còn là trạng thái hiển thị (xem RefreshStatus) — thưởng ngày giờ
            // gửi thẳng qua mailbox, không còn thao tác nhận thủ công ở đây nữa.
            if (_isBought)
            {
                return;
            }

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

        public void Bind(DynamicHeroesGlobalSpecificationsProductIdRow product,
            DynamicHeroesGlobalSpecificationsPackIapRow iap,
            Action<string, int> onClickBuy,
            IReadOnlyList<ItemRewardData> rewardMonthly,
            IReadOnlyList<ItemRewardData> rewardDaily,
            IReadOnlyList<ItemRewardData> rewardInstant
        )
        {
            _product = product;
            _iap = iap;
            _onClickBuy = onClickBuy;
            txtBonus.text = LocalizationManager.GetText(LocalizationKeys.UI_VALUE, iap.bonus);

            RefreshStatus();
            RefreshRewards(rewardInstantContainer, _instantPools, rewardInstant);
            RefreshRewards(rewardDailyContainer, _dailyPools, rewardDaily);
            RefreshRewards(rewardMonthlyContainer, _monthlyPools, rewardMonthly);
        }

        private void RefreshRewards(Transform parent, List<UIShopProductItem> pools, IReadOnlyList<ItemRewardData> rewards)
        {
            for (var index = 0; index < rewards.Count; index++)
            {
                var reward = rewards[index];

                if (pools.Count > index)
                {
                    var clone = pools[index];
                    clone.gameObject.SetActive(true);
                    clone.Bind(reward.ItemIcon, reward.Quantity);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, parent);
                    clone.Bind(reward.ItemIcon, reward.Quantity);
                    pools.Add(clone);
                }
            }

            for (int i = rewards.Count; i < pools.Count; i++)
            {
                pools[i].gameObject.SetActive(false);
            }
        }

        private void RefreshStatus()
        {
            var currentDay = ShopManager.Instance.GetMonthlyPassCurrentDay(_iap.iD);
            var price = _product.price.ToString(CultureInfo.InvariantCulture);

            _isBought = ShopManager.Instance.IsMonthlyPassPurchased(_iap.iD);
            instantReward.SetActive(!_isBought);

            if (_isBought)
            {
                // Thưởng mỗi ngày giờ tự động gửi vào mailbox (server: rpcPlayerMe →
                // sendMonthlyPassDailyRewardsIfNeeded) — không còn thao tác nhận thủ công, nút chỉ
                // hiển thị trạng thái.
                txtPrice.text = LocalizationManager.GetText(LocalizationKeys.UI_ACTIVATED);
                txtDay.text = LocalizationManager.GetText(LocalizationKeys.UI_30_DAY_CHECK, currentDay);
                btnBuy.interactable = false;
            }
            else
            {
                txtPrice.text = price;
                btnBuy.interactable = true;
                txtDay.text = LocalizationManager.GetText(LocalizationKeys.UI_BUY_NOW_STORE);
            }
        }
    }
}