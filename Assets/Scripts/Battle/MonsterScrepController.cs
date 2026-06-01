using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;
using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Battle
{

    public class MonsterScrepController : BaseCharacterController<MonsterScrepController>
    {
        [SerializeField] bool isBoss = false;

        private bool isReady = false;

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
        public bool IsReady { get => isReady; set => isReady = value; }

        public virtual void InitMonster(int hid, PlayerHeroController etarget, PvEBattleController pBc, BaseStat monsterData, bool isBoss = false, List<PlayerHeroController> eEargets = null)
        {
            isReady = false;
            hId = hid;
            pvEBattleController = pBc;
            //healthBarController?.rese();
            targets = eEargets;
            SetTargetTrans(etarget);
            DoRotate(transform.position.x < etarget.transform.position.x);
            Stats.Initialize(monsterData);
            InitMonsterData();
            SwitchState(SpawnState);
            SetAnimMoveSpeed(UnityEngine.Random.Range(0.85f, 1f));
        }

        private void SetTargetTrans(PlayerHeroController pHc)
        {
            etarget = pHc;
            if (etarget == null) return;

            targetTrans = etarget.FollowHeroController.GetNextPoint();
        }

        private void SetTarget()
        {
            if (targets == null || targets.Count == 0) return;

            var nearDist = float.MaxValue;
            PlayerHeroController target = null;
            foreach (var tg in targets)
            {
                if (!tg.IsValid()) continue;

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
            SetTarget();

            MoveToTarget();

            if (etarget == null) return;

            if (IsInAttackRange(Stats.StatModule.GetBaseStat(StatType.AttackRange), etarget.transform.position))
            {
                SwitchState(AttackState);
            }
        }

        private void MoveToTarget()
        {
            if(etarget == null) return;

            var isRight = transform.position.x < etarget.transform.position.x;
            if (!IsBoss())
            {
                var nPos = GetNextPos();
                DoRotate(isRight);
                transform.position = nPos;
            }
            else
                DoMoveToTarget(etarget.transform, 3);
        }

        //avoidance
        private Vector3 GetNextPos()
        {
            // var offsetSpeed = 0.6f;
            // Vector3 targetPos = Vector3.MoveTowards(transform.position, etarget.transform.position, Time.deltaTime * offsetSpeed);
            // var monsterArounds = pvEBattleController.GetNearestEnemiesInRange(targetPos, 1f);
            // if (monsterArounds == null || monsterArounds.Count <= 1) return targetPos;
            //
            // Vector3 avoidanceDir = Vector3.zero;
            // int count = 0;
            //
            // foreach (var monster in monsterArounds)
            // {
            //     if (monster.gameObject == this.gameObject) continue;
            //     Vector3 pushDir = targetPos - monster.transform.position;
            //     float distance = pushDir.magnitude;
            //     if (distance < .35f) continue;
            //
            //     avoidanceDir += pushDir.normalized / distance;
            //     count++;
            // }
            //
            // if (count > 0)
            // {
            //     targetPos += (avoidanceDir / count) * Time.deltaTime * offsetSpeed;
            // }

            return Vector3.zero;
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
        

        public override void OnReceiveDamage(float factorSkillDamage, Action endAct, PlayerHeroController target)
        {
            if (CurrentHp <= 0)
            {
                SwitchState(DeathState);
                return;
            }

            if (!isBoss) etarget = target;
            //TakeDamage(etarget, factorSkillDamage);
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
            // isReady = false;
            // pvEBattleController?.NotifyMonsterDeath(this);
            //
            // if(isBoss) GameEventManager.Trigger(GameEvents.OnStageCleared);
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
            //state.DoIntoSpawn(() => state.DoEndSpawn());
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