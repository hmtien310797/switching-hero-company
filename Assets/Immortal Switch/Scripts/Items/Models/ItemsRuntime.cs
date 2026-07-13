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
        public List<ItemData> Items = new();
    }

    public interface IItemDisplayData
    {
        public Sprite ItemIcon { get; set; }
        public ItemTierEntry TierInfo { get; set; }
    }

    public class ItemRewardData : ItemData, IItemDisplayData
    {
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
        public string ItemKey;
        public BigNumber Quantity;
    }
}