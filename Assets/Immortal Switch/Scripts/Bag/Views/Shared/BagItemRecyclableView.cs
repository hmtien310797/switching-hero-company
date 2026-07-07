using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Bag.Views.UI;
using Immortal_Switch.Scripts.Items.Models;
using RecyclableScrollRect;
using UnityEngine;

namespace Immortal_Switch.Scripts.Bag.Views.Shared
{
    public class BagItemRecyclableView : MonoBehaviour, IGridDataSource
    {
        [Header("RSR")]
        [SerializeField]
        private RSRGrid rsr;

        [SerializeField]
        private GameObject itemPrototype;

        public int ItemsCount { get; private set; }

        public GameObject[] PrototypeItems =>
            itemPrototype != null
                ? new[]
                {
                    itemPrototype,
                }
                : Array.Empty<GameObject>();

        // --- Private Fields ---
        private List<ItemData> _items = new();
        private Func<int, ItemRewardData> _onResolveItem;

        public void Bind(List<ItemData> items, int itemsCount, Func<int, ItemRewardData> onResolveItem)
        {
            var isChanged = itemsCount != ItemsCount;

            _items = items;
            _onResolveItem = onResolveItem;
            ItemsCount = itemsCount;

            if (!rsr.IsInitialized)
            {
                rsr.Initialize(this);
            }
            else
            {
                rsr.ReloadData(isChanged);
            }

            ForceRefreshVisibleItems();
        }

        private void ForceRefreshVisibleItems()
        {
            for (var i = 0; i < ItemsCount; i++)
            {
                rsr.ReloadItem(i);
            }
        }

        public GameObject GetItemPrototype(int itemIndex)
        {
            return itemPrototype;
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

        private void SetItemDataInternal(IItem item, int itemIndex)
        {
            if (item is not UIBagItem ui)
            {
                return;
            }

            var data = _onResolveItem?.Invoke(itemIndex);

            if (data != null)
            {
                ui.Bind(
                    data.ItemIcon,
                    data.TierInfo.border,
                    data.TierInfo.background,
                    data.TierInfo.tierIcon,
                    data.Quantity
                );
            }
        }
    }
}