using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace Battle
{
    public enum HeroSkills
    {
        Idle,
        Attack,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5,
        Run,
        Die,
        Injured,
        Win,
        Switch,
        Spawn,
        Flash,
        Skill6,
    }

    public class BaseSkillController : MonoBehaviour, ICharacterSkillController
    {
        [SerializeField] List<bool> IsMissingSkills = new List<bool>(5);
        [SerializeField] BaseAnimController baseAnimController;
        [SerializeField] SkeletonAnimation skaFx;
        [SerializeField] float moveAnimSpeed = 1f;

        public BaseAnimController BaseAnimController { get => baseAnimController; set => baseAnimController = value; }
        public SkeletonAnimation SkaFx { get => skaFx; set => skaFx = value; }

        private void Start()
        {
            if (skaFx && !skaFx.valid)
            {
                skaFx.Initialize(false);
            }
        }

        public void SetAnimMoveSpeed(float speed)
        {
            moveAnimSpeed = speed;
        }

        public virtual void InitSkill(List<int> bEscs, Transform soTrans)
        {

        }

        public virtual void ChangeSkillBySlot(int slotId, int skillId)
        {

        }

        public void HideFxAttack()
        {
            skaFx?.gameObject.SetActive(false);
        }

        public virtual void DoAttack(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Attack1);
        }

        public virtual void DoSkill01(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Idle);
        }

        public virtual void DoSkill02(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Idle);
        }

        public virtual void DoSkill03(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Idle);
        }

        public virtual void DoSkill04(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Idle);
        }

        public virtual void DoSkill05(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Idle);
        }

        public void DoRun(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Run, moveAnimSpeed);
            endAct?.Invoke();
        }

        public virtual void DoDeath(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Die, 1, false);
        }


        public void DoIdle(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Idle);
            endAct?.Invoke();
        }

        public virtual void DoSwitch(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Switch);
        }

        public virtual void DoWin(Action endAct)
        {
            baseAnimController?.PlayAmin(StandAnimName.Win);
        }

        public virtual void DoSpawn(Action endAct)
        {
            HideFxAttack();
            baseAnimController?.PlayAmin(StandAnimName.Spawn, 1, false);
        }

        public virtual void DoFlash(Action endAct)
        {

        }

        public virtual void DoSkillByIdx(HeroSkills skillIdx, Action endAct)
        {
            switch (skillIdx)
            {
                case HeroSkills.Idle:
                    DoIdle(endAct);
                    break;
                case HeroSkills.Run:
                    DoRun(endAct);
                    break;
                case HeroSkills.Die:
                    DoDeath(endAct);
                    break;
                case HeroSkills.Attack:
                    DoAttack(endAct);
                    break;
                case HeroSkills.Skill1:
                    DoSkill01(endAct);
                    break;
                case HeroSkills.Skill2:
                    DoSkill02(endAct);
                    break;
                case HeroSkills.Skill3:
                    DoSkill03(endAct);
                    break;
                case HeroSkills.Skill4:
                    DoSkill04(endAct);
                    break;
                case HeroSkills.Skill5:
                    DoSkill05(endAct);
                    break;
                case HeroSkills.Switch:
                    DoSwitch(endAct);
                    break;
                case HeroSkills.Win:
                    DoWin(endAct);
                    break;
                case HeroSkills.Spawn:
                    DoSpawn(endAct);
                    break;
                case HeroSkills.Flash:
                    DoFlash(endAct);
                    break;
            }
        }
    }
}