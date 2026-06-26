using Common;

namespace Immortal_Switch.Scripts.Skill
{
    public interface ISkillLevelProvider
    {
        int GetSkillLevel(SkillDataSO skillData, HeroActor owner);
    }

    public sealed class DefaultSkillLevelProvider : ISkillLevelProvider
    {

        public int GetSkillLevel(SkillDataSO skillData, HeroActor owner)
        {
            return skillData != null ? skillData.GetSafeLevel(UserDataCache.Instance.GetServerSkillLevel(skillData.SkillId)) : 1;
        }
    }
}
