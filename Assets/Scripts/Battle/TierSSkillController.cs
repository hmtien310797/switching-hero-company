using Cysharp.Threading.Tasks;
using Scripts.Common;
using Spine.Unity;
using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class TierSSkillController : BaseExternalSkillController
    {
        [SerializeField] SkeletonAnimation skaFx;

        private Vector3 targetPos;

        private async UniTaskVoid DoActSkill(Action<float> camAct = null)
        {
            PlayAnim(skaFx);
            var dur = GetAnimDur(skaFx);
            camAct?.Invoke(dur);
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            if (!IsAtkEvent) PlayerHeroController.AttackByArea(transform.position, RangeSkill, DameSkillFactor);
            base.DoEndSkill().Forget();
            PoolController.Instance?.ReturnToPool(gameObject);
        }

        public override void SetHeroPlayerController(PlayerHeroController phC)
        {
            base.SetHeroPlayerController(phC);
        }

        public override void InitInnerSkill(bool isFinal, Action<float> camAct)
        {
            InitSkeletonAnimation();
            targetPos = PlayerHeroController.GetNearestMonster();
            transform.position = targetPos;
            if (IsAtkEvent)
            {
                RegisterAnimEvent(AttackCallback);
            }
            
            DoActSkill(camAct).Forget();
        }
                
        private void AttackCallback(float rangeAtk, float dameAtk)
        {
            PlayerHeroController.AttackByArea(targetPos, rangeAtk, dameAtk);
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

                if (AnimSkill == entry.Animation.Name && e.Data.Name == EnventFinalHit)
                {
                    Debug.Log($"Anim event {EnventHit} triggered.");
                    eventAct?.Invoke(RangeSkill, SkillData.FinalDame);
                }
            };
        }
    }
}
