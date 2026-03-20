using System;

namespace Scripts.Battle
{
    public interface ICharacterSkillController
    {
        public void DoSkillByIdx(HeroSkills skillId, Action endAct);
    }
}