using System;
using System.Collections.Generic;
using Common;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    public class MonsterBossController : MonsterScrepController
    {
        private IBossSkillLogic skillLogic;
        private int normalAttackCount;

        protected override void Awake()
        {
            base.Awake();
            //.Subscribe(GameEvents.OnStageLost, OnDead);
        }

        public override void InitMonster(int hid, PlayerHeroController etarget, PvEBattleController pBc, BaseStat BossData, bool isBoss = true, List<PlayerHeroController> eTargets = null)
        {
            base.InitMonster(hid, etarget, pBc, BossData, isBoss, eTargets);
            normalAttackCount = 0;
            skillLogic = BossSkillLogicFactory.Create(hid);
            //skillLogic?.Initialize(this);
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

        public override void OnReceiveDamage(float factorSkillDamage, Action endAct, PlayerHeroController target)
        {
            base.OnReceiveDamage(factorSkillDamage, endAct, target);
            if (CurrentHp <= 0)
            {
                if (pvEBattleController.State == BattleState.Ended)
                {
                    return;
                }
                GameEventManager.Trigger(GameEvents.OnStageCleared);
                return;
            }
            skillLogic?.OnHitTaken(factorSkillDamage);
            skillLogic?.OnHpChanged();
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
        
        public void DealDamageToTarget(ICombatUnit target, float percentAtk)
        {
            if (target == null) return;
            if (target.IsDead) return;

            float atk = Stats.StatModule.GetFinalStat(StatType.Atk);
            float damage = atk * percentAtk / 100f;

            //target.TakeDamage(this);

            Debug.Log($"boss deals {damage} damage ({percentAtk}% ATK) to target");
        }

        private void OnDead()
        {
            //PoolController.Instance.ReturnToPool(gameObject);
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