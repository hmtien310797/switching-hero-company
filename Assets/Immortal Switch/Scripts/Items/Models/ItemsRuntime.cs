using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using UnityEngine;

namespace Immortal_Switch.Scripts.Items.Models
{
    public class ItemsRuntime
    {
        /// <summary>
        /// ds items sync voi server
        /// </summary>
        public Dictionary<int, ItemData> Items = new();
    }

    public interface IItemDisplayData
    {
        public Sprite ItemIcon { get; set; }
        public ItemTierEntry TierInfo { get; set; }
    }

    public class ItemRewardData : ItemData, IItemDisplayData
    {
        public ItemRewardData(string itemKey, BigNumber quantity) : base(itemKey, quantity)
        {
        }

        public ItemRewardData(string itemKey, BigNumber quantity, Sprite itemIcon, ItemTierEntry tierInfo)
            : base(itemKey, quantity)
        {
            ItemIcon = itemIcon;
            TierInfo = tierInfo;
        }

        public ItemRewardData(int itemId, BigNumber quantity) : base(itemId, quantity)
        {
        }

        public ItemRewardData(int itemId, BigNumber quantity, Sprite itemIcon, ItemTierEntry tierInfo)
            : base(itemId, quantity)
        {
            ItemIcon = itemIcon;
            TierInfo = tierInfo;
        }

        public Sprite ItemIcon { get; set; }
        public ItemTierEntry TierInfo { get; set; }
    }

    public class ItemDisplayData : IItemDisplayData
    {
        public Sprite ItemIcon { get; set; }
        public ItemTierEntry TierInfo { get; set; }
    }

    public class ItemData
    {
        public int ItemId;
        public string ItemKey;
        public BigNumber Quantity;

        public ItemData(string itemKey, BigNumber quantity)
        {
            ItemKey = itemKey;
            Quantity = quantity;
        }

        public ItemData(int itemId, BigNumber quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}