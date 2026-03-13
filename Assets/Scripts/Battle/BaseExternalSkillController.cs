using Cysharp.Threading.Tasks;
using Scripts.Common;
using Spine.Unity;
using System;
using System.Threading;
using UnityEngine;

namespace Scripts.Battle
{
    public enum TierSkill
    {
        A,
        AA,
        S,
        SS,
        B,
        BB,
    }

    public class BaseExternalSkillController : MonoBehaviour
    {
        [SerializeField] bool isFollow;
        [SerializeField] SkeletonAnimation skaFx;
        [SerializeField] TierSkill tierSkill;
        [SerializeField] float rangeSkill = 4;
        [SerializeField] float dameSkillFactor = 2.5f;

        private const string animSkill = "animation";
        private const string enventHit = "hit";
        private float skillDuration;
        
        private Action<float, float> atkAct;

        public SkeletonAnimation SkaFx { get => skaFx; set => skaFx = value; }

        public string AnimSkill => animSkill;

        public string EnventHit => enventHit;

        public TierSkill TierBaseSkill { get => tierSkill; set => tierSkill = value; }
        public float RangeSkill { get => rangeSkill; set => rangeSkill = value; }
        public float DameSkillFactor { get => dameSkillFactor; set => dameSkillFactor = value; }
        public Action<float, float> AtkAct { get => atkAct; set => atkAct = value; }

        public virtual void InitSkill(Action<float,float> hitAct = null)
        {
            if (skaFx && !skaFx.valid)
            {
                skaFx.Initialize(false);
            }

            skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            SetFxState(false);
            AtkAct = hitAct;
        }

        public virtual void RegisterAnimEvent(Action<float, float> eventAct)
        {
            
        }

        private void PlayAnim(float speed = 1)
        {
            skaFx.AnimationState.TimeScale = speed;
            skaFx.AnimationState.SetAnimation(0, animSkill, false);
        }

        public async UniTaskVoid DoSkill(Func<float, CancellationToken, UniTask> heroAct, Action endAct, Transform targetTrans, CancellationToken token)
        {
            if (isFollow)
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
            PoolController.Instance.ReturnToPool(gameObject);
        }

        public async UniTaskVoid DoSkill(Func<Vector3, float, CancellationToken, UniTask> heroAct, Action endAct, Vector3 targetPos, CancellationToken token)
        {
            transform.position = targetPos;
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
            PoolController.Instance.ReturnToPool(gameObject);
        }

        public async UniTaskVoid DoSkill(Vector3 targetPos, Func<Vector3,float, CancellationToken, Action<Vector3>, UniTask> heroAct, Action endAct, CancellationToken token)
        {
            if (skillDuration <= 0)
            {
                skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            }

            Debug.Log($"skill action dur  = {skillDuration}");
            await heroAct.Invoke(targetPos, skillDuration, token, (p) => DoSingleSkill(p));

            atkAct?.Invoke(RangeSkill, DameSkillFactor);
            SetFxState(false);
            endAct?.Invoke();
            PoolController.Instance.ReturnToPool(gameObject);
        }

        private void DoSingleSkill(Vector3 pos) 
        {
            SetFxState(false);
            transform.position = pos;
            PlayAnim();
            SetFxState(true);
        }

        public void SetFxState(bool isShow)
        {
            gameObject.SetActive(isShow);
        }
    }
}
