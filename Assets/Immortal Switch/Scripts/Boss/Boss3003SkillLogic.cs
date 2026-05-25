using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;

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
            if (Time.time < lastPassiveTime + 2f)
                return;

            if (!RollChance(30f))
                return;

            ICombatUnit target = boss.Target;

            if (target == null || target.IsDead)
                return;

            lastPassiveTime = Time.time;

            LogPassive("Lõi Bão Tố");

            boss.DealDamageToTarget(target, 90f);
        }

        private void TryCastActive()
        {
            if (boss.NormalAttackCount < 7)
                return;

            boss.ResetNormalAttackCount();
            CastActive();
        }

        private void CastActive()
        {
            LogActive("Lôi Vân Giáng Thế");

            boss.CastActiveSkillAnimation();

            for (int i = 0; i < 5; i++)
            {
                ICombatUnit target = boss.Target;

                if (target == null || target.IsDead)
                    continue;

                boss.DealDamageToTarget(target, 150f);

                if (RollChance(25f))
                {
                    boss.ApplyBuffToTarget(
                        target,
                        BossBuffFactory.CreateStun_1_5s()
                    );
                }
            }
        }
    }
}