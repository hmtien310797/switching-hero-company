using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
{
    public class SkillSummonGroupedResultEntry
    {
        public SkillDataSO SkillAsset;
        public Sprite Icon;
        public string SkillName;
        public int Count;
        public bool IsNewSkill;
        public int TotalShardGained;
        public SkillSummonGrade Grade;
    }
}