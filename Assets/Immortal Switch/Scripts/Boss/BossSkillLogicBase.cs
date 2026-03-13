using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    public abstract class BossSkillLogicBase : IBossSkillLogic
    {
        protected MonsterBossController boss;

        public virtual void Initialize(MonsterBossController boss)
        {
            this.boss = boss;
        }

        public virtual void OnBattleStart() { }
        public virtual void OnNormalAttack() { }
        public virtual void OnSkillCast() { }
        public virtual void OnHitTaken(float damageTaken) { }
        public virtual void OnHpChanged() { }
        protected float HpPercent => boss.CurrentHp / boss.MaxHp * 100f;
        protected bool RollChance(float percent)
        {
            return Random.Range(0f, 100f) <= percent;
        }

        protected void LogPassive(string msg)
        {
            Debug.Log($"Boss [{boss.hId}] Passive: {msg}");
        }

        protected void LogActive(string msg)
        {
            Debug.Log($"Boss [{boss.hId}] Active: {msg}");
        }
    }
}