using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using RecyclableScrollRect;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel.Layout
{
    public class PassRecyclableView : MonoBehaviour, IRSRDataSource
    {
        [Header("RSR")]
        [SerializeField]
        private RSR rsr;

        [SerializeField]
        private RectTransform passContainer;

        [SerializeField]
        private GameObject passPrefab;

        [Header("Item Size")]
        [SerializeField]
        private bool isItemSizeKnown = true;

        [SerializeField]
        private float itemSize = 130f;

        // --- Private Fields ---
        private EventWheelPassManager _manager;
        private List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> _rows = new();
        private Func<int, DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> _onResolveItem;

        private int _eventId;

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows,
            int eventId,
            Func<int, DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> onResolveItem
        )
        {
            _manager = EventWheelPassManager.Instance;
            _rows = rows ?? new List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow>();
            _onResolveItem = onResolveItem;
            _eventId = eventId;

            if (!rsr.IsInitialized)
            {
                rsr.Initialize(this);
            }

            ForceRefreshVisibleItems();
            ScrollTo(_rows.Count - 1);
        }

        private void ForceRefreshVisibleItems()
        {
            for (var i = 0; i < ItemsCount; i++)
            {
                rsr.ReloadItem(i);
            }
        }

        public void ScrollTo(int idx)
        {
            if (ItemsCount <= 0)
            {
                return;
            }

            rsr.ScrollToItemIndex(
                idx,
                -1f,
                false,
                true
            );
        }

        private void OnClickClaim(int milestoneId)
        {
            var row = _rows.Find(item => item.milestoneId == milestoneId);

            if (row == null)
            {
                return;
            }

            ClaimAsync(row).Forget();
        }

        private async UniTaskVoid ClaimAsync(DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row)
        {
            var (rewards, error) = await _manager.ClaimMilestoneAsync(row);

            if (error != null)
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
                case "EVENT_NOT_ACTIVE":  return "Sự kiện không còn hoạt động.";
                case "ALREADY_CLAIMED":   return "Đã nhận thưởng mốc này rồi.";
                case "NOT_YET_ELIGIBLE":  return "Chưa đủ điều kiện nhận thưởng.";
                default:                  return "Nhận thưởng thất bại, vui lòng thử lại.";
            }
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

        public int ItemsCount => _rows.Count;

        public GameObject[] PrototypeItems =>
            passPrefab != null
                ? new[] { passPrefab, }
                : Array.Empty<GameObject>();

        public GameObject GetItemPrototype(int itemIndex)
        {
            return passPrefab;
        }

        public bool IsItemStatic(int itemIndex)
        {
            return false;
        }

        public void SetItemData(IItem item, int itemIndex)
        {
            SetItemDataInternal(item, itemIndex);
        }

        public void ItemCreated(int itemIndex, IItem item, GameObject itemGo)
        {
        }

        public void ItemHidden(IItem item, int itemIndex)
        {
        }

        public void ScrolledToItem(IItem item, int itemIndex)
        {
        }

        public bool IgnoreContentPadding(int itemIndex)
        {
            return false;
        }

        public void PullToRefresh()
        {
        }

        public void PushToClose()
        {
        }

        public void ReachedScrollStart()
        {
        }

        public void ReachedScrollEnd()
        {
        }

        public void LastItemIsVisible()
        {
        }

        public bool IsItemSizeKnown => isItemSizeKnown;

        public float GetItemSize(int itemIndex)
        {
            return itemSize;
        }

        private void SetItemDataInternal(IItem item, int itemIndex)
        {
            if (item is not UIEventWheelPassItem ui)
            {
                return;
            }

            var data = _onResolveItem?.Invoke(itemIndex);

            if (data != null)
            {
                var normalItems = DatabaseManager.Instance.GetRewards(data.freeItem);
                var premiumItems = DatabaseManager.Instance.GetRewards(data.paidItem);

                ui.Bind(
                    _eventId,
                    data,
                    normalItems.Count > 0 ? normalItems[0] : null,
                    premiumItems.Count > 0 ? premiumItems[0] : null,
                    OnClickClaim
                );
            }
        }
    }
}