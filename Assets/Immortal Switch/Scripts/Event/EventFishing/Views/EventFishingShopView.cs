using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventFishing.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing.Views
{
    public class EventFishingShopView : AnimatedUIView
    {
        [SerializeField]
        private UIEventFishingShopItem shopNormal;

        [SerializeField]
        private UIEventFishingShopItem shopPremium;

        [SerializeField]
        private Button btnClose;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            var shop = DatabaseManager.Instance.GetEventFishingShop();
            var normalItem = shop.FirstOrDefault(v => !v.isPremium);
            var premiumItem = shop.FirstOrDefault(v => v.isPremium);

            if (normalItem != null)
            {
                shopNormal.Bind(normalItem, OnClickBuy);
            }

            if (premiumItem != null)
            {
                shopPremium.Bind(premiumItem, OnClickBuy);
            }
        }

        private void OnClickBuy(DynamicHeroesGlobalSpecificationsEventFishingShopRow row)
        {
            BuyAsync(row.shopId).Forget();
        }

        private async UniTaskVoid BuyAsync(int shopId)
        {
            var (success, rewards, error) = await EventFishingManager.Instance.ShopBuyAsync(shopId);

            if (!success)
            {
                Debug.LogWarning($"[EventFishingShopView] eventfishing/shop_buy shop_id={shopId} failed: {error}");
                return;
            }

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void OnDestroy()
        {
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<EventFishingShopView>();
        }
    }
}