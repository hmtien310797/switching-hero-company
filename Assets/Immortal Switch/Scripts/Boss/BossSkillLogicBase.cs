using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    public abstract class BossSkillLogicBase : IBossSkillLogic
    {
        protected BossActor boss;

        public virtual void Initialize(BossActor boss)
        {
            this.boss = boss;
        }

        public virtual void OnBattleStart() { }

        public virtual void OnNormalAttack() { }

        public virtual void OnSkillCast() { }

        public virtual void OnHitTaken(float damageTaken) { }

        public virtual void OnHpChanged() { }

        protected float HpPercent
        {
            get
            {
                if (boss == null || boss.MaxHp <= 0f)
                    return 0f;

                return boss.CurrentHp / boss.MaxHp * 100f;
            }
        }

        protected bool RollChance(float percent)
        {
            return Random.Range(0f, 100f) <= percent;
        }

        protected void LogPassive(string msg)
        {
            Debug.Log($"Boss [{boss.BossId}] Passive: {msg}");
        }

        protected void LogActive(string msg)
        {
            Debug.Log($"Boss [{boss.BossId}] Active: {msg}");
        }
    }
}