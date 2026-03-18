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
            if(!IsAtkEvent) PlayerHeroController.AttackByArea(transform.position, RangeSkill, SkillData.FinalDame);
            base.DoEndSkill().Forget();
            PoolController.Instance?.ReturnToPool(gameObject);
        }

        private async UniTaskVoid DoActSkillSSR(Action<float> camAct = null, bool isFinal = false)
        {
            PlayAnim(skaFx, isLoop:true);
            camAct?.Invoke(.25f);

            var targetPos  = new Vector3(transform.position.x, 0, transform.position.z);
            while(transform.position.y > 0)
            {
                await UniTask.DelayFrame(1);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 50);
            }
            
            PlayerHeroController.AttackByArea(targetPos, RangeSkill, isFinal ? SkillData.FinalDame : SkillData.NomalDame);
            if(isFinal) base.DoEndSkill().Forget();
            PoolController.Instance?.ReturnToPool(gameObject);
        }

        private async UniTaskVoid DoActSkillWithoutEndAct(Action<float> camAct)
        {
            PlayAnim(skaFx);
            var dur = GetAnimDur(skaFx);
            camAct?.Invoke(dur);
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            if (!IsAtkEvent) PlayerHeroController.AttackByArea(transform.position, RangeSkill, SkillData.NomalDame);
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
            if (IsAtkEvent)
            {
                RegisterAnimEvent(AttackCallback);
            }

            DoActSkill(camAct).Forget();
        }

        public override void InitInnerSkillMultiSpawn(bool isFinal, Action<float> camAct)
        {
            InitSka();
            targetPos = transform.position;
            if (TierBaseSkill == TierSkill.SS)
            {
                if (!IsAtkEvent)
                {
                    if (isFinal)
                        DoActSkill().Forget();
                    else
                        DoActSkillWithoutEndAct(camAct).Forget();
                }
                else
                {
                    RegisterAnimEvent(AttackCallback);
                    DoActSkill(camAct).Forget();
                }
            }
            else if(TierBaseSkill == TierSkill.SSR)
            {
                if (IsAtkEvent)
                {
                    RegisterAnimEvent(AttackCallback);
                    DoActSkill(camAct).Forget();
                }
                else
                    DoActSkillSSR(camAct, isFinal).Forget();
            }
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

        private void SingleAttackCallback(float rangeAtk, float dameAtk)
        {
            PlayerHeroController.AttackBySpecific();
        }

        public override void RegisterAnimEvent(Action<float, float> eventAct)
        {
            skaFx.AnimationState.Event += (entry, e) =>
            {
                if (AnimSkill == entry.Animation.Name && e.Data.Name == EnventHit)
                {
                    Debug.Log($"Anim event {EnventHit} triggered.");
                    eventAct?.Invoke(RangeSkill, SkillData.NomalDame);
                }
            };
        }
    }
}
