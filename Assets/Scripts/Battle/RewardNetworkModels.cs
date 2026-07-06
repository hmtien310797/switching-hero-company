using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Level.Stage;

namespace Immortal_Switch.Scripts.Reward
{
    [Serializable]
    public class ClearStageRewardRequest
    {
        public int stage;
        public int chapterId;
        public int localStage;
        public List<StageReward> rewards;
    }

    [Serializable]
    public class OnlineIdleRewardRequest
    {
        public int stage;
        public int chapterId;
        public int elapsedSeconds;
        public List<StageReward> rewards;
    }

    [Serializable]
    public class OfflineAfkRewardRequest
    {
        public int afkStage;
        public int elapsedSeconds;
        public List<StageReward> rewards;
    }

    [Serializable]
    public class RewardClaimResponse
    {
        public bool success;
        public string error;
        public List<StageReward> rewardsAdded;
        public List<StageReward> balances;
    }
}