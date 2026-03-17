using Cysharp.Threading.Tasks;
using Scripts.Common;
using Spine.Unity;
using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class TierS2SkillController : BaseExternalSkillController
    {
        [SerializeField] SkeletonAnimation skaFx;

        private Vector3 targetPos;

        private async UniTaskVoid DoActSkill(Action<float> camAct = null)
        {
            PlayAnim(skaFx);
            var dur = GetAnimDur(skaFx);
            camAct?.Invoke(dur);
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            base.DoEndSkill().Forget();
            PoolController.Instance?.ReturnToPool(gameObject);
        }

        private async UniTaskVoid DoActSkillWithoutEndAct(Action<float> camAct)
        {
            PlayAnim(skaFx);
            var dur = GetAnimDur(skaFx);
            camAct?.Invoke(dur);
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            PoolController.Instance?.ReturnToPool(gameObject);
        }

        public override void SetHeroPlayerController(PlayerHeroController phC)
        {
            base.SetHeroPlayerController(phC);
        }

        public override void InitInnerSkill(bool isInit, Action<float> camAct)
        {
            InitSka();
            targetPos = PlayerHeroController.GetNearestMonster();
            transform.position = targetPos;
            if (isInit)
            {
                RegisterAnimEvent(AttackCallback);
            }

            DoActSkill(camAct).Forget();
        }

        public override void InitInnerSkillMultiSpawn(bool isFinal, Action<float> camAct)
        {
            InitSka();
            if (isFinal)
            {
                DoActSkill().Forget();
            }
            else
                DoActSkillWithoutEndAct(camAct).Forget();
        }

        public void InitSka()
        {
            if (skaFx && !skaFx.valid)
            {
                skaFx.Initialize(false);
            }

            GetAnimDur(skaFx);
        }

        private void AttackCallback(float rangeAtk, float dameAtk)
        {
            PlayerHeroController.AttackByArea(targetPos,rangeAtk, dameAtk);
        }

        public override void RegisterAnimEvent(Action<float, float> eventAct)
        {
            skaFx.AnimationState.Event += (entry, e) =>
            {
                if (AnimSkill == entry.Animation.Name && e.Data.Name == EnventHit)
                {
                    Debug.Log($"Anim event {EnventHit} triggered.");
                    eventAct?.Invoke(RangeSkill, DameSkillFactor);
                }
            };
        }
    }
}
