using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventWheel.Layout;
using Immortal_Switch.Scripts.Event.Views.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
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
        private UICountdownTimer countdownTimer;

        // --- Private Fields ---
        private EventWheelTab _selectedTab;

        private void Awake()
        {
            BindTabs();
        }

        private void OnEnable()
        {
            RefreshAndBindAsync().Forget();
        }

        /// <summary>Tải state server (cửa sổ thời gian, số dư vé/point, tiến trình pass, giới hạn
        /// shop) mỗi lần view được mở — mọi tab con + đồng hồ đếm ngược đều đọc qua
        /// EventWheelPassManager.State, không còn dùng DatabaseManager.GetRemainTimeEvent cục bộ.</summary>
        private async UniTaskVoid RefreshAndBindAsync()
        {
            await EventWheelPassManager.Instance.RefreshAsync();
            BindCountdown();
            OnClickTab(0);
        }

        private void BindCountdown()
        {
            var window = EventWheelPassManager.Instance.State?.Window;
            var endMs = window?.WheelEndMs;

            var remainSeconds = -1d;

            if (endMs.HasValue)
            {
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                remainSeconds = Math.Max(0d, (endMs.Value - nowMs) / 1000d);
            }

            countdownTimer.Bind(remainSeconds, OnCountdown);
        }

        private string OnCountdown(long days, long hours, long minutes, long seconds)
        {
            return $"{days:00} ngày {hours:00} giờ {minutes:00} phút";
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
                {
                    var passItems = DatabaseManager.Instance.GetEventPassItem(EventIdConstants.EVENT_WHEEL);
                    passItems.Reverse();

                    // Giá + mua Premium Pass đi qua server (State.PremiumPack /
                    // EventWheelPassManager.BuyPremiumAsync), KHÔNG dùng
                    // DatabaseManager.GetEventPassConfig/GetProduct cục bộ nữa — tránh lệch
                    // product_id giữa client/server (xem EventWheelPassManager.BuyPremiumAsync).
                    var premiumPack = EventWheelPassManager.Instance.State?.PremiumPack;
                    var price = premiumPack != null ? $"${premiumPack.PriceUsd}" : "--";

                    _selectedTab.layout
                        .GetComponent<TicketLayout>()
                        .Bind(passItems, EventIdConstants.EVENT_WHEEL, price);

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}