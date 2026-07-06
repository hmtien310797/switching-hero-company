using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
{
    public static class SkillSummonResultGrouper
    {
        public static List<SkillSummonGroupedResultEntry> Group(SkillSummonResult result)
        {
            var groupedMap = new Dictionary<int, SkillSummonGroupedResultEntry>();
            var orderedResult = new List<SkillSummonGroupedResultEntry>();

            if (result == null || result.Entries == null)
                return orderedResult;

            for (int i = 0; i < result.Entries.Count; i++)
            {
                var entry = result.Entries[i];
                var skill = entry.SkillAsset;
                int groupKey = entry.SkillId;

                if (!groupedMap.TryGetValue(groupKey, out var grouped))
                {
                    grouped = new SkillSummonGroupedResultEntry
                    {
                        SkillAsset = skill,
                        Icon = skill != null ? skill.SkillIcon : null,
                        SkillName = entry.SkillName,
                        Count = 0,
                        IsNewSkill = false,
                        TotalShardGained = 0,
                        Grade = entry.Grade
                    };

                    groupedMap.Add(groupKey, grouped);
                    orderedResult.Add(grouped);
                }

                grouped.Count += 1;
                grouped.TotalShardGained += entry.ShardGained;

                if (entry.IsNewSkill)
                    grouped.IsNewSkill = true;
            }

            return orderedResult;
        }
    }
}