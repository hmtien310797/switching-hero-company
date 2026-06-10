using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.StageSelection;

namespace Immortal_Switch.Scripts.Reward
{
    [Serializable]
    public class OfflineBattleRewardResult
    {
        public int Stage;
        public int OfflineSeconds;
        public int MaxOfflineSeconds;
        public int DefeatsPerMinute;
        public int MonstersDefeated;
        public List<StageReward> Rewards = new();
    }
}