using System.Collections.Generic;
using Immortal_Switch.Scripts.Bag.Views.Shared;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

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
            recyclableView.Bind(items.Count, OnResolveItem);
        }

        private ItemData OnResolveItem(int itemIdx)
        {
            if (itemIdx < 0 ||
                _items.Count < itemIdx)
            {
                return null;
            }

            return _items[itemIdx];
        }
    }
}