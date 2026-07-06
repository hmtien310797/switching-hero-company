using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Level.Stage;

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