using System;
using System.Numerics;

namespace Immortal_Switch.Scripts.ItemSystem.Models
{
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