using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Reward
{
    [Serializable]
    public class RewardAmountDto
    {
        public string currencyType;
        public string amount;
    }

    [Serializable]
    public class ClearStageRewardRequest
    {
        public int stage;
        public int chapterId;
        public int localStage;
        public List<RewardAmountDto> rewards;
    }

    [Serializable]
    public class OnlineIdleRewardRequest
    {
        public int stage;
        public int chapterId;
        public int elapsedSeconds;
        public List<RewardAmountDto> rewards;
    }

    [Serializable]
    public class OfflineAfkRewardRequest
    {
        public int afkStage;
        public int elapsedSeconds;
        public List<RewardAmountDto> rewards;
    }

    [Serializable]
    public class RewardClaimResponse
    {
        public bool success;
        public string error;
        public List<RewardAmountDto> rewardsAdded;
        public List<RewardAmountDto> balances;
    }
}