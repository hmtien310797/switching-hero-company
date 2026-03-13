using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Scripts.Battle
{
    [Serializable]
    public class BaseStat
    {
        public float Health;
        public float IdleStateTime;
        public float IdleIntervalTime;
        public float AttackRange;
        public float Defense;
        public float Attack;
        public float AttackSpeed;
        public float CritChance;
        public float CritDamage;
        public float Accuracy;
        public float MoveSpeed;
        public Element Element;
    }

    public class MonsterScrepController : BaseCharacterController<MonsterScrepController>, ICombatUnit
    {
        [SerializeField] bool isBoss = false;

        private List<PlayerHeroController> targets;
        private PlayerHeroController etarget;
        protected PvEBattleController pvEBattleController = null;

        public ScrepSpanwState SpawnState = new ScrepSpanwState();
        public ScrepIdleState IdleState = new ScrepIdleState();
        public ScrepMoveState MoveState = new ScrepMoveState();
        public ScrepAttackState AttackState = new ScrepAttackState();
        public ScrepDeathState DeathState = new ScrepDeathState();
        private Transform targetTrans = null;
        public int hId { get; private set; }

        public PlayerHeroController Target
        {
            get => etarget;
            set => etarget = value;
        }

        public virtual void InitMonster(int hid, PlayerHeroController etarget, PvEBattleController pBc,
            BaseStat monsterData, bool isBoss = false)
        {
            hId = hid;
            pvEBattleController = pBc;
            healthBarController?.PreSetHealth();
            SetTargetTrans(etarget);
            DoRotate(transform.position.x < etarget.transform.position.x);
            Stats.Initialize(monsterData);
            InitMonsterData();

            SwitchState(SpawnState);
        }

        private void SetTargetTrans(PlayerHeroController pHc)
        {
            etarget = pHc;
            targetTrans = etarget.FollowHeroController.GetNextPoint();
        }

        private void SetTarget()
        {
            if (targets == null || targets.Count == 0) return;

            var nearDist = float.MaxValue;
            PlayerHeroController target = null;
            foreach (var tg in targets)
            {
                var dist = (transform.position - tg.transform.position).sqrMagnitude;
                if (dist < nearDist)
                {
                    nearDist = dist;
                    target = tg;
                }
            }

            SetTargetTrans(target);
        }

        private void InitMonsterData()
        {
            baseStatData.IdleIntervalTime = 3f;
            baseStatData.IdleStateTime = baseStatData.IdleIntervalTime / 2;
            baseStatData.AttackRange = isBoss ? 4f : 2f;
        }

        public void DoIdleCallback()
        {
            if (baseStatData.IdleStateTime > 0)
            {
                baseStatData.IdleStateTime -= Time.deltaTime;
                return;
            }

            if (IsConditionValid())
            {
                if (IsInAttackRange(Stats.StatModule.GetBaseStat(StatType.AttackRange),
                        etarget.transform.position))
                {
                    SwitchState(AttackState);
                }
                else
                    SwitchState(MoveState);
            }
            else
                baseStatData.IdleStateTime = baseStatData.IdleIntervalTime;
        }

        public void DoMoveCallback()
        {
            DoMoveToTarget(targetTrans, 1.5f, offsetX: 1f);

            if (IsInAttackRange(Stats.StatModule.GetBaseStat(StatType.AttackRange), etarget.transform.position))
            {
                SwitchState(AttackState);
            }
        }

        private void ResetIdleTime()
        {
            baseStatData.IdleStateTime = baseStatData.IdleIntervalTime;
        }

        private void IdleTimeToZero()
        {
            baseStatData.IdleStateTime = 0;
        }

        public bool IsConditionValid()
        {
            return etarget != null;
        }

        public bool isLookRight()
        {
            return transform.eulerAngles.y == 0;
        }

        public void DoChangeState()
        {
            ResetIdleTime();

            if (IsDead)
            {
                SwitchState(DeathState);
                return;
            }

            SwitchState(IdleState);
        }

        public void DoEndSpawn()
        {
            IdleTimeToZero();
            if (IsDead)
            {
                SwitchState(DeathState);
                return;
            }

            SwitchState(IdleState);
        }

        public override void OnReceiveDamage(float damage, Action endAct, PlayerHeroController target)
        {
            if (CurrentHp <= 0)
            {
                SwitchState(DeathState);
                return;
            }

            if (!isBoss) etarget = target;

            healthBarController?.ShowHealthTxt(damage, transform.position + Vector3.up);
            Stats.HealthModule.ApplyDamage(damage);
            healthBarController?.SetHealth(CurrentHp / MaxHp);
            if (CurrentHp <= 0)
            {
                endAct?.Invoke();
                SwitchState(DeathState);
            }
        }

        public override void AttackBySpecific()
        {
            etarget?.OnReceiveDamage(baseStatData.Attack, null, this);
        }

        public void ResolveDeath()
        {
            pvEBattleController?.NotifyMonsterDeath(this);
        }

        public void CheckDead(Action endAct)
        {
            if (IsDead)
            {
                SwitchState(DeathState);
            }
            else
            {
                endAct?.Invoke();
            }
        }

        public bool IsBoss()
        {
            return isBoss;
        }

        public void IsLookRight()
        {
            DoRotate(transform.position.x < etarget.transform.position.x);
        }

        public void TakeDamage(float amount, DamageType damageType = DamageType.Normal)
        {
            throw new NotImplementedException();
        }

        public void Heal(float amount)
        {
            throw new NotImplementedException();
        }

        public override bool IsInAttackRange(float rangeAttack, Vector3 target)
        {
            var isValidX = Mathf.Pow(transform.position.x - target.x, 2) <= rangeAttack * rangeAttack;
            var isValidZ = Mathf.Pow(transform.position.z - target.z, 2) <= rangeAttack;

            return isValidX && isValidZ;
        }
    }

    public class ScrepSpanwState : ICharacterState<MonsterScrepController>
    {
        public void EndState(MonsterScrepController state)
        {
        }

        public void StartState(MonsterScrepController state)
        {
            state.DoIntoSpawn(() => state.DoEndSpawn());
        }

        public void UpdateState(MonsterScrepController state)
        {
        }
    }

    public class ScrepIdleState : ICharacterState<MonsterScrepController>
    {
        public void EndState(MonsterScrepController state)
        {
        }

        public void StartState(MonsterScrepController state)
        {
            state.CheckDead(() => state.DoIntoIdle());
        }

        public void UpdateState(MonsterScrepController state)
        {
            state.DoIdleCallback();
        }
    }

    public class ScrepMoveState : ICharacterState<MonsterScrepController>
    {
        public void EndState(MonsterScrepController state)
        {
        }

        public void StartState(MonsterScrepController state)
        {
            state.DoIntoMove();
        }

        public void UpdateState(MonsterScrepController state)
        {
            state.DoMoveCallback();
        }
    }

    public class ScrepAttackState : ICharacterState<MonsterScrepController>
    {
        public void EndState(MonsterScrepController state)
        {
        }

        public void StartState(MonsterScrepController state)
        {
            state.DoIntoAttack(() => state.DoChangeState());
        }

        public void UpdateState(MonsterScrepController state)
        {
        }
    }

    public class ScrepDeathState : ICharacterState<MonsterScrepController>
    {
        public void EndState(MonsterScrepController state)
        {
        }

        public void StartState(MonsterScrepController state)
        {
            state.DoIntoDeath(state.ResolveDeath);
        }

        public void UpdateState(MonsterScrepController state)
        {
        }
    }
}