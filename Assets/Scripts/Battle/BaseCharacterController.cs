using System;
using UnityEngine;

namespace Scripts.Battle
{
    public enum HeroType
    {
        Player,
        Screp,
        Boss,
    }

    public class BaseCharacterController<T> : MonoBehaviour where T : BaseCharacterController<T>
    {
        [SerializeField] BaseSkillController baseSkillController;
        [SerializeField] HeroType heroType;
        public HealthBarController healthBarController;

        private ICharacterSkillController characterSkillController;
        public ICharacterState<T> currentState = null;


        private void Awake()
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

        public void DoMoveToTarget(Transform target, float speed = 3f, float offsetX = 0)
        {
            var isRight = transform.position.x < target.position.x;
            DoRotate(isRight);
            
            var newPos = target.position - (isRight ? Vector3.right : Vector3.left) * offsetX;
            transform.position = Vector3.MoveTowards(transform.position, newPos, Time.deltaTime * speed);
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

        public virtual void OnReceiveDamage(float damage, Action endAct)
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

        public bool IsInAttackRange(float rangeAttack, Vector3 target, float offsetX = .25f, float offsetZ = .5f)
        {
            bool isInRangeX = Mathf.Abs(transform.position.x - target.x) < rangeAttack - offsetX;
            bool isInRangeZ = Mathf.Abs(transform.position.z - target.z) < offsetZ;

            return isInRangeX && isInRangeZ;
        }
    }
}
