using System;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using Spine.Unity;
using UnityEngine;

namespace Battle
{
    public enum TierSkill
    {
        B,
        A,
        S,
        SS,
    }

    public class BaseExternalSkillController : MonoBehaviour
    {
        [SerializeField] bool isFollow;
        [SerializeField] TierSkill tierSkill;
        [SerializeField] float rangeSkill = 4;
        [SerializeField] float dameSkillFactor = 2.5f;
        [SerializeField] bool isAtkEvent = true;

        private const string animSkill = "animation";
        private const string enventHit = "hit";
        private const string enventFinalHit = "finalhit";
        private float skillDuration;
        
        private Action<float, float> atkAct;
        private Action endAct;

        private PlayerHeroController playerHeroController;
        private SkillDataSO skillData;

        public string AnimSkill => animSkill;

        public string EnventHit => enventHit;

        public TierSkill TierBaseSkill { get => tierSkill; set => tierSkill = value; }
        public float RangeSkill { get => rangeSkill; set => rangeSkill = value; }
        public float DameSkillFactor { get => dameSkillFactor; set => dameSkillFactor = value; }
        public Action<float, float> AtkAct { get => atkAct; set => atkAct = value; }
        public bool IsFollow { get => isFollow; set => isFollow = value; }
        public PlayerHeroController PlayerHeroController { get => playerHeroController; set => playerHeroController = value; }
        public bool IsAtkEvent { get => isAtkEvent; set => isAtkEvent = value; }
        public SkillDataSO SkillData { get => skillData; set => skillData = value; }

        public static string EnventFinalHit => enventFinalHit;
        protected const int defaultSkillLevel = 1;

        public virtual void InitInnerSkill(bool isInit, Action<float> camAct)
        {
        }

        public void InitSkill(PlayerHeroController pHc, SkillDataSO skillData, Action endAct, Action<float> camAct)
        {
            // if (skillData.SkillGroup == TierSkillGroup.FollowHero)
            // {
            //     var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, pHc.transform.position);
            //     
            //     //if(b)
            //     {
            //         isFollow = fxObj.isFollow;
            //         fxObj.SetHeroPlayerController(pHc);
            //         fxObj.skillData = skillData;
            //         fxObj.endAct = endAct;
            //     }
            //
            //     fxObj.InitInnerSkill(b, camAct);
            // }
            // else if(skillData.SkillGroup == TierSkillGroup.SingleShot)
            // {
            //     var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, pHc.transform.position);
            //     //if (b)
            //     {
            //         isFollow = fxObj.isFollow;
            //         fxObj.SetHeroPlayerController(pHc);
            //         fxObj.skillData = skillData;
            //         fxObj.endAct = endAct;
            //     }
            //
            //     fxObj.InitInnerSkill(b, camAct);
            // }
            // else if(skillData.SkillGroup == TierSkillGroup.MultiShot)
            // {
            //     DoS2SkillWithMultiSpawn(endAct, skillData, pHc, camAct);
            // }
        }

        

        public virtual void InitInnerSkillMultiSpawn(bool isFinal, Action<float>camAct) { }

        public virtual void SetHeroPlayerController(PlayerHeroController pHc)
        {
            playerHeroController = pHc;
        }

        public float GetAnimDur(SkeletonAnimation skaFx)
        {
            if (skillDuration <= 0)
            {
                skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            }

            return skillDuration;
        }

        public virtual void RegisterAnimEvent(Action<float, float> eventAct)
        {
            
        }

        public void PlayAnim(SkeletonAnimation skaFx, float speed = 1, bool isLoop = false)
        {
            skaFx.AnimationState.TimeScale = speed;
            skaFx.AnimationState.SetAnimation(0, animSkill, false);
        }

        public virtual async UniTaskVoid DoEndSkill()
        {
            endAct?.Invoke();
        }

        public async UniTaskVoid DoSkill(Func<float, CancellationToken, UniTask> heroAct, Action endAct, Transform targetTrans, CancellationToken token)
        {
            
        }

        public async UniTaskVoid DoSkill(Func<Vector3, float, CancellationToken, UniTask> heroAct, Action endAct, Vector3 targetPos, CancellationToken token)
        {
            
        }

        public async UniTaskVoid DoSkill(Vector3 targetPos, Func<Vector3,float, CancellationToken, Action<Vector3>, UniTask> heroAct, Action endAct, CancellationToken token)
        {
            
        }


        public void SetFxState(bool isShow)
        {
            gameObject.SetActive(isShow);
        }
    }
}
