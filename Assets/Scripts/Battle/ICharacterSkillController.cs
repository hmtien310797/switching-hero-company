using System;

namespace Battle
{
    public interface ICharacterSkillController
    {
        public void DoSkillByIdx(HeroSkills skillId, Action endAct);
        
    }
}