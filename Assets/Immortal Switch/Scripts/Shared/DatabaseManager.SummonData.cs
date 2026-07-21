using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public HeroSummonConfigSO HeroSummonConfig{get; private set;}
        
        [field: DatabaseBinding]
        public SkillSummonConfigSO SkillSummonConfig{get; private set;}
        
        [field: DatabaseBinding]
        public WeaponSummonConfigSO WeaponSummonConfig {get; private set;}
    }
}