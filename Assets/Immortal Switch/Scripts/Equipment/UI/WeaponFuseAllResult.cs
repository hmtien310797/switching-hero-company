using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Equipment.UI
{
    public class WeaponFuseAllResult
    {
        public List<WeaponFuseAllRewardEntry> Rewards = new();

        public bool HasAnyReward => Rewards != null && Rewards.Count > 0;
    }
}