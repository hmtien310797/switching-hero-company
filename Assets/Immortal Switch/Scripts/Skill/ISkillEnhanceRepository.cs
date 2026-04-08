using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Skill
{
    public interface ISkillEnhanceRepository
    {
        List<SkillEnhanceState> GetAllSkillStates();
        SkillEnhanceState GetSkillState(int skillId);
        void SetSkillState(int skillId, SkillEnhanceState state);
        void Save();
    }
}