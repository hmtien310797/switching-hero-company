using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    public class Boss3003SkillLogic : BossSkillLogicBase
    {
        private float lastPassiveTime = -999f;

        public override void OnNormalAttack()
        {
            TryCastPassive();
            TryCastActive();
        }

        public override void OnSkillCast()
        {
            TryCastPassive();
        }

        private void TryCastPassive()
        {
            if (Time.time < lastPassiveTime + 2f) return;
            if (!RollChance(30f)) return;
            
            //ICombatUnit randomTarget = boss.Target;
            //if (randomTarget == null) return;

            lastPassiveTime = Time.time;

            LogPassive("Lõi Bão Tố");
        }

        private void TryCastActive()
        {
            if (boss.NormalAttackCount < 7) return;

            boss.ResetNormalAttackCount();
            CastActive();
        }

        private void CastActive()
        {
            LogActive("Lôi Vân Giáng Thế");

            // for (int i = 0; i < 5; i++)
            // {
            //     ICombatUnit randomTarget = boss.Target;
            //     if (randomTarget == null) continue;
            //
            //     boss.DealDamageToTarget(randomTarget, 150f);
            //
            //     if (RollChance(25f))
            //     {
            //         boss.ApplyBuffToTarget(randomTarget, BossBuffFactory.CreateStun_1_5s());
            //     }
            // }
        }
    }
}