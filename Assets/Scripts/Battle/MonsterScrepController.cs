using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class ScrepData
    {
        public int Hid;
        public float Health;
        public float RemainHealth;
        public float IdleStateTime;
        public float IdleIntervalTime;
        public float RangeAttack;
    }

    public class MonsterScrepController : BaseCharacterController<MonsterScrepController>
    {
        [SerializeField] bool isBoss = false;

        private PlayerHeroController target;
        private PvEBattleController pvEBattleController = null;
        private int hId;

        public ScrepData MonsterData = new ScrepData();

        public ScrepSpanwState SpawnState = new ScrepSpanwState();
        public ScrepIdleState IdleState = new ScrepIdleState();
        public ScrepMoveState MoveState = new ScrepMoveState();
        public ScrepAttackState AttackState = new ScrepAttackState();
        public ScrepDeathState DeathState = new ScrepDeathState();
        public WinState WinState = new WinState();

        public PlayerHeroController Target { get => target; set => target = value; }

        public virtual void InitMonster(int hid, PlayerHeroController etarget, PvEBattleController pBc, bool isBoss = false)
        {
            hId = hid;
            target = etarget;
            pvEBattleController = pBc;
            healthBarController?.PreSetHealth();
            DoRotate(transform.position.x < target.transform.position.x);
            if(!isBoss) InitMonsterData();
            SwitchState(SpawnState);
        }

        private void InitMonsterData()
        {
            MonsterData.Health = 100;
            MonsterData.RemainHealth = MonsterData.Health;
            MonsterData.RangeAttack = 2f;
            MonsterData.IdleIntervalTime = 3f;
            MonsterData.IdleStateTime = MonsterData.IdleIntervalTime/2;
        }

        public override void Update()
        {
            base.Update();
        }

        public void DoIdleCallback()
        {
            if (MonsterData.IdleStateTime > 0)
            {
                MonsterData.IdleStateTime -= Time.deltaTime;
                return;
            }

            if (IsConditionValid())
            {
                if(IsInAttackRange(MonsterData.RangeAttack, target.transform.position))
                {
                    SwitchState(AttackState);
                }
                else
                    SwitchState(MoveState);
            }
            else
                MonsterData.IdleStateTime = MonsterData.IdleIntervalTime;
        }

        public void DoMoveCallback()
        {
            DoMoveToTarget(target.transform, offsetX:1f);

            if(IsInAttackRange(MonsterData.RangeAttack, target.transform.position))
            {
                SwitchState(AttackState);
            }
        }

        private void ResetIdleTime()
        {
            MonsterData.IdleStateTime = MonsterData.IdleIntervalTime;
        }

        private void IdleTimeToZero()
        {
            MonsterData.IdleStateTime = 0;
        }

        public bool IsConditionValid()
        {
            return target != null;
        }

        public bool isLookRight()
        {
            return transform.eulerAngles.y == 0;
        }

        public void DoChangeState()
        {
            ResetIdleTime();
            
            if(MonsterData.RemainHealth <= 0)
            {
                SwitchState(DeathState);
                return;
            }
            SwitchState(IdleState);
        }

        public void DoEndSpawn()
        {
            IdleTimeToZero();
            if (MonsterData.RemainHealth <= 0)
            {
                SwitchState(DeathState);
                return;
            }
            SwitchState(IdleState);
        }
        
        public override void OnReceiveDamage(float damage, Action endAct)
        {
            if (MonsterData.RemainHealth <= 0)
            {
                SwitchState(DeathState);
                return;
            }

            healthBarController?.ShowHealthTxt(damage, transform.position + Vector3.up);
            MonsterData.RemainHealth = Mathf.Max(0, MonsterData.RemainHealth - damage);
            healthBarController?.SetHealth((float)MonsterData.RemainHealth / MonsterData.Health);
            if (MonsterData.RemainHealth <= 0)
            {
                endAct?.Invoke();
                SwitchState(DeathState);

                return;
            }
        }

        public override void AttackBySpecific()
        {
            target?.OnReceiveDamage(10, null);
        }

        public void ResolveDeath()
        {
            pvEBattleController?.NotifyMonsterDeath(this);
        }

        public void CheckDead(Action endAct)
        {
            if(MonsterData.RemainHealth <= 0)
            {
                SwitchState(DeathState);
            }
            else
            {
                endAct?.Invoke();
            }
        }

        public bool IsDead()
        {
            return MonsterData.RemainHealth <= 0;
        }

        public bool IsBoss()
        {
            return isBoss;
        }

        public void IsLookRight()
        {
            DoRotate(transform.position.x < target.transform.position.x);
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
            state.CheckDead(()=>state.DoIntoIdle());
            //state.DoIntoIdle();
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
