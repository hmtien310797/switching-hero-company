using Battle;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UI
{
    public class WeaponFusionPopupViewModel
    {
        public int WeaponId;
        public string WeaponName;
        public Sprite WeaponIcon;
        public WeaponTier Tier;
        public int Star;
        public int CurrentShard;
        public int RequiredShardPerFusion;

        public CurrencyType ConsumableCurrencyType;
        public Sprite ConsumableCurrencyIcon;
        public BigNumber CurrentConsumableAmount;
        public BigNumber ConsumableCostPerFusion;

        public int CurrentFusionCount;
        public int MaxFusionCount;

        public bool CanFusion;
    }
}