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
        B,
        A,
        S,
        SS,
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
            if (skillData.SkillGroup == TierSkillGroup.FollowHero)
            {
                var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, pHc.transform.position);
                
                if(b)
                {
                    isFollow = fxObj.isFollow;
                    fxObj.SetHeroPlayerController(pHc);
                    fxObj.skillData = skillData;
                    fxObj.endAct = endAct;
                }

                fxObj.InitInnerSkill(b, camAct);
            }
            else if(skillData.SkillGroup == TierSkillGroup.SingleShot)
            {
                var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, pHc.transform.position);
                if (b)
                {
                    isFollow = fxObj.isFollow;
                    fxObj.SetHeroPlayerController(pHc);
                    fxObj.skillData = skillData;
                    fxObj.endAct = endAct;
                }

                fxObj.InitInnerSkill(b, camAct);
            }
            else if(skillData.SkillGroup == TierSkillGroup.MultiShot)
            {
                DoS2SkillWithMultiSpawn(endAct, skillData, pHc, camAct);
            }
        }

        public async void DoS2SkillWithMultiSpawn(Action endAct, SkillDataSO skillData, PlayerHeroController pHc, Action<float>camAct)
        {
            var pos = pHc.GetNearestMonster();
            if (skillData.Tier == TierSkill.SS)
            {
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
            // else if (skillData.Tier == TierSkill.SSR)
            // {
            //     for (int i = 0; i < skillData.NumSpawn; i++)
            //     {
            //         var nPos = pos + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f)) + Vector3.up * 8;
            //         var (fxObj, b) = PoolController.Instance.Get(skillData.SkillPrefab, nPos);
            //         fxObj.transform.rotation = Quaternion.FromToRotation(Vector3.right, Vector3.down);
            //         isFollow = fxObj.isFollow;
            //         fxObj.SetHeroPlayerController(pHc);
            //         fxObj.skillData = skillData;
            //         fxObj.endAct = i == skillData.NumSpawn - 1 ? endAct : null;
            //         fxObj.InitInnerSkillMultiSpawn(i == skillData.NumSpawn - 1, i == 0 ? camAct : null);
            //
            //         await UniTask.Delay(TimeSpan.FromSeconds(0.075f), cancellationToken: this.GetCancellationTokenOnDestroy());
            //     }
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
            /*if (isFollow)
            {
                transform.SetParent(targetTrans);
                transform.localPosition = Vector3.zero;
            }
            else
            {
                transform.position = targetTrans.position;
            }

            SetFxState(true);
            PlayAnim();
            if (skillDuration <= 0)
            {
                skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            }
            Debug.Log($"skill action dur  = {skillDuration}");
            await heroAct.Invoke(skillDuration, token);

            SetFxState(false);
            endAct?.Invoke();
            PoolController.Instance.ReturnToPool(gameObject);*/
        }

        public async UniTaskVoid DoSkill(Func<Vector3, float, CancellationToken, UniTask> heroAct, Action endAct, Vector3 targetPos, CancellationToken token)
        {
            /*transform.position = targetPos;
            SetFxState(true);
            PlayAnim();
            if (skillDuration <= 0)
            {
                skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            }

            await heroAct.Invoke(targetPos,skillDuration, token);

            atkAct?.Invoke(RangeSkill, dameSkillFactor);
            SetFxState(false);
            endAct?.Invoke();
            PoolController.Instance.ReturnToPool(gameObject);*/
        }

        public async UniTaskVoid DoSkill(Vector3 targetPos, Func<Vector3,float, CancellationToken, Action<Vector3>, UniTask> heroAct, Action endAct, CancellationToken token)
        {
            /*if (skillDuration <= 0)
            {
                skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            }

            Debug.Log($"skill action dur  = {skillDuration}");
            await heroAct.Invoke(targetPos, skillDuration, token, (p) => DoSingleSkill(p));

            atkAct?.Invoke(RangeSkill, DameSkillFactor);
            SetFxState(false);
            endAct?.Invoke();
            PoolController.Instance.ReturnToPool(gameObject);*/
        }

        private void DoSingleSkill(Vector3 pos) 
        {
            /*SetFxState(false);
            transform.position = pos;
            PlayAnim();
            SetFxState(true);*/
        }

        public void SetFxState(bool isShow)
        {
            gameObject.SetActive(isShow);
        }
    }
}
