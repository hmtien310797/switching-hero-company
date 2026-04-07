using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.GachaSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [CreateAssetMenu(fileName = "SkillSummonConfig", menuName = "ScriptableObjects/Summon/SkillSummonConfig")]
    public class SkillSummonConfigSO : ScriptableObject
    {
        public List<SkillSummonOptionEntry> SummonOptions = new();
        public List<SkillSummonLevelEntry> SummonLevels = new();
        public List<SummonLevelRewardEntry> LevelRewards = new();

        [Header("Skill Pool")]
        public List<SkillDataSO> SkillPool = new();

        public SkillSummonOptionEntry GetOption(string optionId)
        {
            return SummonOptions.Find(x => x != null && x.OptionId == optionId);
        }

        public SkillSummonLevelEntry GetExactLevelEntry(int summonLevel)
        {
            return SummonLevels.Find(x => x != null && x.SummonLevel == summonLevel);
        }

        public SummonLevelRewardEntry GetRewardEntry(int summonLevel)
        {
            return LevelRewards.Find(x => x != null && x.SummonLevel == summonLevel);
        }

        public int GetMaxSummonLevel()
        {
            if (SummonLevels == null || SummonLevels.Count == 0)
                return 1;

            return Mathf.Max(1, SummonLevels.Max(x => x != null ? x.SummonLevel : 1));
        }
    }

    [Serializable]
    public class SkillSummonOptionEntry
    {
        public string OptionId;
        public string DisplayName;
        [Min(1)] public int RollCount = 1;
        [Min(0)] public int TicketCost = 1;
        [Min(0)] public int GemCost = 1;
        public bool Enabled = true;
    }

    [Serializable]
    public class SkillSummonLevelEntry
    {
        [Min(1)] public int SummonLevel = 1;
        [Min(0)] public int TotalRollRequired = 0; // threshold tổng roll để đạt level này

        [Range(0, 100)] public float GradeBRate;
        [Range(0, 100)] public float GradeARate;
        [Range(0, 100)] public float GradeSRate;
        [Range(0, 100)] public float GradeSSRate;
    }

    [Serializable]
    public class SkillSummonLevelRewardEntry
    {
        [Min(1)] public int SummonLevel;
        public List<SummonRewardItem> RewardItems = new();
    }
}