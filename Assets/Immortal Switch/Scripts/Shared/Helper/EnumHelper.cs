using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.Skill;

namespace Immortal_Switch.Scripts.Helper
{
    public static class EnumHelper
    {
        public static EItemTier GradeToItemTier(SkillSummonGrade grade)
        {
            return grade switch
            {
                SkillSummonGrade.B => EItemTier.B,
                SkillSummonGrade.A => EItemTier.A,
                SkillSummonGrade.S => EItemTier.S,
                SkillSummonGrade.SS => EItemTier.SS,
                _ => EItemTier.B,
            };
        }

        public static EItemTier TierSkillToItemTier(TierSkill tier)
        {
            return tier switch
            {
                TierSkill.B => EItemTier.B,
                TierSkill.A => EItemTier.A,
                TierSkill.S => EItemTier.S,
                TierSkill.SS => EItemTier.SS,
                _ => EItemTier.B,
            };
        }
    }
}