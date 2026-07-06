using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UI
{
    public class WeaponFuseAllRewardEntry
    {
        public int WeaponId;
        public string WeaponName;
        public HeroClass HeroClass;
        public WeaponTier Tier;
        public int Star;
        public bool IsExclusive;
        public int Amount;
        public Sprite Icon;
    }
}