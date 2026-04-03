using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    [Serializable]
    public class SharedSummonRewardPreviewData
    {
        public int SummonLevel;
        public string AmountText;
        public Sprite RewardIcon;
        public bool IsClaimable;
        public bool IsClaimed;
    }

    [Serializable]
    public class SharedSummonSequenceItemData
    {
        public Sprite Icon;
        public string Name;
        public string AmountText;
        public string GradeText;
        public bool IsNew;
    }

    [Serializable]
    public class SharedSummonAchievementItemData
    {
        public string Title;
        public string RewardText;
        public Sprite RewardIcon;
        public bool IsClaimed;
    }
}