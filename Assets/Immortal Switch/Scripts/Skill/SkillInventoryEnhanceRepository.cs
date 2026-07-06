using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill.UI;

namespace Immortal_Switch.Scripts.Skill
{
    public class SkillInventoryEnhanceRepository : ISkillEnhanceRepository
    {
        public List<SkillEnhanceState> GetAllSkillStates()
        {
            var result = new List<SkillEnhanceState>();

            var allStates = SkillInventorySaveService.GetAllSkillStates();
            if (allStates == null)
                return result;

            for (int i = 0; i < allStates.Count; i++)
            {
                var s = allStates[i];
                if (s == null)
                    continue;

                result.Add(new SkillEnhanceState
                {
                    SkillId = s.SkillId,
                    Level = s.Level,
                    ShardCount = s.CurrentShard,
                    IsUnlocked = s.IsOwned
                });
            }

            return result;
        }

        public SkillEnhanceState GetSkillState(int skillId)
        {
            var s = SkillInventorySaveService.GetOrCreate(skillId);
            if (s == null)
                return null;

            return new SkillEnhanceState
            {
                SkillId = s.SkillId,
                Level = s.Level,
                ShardCount = s.CurrentShard,
                IsUnlocked = s.IsOwned
            };
        }

        public void SetSkillState(int skillId, SkillEnhanceState state)
        {
            if (state == null)
                return;

            SkillInventorySaveService.SetOwned(skillId, state.IsUnlocked);
            SkillInventorySaveService.SetLevel(skillId, state.Level);
            SkillInventorySaveService.SetCurrentShard(skillId, state.ShardCount);
        }

        public void Save()
        {
            SkillInventorySaveService.Save();
        }
    }
}