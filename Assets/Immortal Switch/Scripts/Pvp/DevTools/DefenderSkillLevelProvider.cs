using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.Skill;

namespace Immortal_Switch.Scripts.Pvp.DevTools
{
    /// <summary>
    /// ISkillLevelProvider cho defender test: trả về skill level lấy từ config (clamp theo MaxLevel
    /// của skill). Với skill không nằm trong config thì fallback về <see cref="DefaultSkillLevelProvider"/>
    /// (server skill level) — dùng cho ultimate/passive.
    /// </summary>
    public sealed class DefenderSkillLevelProvider : ISkillLevelProvider
    {
        private readonly DefaultSkillLevelProvider fallback = new();
        private readonly Dictionary<int, int> configuredLevels = new();

        public DefenderSkillLevelProvider(SkillProgressionSnapshot skills)
        {
            if (skills?.Equipped == null) return;
            for (int i = 0; i < skills.Equipped.Count; i++)
            {
                var slot = skills.Equipped[i];
                if (slot != null && slot.SkillId > 0)
                    configuredLevels[slot.SkillId] = slot.Level;
            }
        }

        public int GetSkillLevel(SkillDataSO skillData, HeroActor owner)
        {
            if (skillData != null && configuredLevels.TryGetValue(skillData.SkillId, out int level))
                return skillData.GetSafeLevel(level);
            return fallback.GetSkillLevel(skillData, owner);
        }
    }
}