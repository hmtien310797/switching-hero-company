using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Boss
{
    public class Boss3002SkillLogic : BossSkillLogicBase
    {
        private bool passiveTriggered;

        public override void OnNormalAttack()
        {
            if (boss.NormalAttackCount >= 5)
            {
                boss.ResetNormalAttackCount();
                CastActive();
            }
        }

        public override void OnHpChanged()
        {
            if (passiveTriggered) return;

            if (HpPercent <= 60f)
            {
                passiveTriggered = true;
                CastPassive();
            }
        }

        private void CastPassive()
        {
            LogPassive("Da Dung Nham");
            //boss.ApplyBuffToSelf(BossBuffFactory.CreateDamageReduction40_10s());
        }

        private void CastActive()
        {
            LogActive("Nghiền Nát Dung Nham");

            // List<ICombatUnit> targets = BossCombatRegistry.GetEnemyTargets(boss);
            // boss.DealDamageToAllTargets(targets, 200f);
            //
            // for (int i = 0; i < targets.Count; i++)
            // {
            //     boss.ApplyBuffToTarget(targets[i], BossBuffFactory.CreateAttackSpeedDown20_5s());
            // }
        }
    }
}