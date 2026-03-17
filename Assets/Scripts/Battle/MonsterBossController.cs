using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;
using Scripts.Common;
using UnityEngine;

namespace Scripts.Battle
{
    public class MonsterBossController : MonsterScrepController
    {
        private IBossSkillLogic skillLogic;
        private int normalAttackCount;

        protected override void Awake()
        {
            base.Awake();
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnDead);
        }

        public override void InitMonster(int hid, PlayerHeroController etargets, PvEBattleController pBc,
            BaseStat BossData, bool isBoss = true)
        {
            base.InitMonster(hid, etargets, pBc, BossData, isBoss);
            normalAttackCount = 0;
            skillLogic = BossSkillLogicFactory.Create(hid);
            skillLogic?.Initialize(this);
            skillLogic?.OnBattleStart();
        }

        public override void DoIntoAttack(Action endAct)
        {
            base.DoIntoAttack(endAct);
            PerformNormalAttack();
        }

        public void TakeDamage(float amount, DamageType damageType = DamageType.Normal)
        {
            throw new System.NotImplementedException();
        }

        public int NormalAttackCount => normalAttackCount;

        private void PerformNormalAttack()
        {
            normalAttackCount++;
            skillLogic?.OnNormalAttack();
        }

        public void PerformSkillCast()
        {
            skillLogic?.OnSkillCast();
        }

        public override void OnReceiveDamage(float damage, Action endAct, PlayerHeroController target)
        { ;
            base.OnReceiveDamage(damage, endAct, target);
            if (CurrentHp <= 0)
            {
                if (pvEBattleController.State == BattleState.Ended)
                {
                    return;
                }
                GameEventManager.Trigger(GameEvents.OnStageCleared);
                return;
            }
            skillLogic?.OnHitTaken(damage);
            skillLogic?.OnHpChanged();
        }

        public void Heal(float value)
        {
            if (value <= 0) return;

            float oldHp = CurrentHp;

            Stats.HealthModule.ApplyHeal(value);

            Debug.Log($"boss heals {value}. HP: {oldHp} -> {CurrentHp}");
        }

        public void ResetNormalAttackCount()
        {
            normalAttackCount = 0;
        }

        public void DealDamageToCurrentTarget(float multiplierPercent)
        {
            float damage = baseStatData.Attack * multiplierPercent / 100f;
            Debug.Log($"deals {damage} damage to current target ({multiplierPercent}% ATK).");
        }

        public void DealDamageToAllEnemies(float multiplierPercent)
        {
            float damage = baseStatData.Attack * multiplierPercent / 100f;
            Debug.Log($"boss  deals {damage} AOE damage to all enemies ({multiplierPercent}% ATK).");
        }

        public void ApplyDebuffToCurrentTarget(string debuffName, float valuePercent, float duration)
        {
            Debug.Log(
                $"boss applies debuff [{debuffName}] {valuePercent}% for {duration}s to current target.");
        }

        public void ApplyDebuffToAllEnemies(string debuffName, float valuePercent, float duration)
        {
            Debug.Log(
                $"boss applies debuff [{debuffName}] {valuePercent}% for {duration}s to all enemies.");
        }

        public void ApplyBuffToSelf(string buffName, float valuePercent, float duration)
        {
            Debug.Log($"boss gains buff [{buffName}] {valuePercent}% for {duration}s.");
        }

        public void AddShieldToSelf(float value, float duration)
        {
            Debug.Log($"boss gains shield {value} for {duration}s.");
        }

        public void TeleportToLowestHpTarget()
        {
            Debug.Log($"boss teleports to the lowest HP target.");
        }

        public void StrikeRandomTarget(float multiplierPercent)
        {
            float damage = baseStatData.Attack * multiplierPercent / 100f;
            Debug.Log($"boss strikes random target for {damage} damage ({multiplierPercent}% ATK).");
        }

        public void StrikeHighestHpTarget(float multiplierPercent)
        {
            float damage = baseStatData.Attack * multiplierPercent / 100f;
            Debug.Log($"boss strikes highest HP target for {damage} damage ({multiplierPercent}% ATK).");
        }
        
        public void ApplyBuffToTarget(ICombatUnit target, BuffData buffData)
        {
            if (target == null) return;
            if (target.IsDead) return;

            target.Stats.BuffModule.ApplyBuff(buffData);
        }
        
        public void DealDamageToAllTargets(List<ICombatUnit> targets, float percentAtk)
        {
            if (targets == null || targets.Count == 0) return;

            float atk = Stats.StatModule.GetFinalStat(StatType.ATK);
            float damage = atk * percentAtk / 100f;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead) continue;

                target.TakeDamage(this);
            }

            Debug.Log($"boss deals {damage} AOE damage ({percentAtk}% ATK) to {targets.Count} targets");
        }
        
        public void DealDamageToTarget(ICombatUnit target, float percentAtk)
        {
            if (target == null) return;
            if (target.IsDead) return;

            float atk = Stats.StatModule.GetFinalStat(StatType.ATK);
            float damage = atk * percentAtk / 100f;

            target.TakeDamage(this);

            Debug.Log($"boss deals {damage} damage ({percentAtk}% ATK) to target");
        }

        private void OnDead()
        {
            PoolController.Instance.ReturnToPool(gameObject);
        }
    }
    
    public class BossPassiveSkillState : ICharacterState<MonsterBossController>
    {
        public void EndState(MonsterBossController boss)
        {
        }

        public void StartState(MonsterBossController boss)
        {
            boss.DoIntoMove();
        }

        public void UpdateState(MonsterBossController boss)
        {
            boss.DoMoveCallback();
        }
    }
    
    public class BossActiveSkillState : ICharacterState<MonsterBossController>
    {
        public void EndState(MonsterBossController boss)
        {
        }

        public void StartState(MonsterBossController boss)
        {
            boss.DoIntoMove();
        }

        public void UpdateState(MonsterBossController boss)
        {
            boss.DoMoveCallback();
        }
    }
}