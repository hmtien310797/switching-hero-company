using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Profile.Models
{
    public class PlayerEquipItem
    {
        /// <summary>
        /// cfg id cua equip item.
        /// </summary>
        public string CfgId;

        /// <summary>
        /// loai equip item
        /// </summary>
        public string ItemType;

        /// <summary>
        /// level item
        /// </summary>
        public int Level;

        /// <summary>
        /// loai tier
        /// </summary>
        public string Tier;

        /// <summary>
        /// ds modifier của equip
        /// </summary>
        public List<StatModifier> Modifiers = new();

        /// <summary>
        /// parsed tier string sang enum
        /// </summary>
        public EItemTier ParsedTier => Enum.TryParse<EItemTier>(Tier, true, out var result) ? result : EItemTier.D;
    }

    public class PlayerEquipViewData : PlayerEquipItem
    {
        /// <summary>
        /// ten equip
        /// </summary>
        public string Title;
    }
}