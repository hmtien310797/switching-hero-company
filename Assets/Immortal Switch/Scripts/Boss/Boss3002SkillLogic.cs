namespace Immortal_Switch.Scripts.Boss
{
    public class Boss3002SkillLogic : BossSkillLogicBase
    {
        private bool passive60Triggered;
        private bool passive30Triggered;

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
            if (!passive60Triggered && HpPercent <= 60f)
            {
                passive60Triggered = true;
                CastPassive60();
            }

            if (!passive30Triggered && HpPercent <= 30f)
            {
                passive30Triggered = true;
                CastPassive30();
            }
        }

        private void CastPassive60()
        {
            LogPassive("Da Dung Nham 60%");
            boss.ApplyBuffToSelf(BossBuffFactory.CreateDamageReduction40_10s());
        }

        private void CastPassive30()
        {
            LogPassive("Da Dung Nham 30%");
            boss.ApplyBuffToSelf(BossBuffFactory.CreateDamageReduction40_10s());
        }

        private void CastActive()
        {
            LogActive("Nghiền Nát Dung Nham");

            boss.CastActiveSkillAnimation();
            boss.DealDamageToAllHeroTargets(200f);

            // TODO: Apply ATK Speed down cho all hero.
        }
    }
}