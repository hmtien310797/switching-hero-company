using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.Skill.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillSummon
{
    public class SkillProgressionService
    {
        public bool HasSkill(int skillId)
        {
            return SkillInventorySaveService.IsOwned(skillId);
        }

        public int GetLevel(int skillId)
        {
            return SkillInventorySaveService.GetLevel(skillId);
        }

        public int GetCurrentShard(int skillId)
        {
            return SkillInventorySaveService.GetCurrentShard(skillId);
        }

        public void AcquireSkill(int skillId)
        {
            SkillInventorySaveService.SetOwned(skillId, true);
        }

        public void AddShard(int skillId, int amount)
        {
            SkillInventorySaveService.AddShard(skillId, amount);
        }

        public void AcquireOrAddDuplicate(SkillDataSO skillData, int duplicateShardAmount = 1)
        {
            if (skillData == null)
                return;

            int skillId = skillData.SkillId;

            if (!HasSkill(skillId))
            {
                AcquireSkill(skillId);
                return;
            }

            AddShard(skillId, Mathf.Max(1, duplicateShardAmount));
        }
    }
}