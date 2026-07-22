using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Event.Views.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.Shared.Views;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout
{
    public class EventLeHoiBangLongSevenDayLoginLayout : MonoBehaviour
    {
        [SerializeField]
        private UIEventLeHoiBangLongSevenDayRewardPanel rewardPanel;

        [SerializeField]
        private UICountdownTimer countdownTimer;

        [Header("Seven day references")]
        [SerializeField]
        private RectTransform sevenDayContainer;

        [SerializeField]
        private UIEventLeHoiBangLongSevenDayItem sevenDayPrefab;

        [Header("Event point reward references")]
        [SerializeField]
        private RectTransform eventPointRewardContainer;

        [SerializeField]
        private UIItemSlot eventPointRewardPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIEventLeHoiBangLongSevenDayItem> _sevenDayPools;
        private SimpleUIPool<UIItemSlot> _eventPointRewardPools;
        private List<DynamicHeroesGlobalSpecificationsEventBLCheckInRow> _rows = new();
        private IList<int> _eventPointRewards = Array.Empty<int>();

        private void OnEnable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged += RefreshState;
        }

        private void OnDisable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged -= RefreshState;
        }

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventBLCheckInRow> rows,
            IList<int> eventPointRewards,
            double remainTime
        )
        {
            _rows = rows ?? new List<DynamicHeroesGlobalSpecificationsEventBLCheckInRow>();
            _eventPointRewards = eventPointRewards ?? Array.Empty<int>();

            countdownTimer.Bind(remainTime, OnCountdown);
            RefreshState();
        }

        private string OnCountdown(long days, long hours, long minutes, long seconds)
        {
            return $"Kết thúc sau: {days:00} ngày {hours:00}:{minutes:00}:{seconds:00}";
        }

        private void RefreshEventPointRewards(IList<int> rows)
        {
            _eventPointRewardPools ??= new SimpleUIPool<UIItemSlot>(eventPointRewardPrefab, eventPointRewardContainer);

            for (int i = 0; i < rows.Count; i++)
            {
                var reward = rows[i];
                var clone = _eventPointRewardPools.Get(i);

                clone.Bind(reward);
            }

            _eventPointRewardPools.ReleaseFrom(rows.Count);
        }

        private void RefreshSevenDay(
            List<DynamicHeroesGlobalSpecificationsEventBLCheckInRow> rows,
            int currentDay
        )
        {
            _sevenDayPools ??= new SimpleUIPool<UIEventLeHoiBangLongSevenDayItem>(sevenDayPrefab, sevenDayContainer);

            for (int i = 0; i < rows.Count; i++)
            {
                var reward = rows[i];
                var clone = _sevenDayPools.Get(i);

                clone.Bind(
                    reward,
                    OnClickClaim,
                    currentDay,
                    EventLeHoiBangLongManager.Instance.IsFreeLoginRewardClaimed(reward.day)
                );
            }

            _sevenDayPools.ReleaseFrom(rows.Count);
        }

        private void OnClickClaim(int day)
        {
            OnClickClaimAsync(day).Forget();
        }

        private async UniTaskVoid OnClickClaimAsync(int day)
        {
            var rewards = await EventLeHoiBangLongManager.Instance.ClaimFreeLoginReward(day);
            ShowRewards(rewards);
        }

        private void OnClickRewardPanel(int day)
        {
            OnClickRewardPanelAsync(day).Forget();
        }

        private async UniTaskVoid OnClickRewardPanelAsync(int day)
        {
            var manager = EventLeHoiBangLongManager.Instance;

            if (manager.CanClaimFreeBonus(day))
            {
                var rewards = await manager.ClaimFreeBonus(day);
                ShowRewards(rewards);
            }
            else if (manager.CanPurchaseBonus(day))
            {
                manager.RequestBonusPurchase(day, ShowRewards);
            }
            else if (manager.CanClaimBonus(day))
            {
                var confirmed = await manager.ClaimBonusPaid(day);

                if (confirmed)
                {
                    var rewards = DatabaseManager.Instance.GetEventLHBLCheckInBonusRewards(day);
                    ShowRewards(rewards.bonusRewards);
                }
            }
        }

        private static void ShowRewards(IReadOnlyList<ItemData> rewards)
        {
            if (rewards == null ||
                rewards.Count == 0)
            {
                return;
            }

            PopupRewardService.Show(rewards);
        }

        private void RefreshState()
        {
            var manager = EventLeHoiBangLongManager.Instance;
            var currentDay = Math.Clamp(manager.State?.Progress?.LoginDay ?? 1, 1, 7);
            var rewards = DatabaseManager.Instance.GetEventLHBLCheckInBonusRewards(currentDay);

            RefreshSevenDay(_rows, currentDay);
            RefreshEventPointRewards(_eventPointRewards);

            rewardPanel.Bind(
                rewards.instantRewards,
                rewards.bonusRewards,
                OnClickRewardPanel,
                manager.IsFreeBonusClaimed(currentDay),
                manager.IsBonusPurchased(currentDay),
                manager.IsBonusClaimed(currentDay),
                rewards.packPrice,
                currentDay
            );
        }
    }
}