using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Skill
{
    [Serializable]
    public class SkillEnhanceState
    {
        public int SkillId;
        public int Level = 1;
        public int ShardCount = 0;
        public bool IsUnlocked = false;
    }

    [Serializable]
    public class SkillEnhanceAllEntry
    {
        public int SkillId;
        public int OldLevel;
        public int NewLevel;
        public int OldShard;
        public int NewShard;
        public int ShardSpent;
        public bool WasUnlockedBefore;
        public bool IsUnlockedAfter;
    }

    [Serializable]
    public class SkillEnhanceAllResult
    {
        public int ProcessedSkillCount;
        public int UpgradedSkillCount;
        public int TotalLevelGained;
        public int TotalShardSpent;
        public List<SkillEnhanceAllEntry> Entries = new();
    }
}