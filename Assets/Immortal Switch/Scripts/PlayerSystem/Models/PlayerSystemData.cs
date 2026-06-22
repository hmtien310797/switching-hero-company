using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PlayerSystem.Models
{
    public class PlayerSystemData
    {
        /// <summary>
        /// exp
        /// </summary>
        public int Exp;

        /// <summary>
        /// level hien tai
        /// </summary>
        public int Level;

        /// <summary>
        /// current stage
        /// </summary>
        public int CurrentStage;

        /// <summary>
        /// ten hien thi
        /// </summary>
        public string Nickname;

        /// <summary>
        /// avatar
        /// </summary>
        public string Avatar;

        /// <summary>
        /// lan cuoi dang nhap
        /// </summary>
        public DateTime? LastLogin;

        public void UpdateExp(int exp)
        {
            Exp = exp;
        }

        public void UpdateLevel(int level)
        {
            Level = level;
        }
    }

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
        public EEquipmentTier ParsedTier => Enum.TryParse<EEquipmentTier>(Tier, true, out var result) ? result : EEquipmentTier.D;
    }

    public class PlayerEquipViewData : PlayerEquipItem
    {
        /// <summary>
        /// ten equip
        /// </summary>
        public string Title;
    }
}