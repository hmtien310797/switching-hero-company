using Battle;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    public class Boss3001SkillLogic : BossSkillLogicBase
    {
        private bool activeTriggered;
        private int phase;

        public override void Initialize(MonsterBossController boss)
        {
            base.Initialize(boss);
        }

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
            if (HpPercent <= 70f && phase < 1)
            {
                Debug.Log($"Current HP: {boss.CurrentHp} / Boss Max HP: {boss.MaxHp}");
                phase = 1;
                CastActive();
            }

            if (HpPercent <= 40f && phase < 2)
            {
                Debug.Log($"Current HP: {boss.CurrentHp} / Boss Max HP: {boss.MaxHp}");
                phase = 2;
                CastActive();
            }
        }

        private void TryCastPassive()
        {
            if (!RollChance(30f)) return;

            var target = boss.Target;
            if (target == null || target.IsDead) return;

            LogPassive("Hell Curse");
            //boss.ApplyBuffToTarget(target, BossBuffFactory.CreateHellCurseDebuff());
        }

        private void CastActive()
        {
            LogActive("Hỏa Ngục Bùng Cháy");
            
           // boss.DealDamageToTarget(boss.Target, 180f);
           // boss.ApplyBuffToTarget(boss.Target, BossBuffFactory.CreateAtkDown25_5s());
        }
    }
}