using Cysharp.Threading.Tasks;
using Spine.Unity;
using System;
using System.Threading;
using UnityEngine;

namespace Scripts.Battle
{
    public class BaseExternalSkillController : MonoBehaviour
    {
        [SerializeField] bool isFollow;
        [SerializeField] SkeletonAnimation skaFx;

        private const string animSkill = "animation";
        private float skillDuration;

        public void InitSkill()
        {
            if (skaFx && !skaFx.valid)
            {
                skaFx.Initialize(false);
            }

            skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            Debug.Log($"skill dur = {skillDuration}");
            gameObject.SetActive(false);
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

            gameObject.SetActive(true);
            PlayAnim();
            if (skillDuration <= 0)
            {
                skillDuration = skaFx.Skeleton.Data.FindAnimation(animSkill)?.Duration ?? 0;
            }
            Debug.Log($"skill action dur  = {skillDuration}");
            await heroAct.Invoke(skillDuration, token);

            gameObject.SetActive(false);
            endAct?.Invoke();
        }
    }
}
