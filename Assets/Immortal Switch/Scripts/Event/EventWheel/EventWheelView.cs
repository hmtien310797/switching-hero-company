using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Event.EventWheel.Layout;
using Immortal_Switch.Scripts.Event.EventWheel.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.Shop.IAP;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel
{
    public enum EEventWheelTab
    {
        /// <summary>
        /// event
        /// </summary>
        Event = 0,

        /// <summary>
        /// shop
        /// </summary>
        Shop = 1,

        /// <summary>
        /// ticket
        /// </summary>
        Ticket = 2,
    }

    public enum EEventWheelShopLimitType
    {
        /// <summary>
        /// tai khoan
        /// </summary>
        Account = 0,

        /// <summary>
        /// ngay
        /// </summary>
        Daily = 1,
    }

    [Serializable]
    public class EventWheelTab
    {
        public EEventWheelTab tab;
        public string localizedKey;
        public UITabPreset preset;
        public GameObject layout;
    }

    public class EventWheelView : AnimatedUIView
    {
        [SerializeField]
        private List<EventWheelTab> tabs = new();

        [SerializeField]
        private UIEventCountdownTimer countdownTimer;

        // --- Private Fields ---
        private EventWheelTab _selectedTab;

        private void Awake()
        {
            BindTabs();
            OnClickTab(0);
        }

        private void OnEnable()
        {
            var remainTimeEvent = DatabaseManager.Instance.GetRemainTimeEvent(EventIdConstants.EVENT_WHEEL);
            countdownTimer.Bind(remainTimeEvent);
        }

        private void BindTabs()
        {
            for (var index = 0; index < tabs.Count; index++)
            {
                var tab = tabs[index];
                tab.preset.Bind(index, tab.localizedKey, OnClickTab);
                tab.preset.SetStatus(ETabPresetStatus.Normal);
                tab.layout.SetActive(false);
            }
        }

        private void OnClickTab(int idx)
        {
            if (_selectedTab != null)
            {
                _selectedTab.preset.SetStatus(ETabPresetStatus.Normal);
                _selectedTab.layout.SetActive(false);
                _selectedTab = null;
            }

            _selectedTab = tabs[idx];
            _selectedTab.preset.SetStatus(ETabPresetStatus.Selected);
            _selectedTab.layout.SetActive(true);
            BindTab();
        }

        private void BindTab()
        {
            if (_selectedTab == null)
            {
                Debug.LogError("[EventWheelView] No tab selected");
                return;
            }

            switch (_selectedTab.tab)
            {
                case EEventWheelTab.Event:
                {
                    var evt = DatabaseManager.Instance.GetEventIfActive(EventIdConstants.EVENT_WHEEL);
                    var normalItems = DatabaseManager.Instance.GetEventWheelRewardsPool(EEventCategory.Normal);
                    var premiumItems = DatabaseManager.Instance.GetEventWheelRewardsPool(EEventCategory.Premium);

                    var normalX1 = 0;
                    var premiumX1 = 0;

                    if (evt != null)
                    {
                        var consumedItemCfg = evt.consumedItemId.Split(';');

                        foreach (var x in consumedItemCfg)
                        {
                            var consumed = x.Split(':');

                            if (consumed.Length > 0 &&
                                int.TryParse(consumed[0], out var itemId) &&
                                int.TryParse(consumed[1], out var consume))
                            {
                                switch (itemId)
                                {
                                    case ItemIdConstants.WHEEL_TICKET_NORMAL:
                                        normalX1 = consume;
                                        break;

                                    case ItemIdConstants.WHEEL_TICKET_PREMIUM:
                                        premiumX1 = consume;
                                        break;
                                }
                            }
                        }
                    }

                    _selectedTab.layout
                        .GetComponent<EventLayout>()
                        .Bind(
                            normalX1, normalX1 * 10,
                            premiumX1, premiumX1 * 10,
                            normalItems, premiumItems
                        );

                    break;
                }

                case EEventWheelTab.Shop:
                    var shopItems = DatabaseManager.Instance.GetEventShopItem(EventIdConstants.EVENT_WHEEL);
                    _selectedTab.layout.GetComponent<ShopLayout>().Bind(shopItems);
                    break;

                case EEventWheelTab.Ticket:
                    var passCfg = DatabaseManager.Instance.GetEventPassConfig(EventIdConstants.EVENT_WHEEL);
                    var passItems = DatabaseManager.Instance.GetEventPassItem(EventIdConstants.EVENT_WHEEL);
                    var product = passCfg == null ? null : DatabaseManager.Instance.GetProduct(passCfg.productId);

                    passItems.Reverse();

                    _selectedTab.layout
                        .GetComponent<TicketLayout>()
                        .Bind(
                            passItems,
                            EventIdConstants.EVENT_WHEEL,
                            OnClickBuyProduct,
                            $"{product?.price}",
                            IAPManager.GetStoreProductId(product),
                            passCfg?.productId ?? 1
                        );

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnClickBuyProduct(string storeProductId, int packId)
        {
            IAPManager.Instance.BuyProduct(packId, storeProductId, (success, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[EventWheelView] Purchase failed -> product={storeProductId}, error={error}");
                    return;
                }

                EventWheelPassManager.Instance.PurchasePremium(EventIdConstants.EVENT_WHEEL);
            });
        }
    }
}