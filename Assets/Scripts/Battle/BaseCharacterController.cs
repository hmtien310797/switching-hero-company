using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;
using NaughtyAttributes;
using UnityEngine;

namespace Scripts.Battle
{
    public enum HeroType
    {
        Player,
        Screp,
        Boss,
    }

    public class BaseCharacterController<T> : MonoBehaviour where T : BaseCharacterController<T>, ICombatUnit
    {
        [SerializeField] BaseSkillController baseSkillController;
        [SerializeField] HeroType heroType;
        [SerializeField] StatsController statsController;
        public HealthBarController healthBarController;
        [ReadOnly]
        public BaseStat baseStatData = new BaseStat();

        private ICharacterSkillController characterSkillController;
        public ICharacterState<T> currentState = null;
        public StatsController Stats => statsController;
        
        #region Properties
        public float CurrentHp => statsController.HealthModule?.CurrentHP ?? 0f;
        public float MaxHp => statsController.HealthModule?.MaxHP ?? 0f;
        public bool IsDead => statsController == null || statsController.HealthModule == null ||
                              statsController.HealthModule.IsDead;
        public float CurrentMoveSpeed => statsController.StatModule.GetFinalStat(StatType.MoveSpeed);
        public float CurrentDefense => statsController.StatModule.GetFinalStat(StatType.Def);
        public float CurrentAttackSpeed => statsController.StatModule.GetFinalStat(StatType.Atk);
        #endregion

        public void InitSkill(List<int> skills, Transform obTrans)
        {
            baseSkillController.InitSkill(skills, obTrans);
        }

        public void ChangeSkillBySlot(int slotId, int skillID)
        {
            baseSkillController.ChangeSkillBySlot(slotId, skillID);
        }

        public void SetAnimMoveSpeed(float speed)
        {
            baseSkillController?.SetAnimMoveSpeed(speed);
        }

        protected virtual void Awake()
        {
            SetCharacterSkill();
        }

        private void SetCharacterSkill()
        {
            characterSkillController = baseSkillController;
        }

        public virtual void Update()
        {
            if (currentState != null)
            {
                currentState.UpdateState((T)this);
            }
        }

        public void SwitchState(ICharacterState<T> state)
        {
            if (state == null || currentState == state) return;

            if (currentState != null)
            {
                currentState.EndState((T)this);
            }

            currentState = state;
            currentState.StartState((T)this);
        }

        public virtual void DoIntoAttack(Action endAct) 
        {
            /*if (heroType == HeroType.Screp)
            {
                characterSkillController.DoSkillByIdx(HeroSkills.Attack, endAct);
                return;
            }*/
            
            characterSkillController.DoSkillByIdx(HeroSkills.Attack, endAct);
        }

        public virtual void DoIntoSkill(HeroSkills skillIdx, Action endAct)
        {
            characterSkillController.DoSkillByIdx(skillIdx, endAct);
        }

        public virtual void DoIntoSpawn(Action endAct)
        {
            characterSkillController.DoSkillByIdx(HeroSkills.Spawn, endAct);
        }

        public virtual void DoIntoIdle()
        {
            characterSkillController.DoSkillByIdx(HeroSkills.Idle, null);
        }

        public virtual void DoIntoMove()
        {
            characterSkillController.DoSkillByIdx(HeroSkills.Run, null);
        }

        public virtual void DoIntoFlash(Action endAct)
        {
            characterSkillController.DoSkillByIdx(HeroSkills.Flash, endAct);
        }

        public void DoMoveToTarget(Transform target, float speed = 3f, float offsetX = 0)
        {
            var isRight = transform.position.x < target.position.x;
            DoRotate(isRight);

            //var newPos = target.position - (isRight ? Vector3.right : Vector3.left) * offsetX;
            //transform.position = Vector3.MoveTowards(transform.position, newPos, Time.deltaTime * speed);
            var nPos = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * speed);
            transform.position = nPos;
        }

        public virtual void DoIntoInjured()
        {

        }

        public virtual void DoIntoDeath(Action endAct)
        {
            baseSkillController?.DoSkillByIdx(HeroSkills.Die, endAct);
        }

        public virtual void DoIntoWin(Action endAct)
        {
            baseSkillController?.DoSkillByIdx(HeroSkills.Win, endAct);
        }

        public virtual void OnReceiveDamage(float factorSkillDamage, Action endAct, PlayerHeroController target)
        {
            
        }

        public virtual void AttackBySpecific()
        {

        }

        public void DoRotate(bool isRight)
        {
            if(isRight) 
                transform.eulerAngles = Vector3.zero;
            else
                transform.eulerAngles = Vector3.up*180;
        }

        public virtual bool IsInAttackRange(float rangeAttack, Vector3 target)
        {
            return (transform.position - target).sqrMagnitude <= rangeAttack*rangeAttack;
        }

        public void TakeDamage(ICombatUnit attacker, float amount = 1)
        {
            DamageResult damageResult = DamageCalculator.CalculateDamage(attacker, (ICombatUnit)this, amount);
            healthBarController?.ShowHealthTxt((int)damageResult.Damage, transform.position + Vector3.up);
            statsController.HealthModule.TakeDamage(damageResult.Damage, damageResult.DamageTextType);
            healthBarController?.SetHealth(CurrentHp / MaxHp);
        }

        public void Heal(float amount)
        {
            statsController.HealthModule.ApplyHeal(amount);
        }
    }
}
