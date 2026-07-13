using System.Collections.Generic;
using Immortal_Switch.Scripts.Bag.Views.Shared;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using Random = System.Random;

namespace Immortal_Switch.Scripts.Bag.Views
{
    public class BagView : AnimatedUIView
    {
        [SerializeField]
        private BagItemRecyclableView recyclableView;

        // --- Private Fields ---
        private List<ItemData> _items = new();

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is List<ItemData> items)
            {
                Bind(items);
            }
        }

        public void Bind(List<ItemData> items)
        {
            _items = new List<ItemData>(items);
            recyclableView.Bind(items, items.Count, OnResolveItem);
        }

        private ItemRewardData OnResolveItem(int itemIdx)
        {
            var item = _items[itemIdx];

            if (int.TryParse(item.ItemKey, out var itemId))
            {
                var display = DatabaseManager.Instance.GetDisplayData(itemId);

                if (display == null)
                {
                    return null;
                }

                return new ItemRewardData
                {
                    ItemKey = item.ItemKey,
                    Quantity = item.Quantity,
                    TierInfo = display.TierInfo,
                    ItemIcon = display.ItemIcon,
                };
            }

            return null;
        }
    }
}