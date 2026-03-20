using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    public class EmptyBossSkillLogic : BossSkillLogicBase
    {
        public override void Initialize(MonsterBossController boss)
        {
            base.Initialize(boss);
            Debug.LogWarning($"EmptyBossSkillLogic is being used for Boss ID");
        }
    }
}