using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonGroupedResultEntry
    {
        public StandardWeaponDefinitionSO Weapon;
        public int WeaponId;
        public string WeaponName;
        public Sprite Icon;
        public WeaponTier Tier;
        public int Star;

        public int Count;
        public int TotalShardGained;
        public int TotalShardAfter;
        public bool IsNewWeapon;
    }
}