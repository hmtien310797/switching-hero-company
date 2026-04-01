using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public enum SummonAchievementTab
    {
        Heroic,
        Weapon,
        Skill,
        Pet
    }

    public enum SummonAchievementRewardState
    {
        Normal,
        Claimed
    }
    public class SummonAchievementRewardListData
    {
        public SummonAchievementTab Tab;
        public List<SummonAchievementRewardItemData> Items = new();
    }

    public class SummonAchievementRewardItemData
    {
        public int Level;
        public string Title;
        public string RewardText;
        public Sprite RewardIcon;
        public SummonAchievementRewardState State;
    }
}