using Cysharp.Threading.Tasks;
using Scripts.Common;
using Spine.Unity;
using System;
using System.Threading;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Scripts.Battle
{
    public enum TierSkill
    {
        A,
        SS,
        SSR,
        SSS
    }

    public enum TierSkillGroup
    {
        FollowHero,
        MultiShot,
        SingleShot,
    }

    public class BaseExternalSkillController : MonoBehaviour
    {
        [SerializeField] bool isFollow;
        [SerializeField] TierSkill tierSkill;
        [SerializeField] float rangeSkill = 4;
        [SerializeField] float dameSkillFactor = 2.5f;
        [SerializeField] bool isAtkEvent = true;
        [SerializeField] protected SkeletonAnimation skaFx;

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

        public virtual void InitInnerSkill(bool isInit, Action<float> camAct)
        {
            InitSkeletonAnimation();
        }
        
        public void InitSkill(PlayerHeroController pHc, SkillDataSO skillData, Action endAct, Action<float> camAct)
        {
            if(skillData.SkillGroup == TierSkillGroup.MultiShot)
            {
                DoSkillWithMultiSpawnAsync(endAct, skillData, pHc, camAct).Forget();
                return;
            }
            
            var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, pHc.transform.position);
            isFollow = fxObj.isFollow;
            fxObj.SetHeroPlayerController(pHc);
            fxObj.skillData = skillData;
            fxObj.endAct = endAct;

            fxObj.InitInnerSkill(b, camAct);
        }
        
        private async UniTask DoSkillWithMultiSpawnAsync(Action endAct, SkillDataSO skillData, PlayerHeroController pHc, Action<float>camAct)
        {
            var pos = pHc.GetNearestMonster();
            for (int i = 0; i < skillData.NumSpawn; i++)
            {
                var nPos = pos + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
                var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, nPos);
                isFollow = fxObj.isFollow;
                fxObj.SetHeroPlayerController(pHc);
                fxObj.skillData = skillData;
                fxObj.endAct = i == skillData.NumSpawn - 1 ? endAct : null;
                fxObj.InitInnerSkillMultiSpawn(i == skillData.NumSpawn - 1, i == 0 ? camAct : null);

                await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        protected virtual void InitInnerSkillMultiSpawn(bool isFinal, Action<float> camAct)
        {
            InitSkeletonAnimation();
        }

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

        protected virtual void InitSkeletonAnimation()
        {
            if (skaFx && !skaFx.valid)
            {
                skaFx.Initialize(false);
            }

            GetAnimDur(skaFx);
        }
    }
}
