using System;
using System.Collections.Generic;
using System.Numerics;
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

    public class ItemDisplayData
    {
        public Sprite ItemIcon;
        public ItemTierEntry TierInfo;
    }

    public class ItemRewardData : ItemDisplayData
    {
        public string ItemKey;
        public BigNumber Quantity;
    }

    public class ItemData
    {
        public string ItemKey;
        public BigNumber Quantity;
    }

    [Serializable]
    public struct RewardEntry
    {
        /// <summary>
        /// item id
        /// </summary>
        public string itemKey;

        /// <summary>
        /// so luong phan thuong
        /// </summary>
        public BigInteger quantity;
    }
}