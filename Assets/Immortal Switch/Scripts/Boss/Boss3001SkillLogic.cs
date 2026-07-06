using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Boss
{
    public class Boss3001SkillLogic : BossSkillLogicBase
    {
        private readonly float[] thresholds = { 90f, 70f, 50f, 30f };
        private readonly bool[] triggered = new bool[4];

        public override void OnNormalAttack()
        {
            TryCastPassive();
        }

        public override void OnSkillCast()
        {
            TryCastPassive();
        }

        public override void OnHpChanged()
        {
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (triggered[i])
                    continue;

                if (HpPercent <= thresholds[i])
                {
                    triggered[i] = true;
                    CastActiveByPhase(i);
                }
            }
        }

        private void TryCastPassive()
        {
            if (!RollChance(30f))
                return;

            ICombatUnit target = boss.Target;

            if (target == null || target.IsDead)
                return;

            LogPassive("Hell Curse");

            boss.ApplyBuffToTarget(
                target,
                BossBuffFactory.CreateHellCurseDebuff()
            );
        }

        private void CastActiveByPhase(int phaseIndex)
        {
            LogActive($"Hỏa Ngục Bùng Cháy Phase {phaseIndex + 1}");

            boss.CastActiveSkillAnimation();

            float[] damageMultipliers = { 150f, 180f, 210f, 240f };

            boss.DealDamageToAllHeroTargets(
                damageMultipliers[phaseIndex]
            );

            // Debuff ATK/DEF phase sau có thể bổ sung bằng BossBuffFactory.
        }
    }
}