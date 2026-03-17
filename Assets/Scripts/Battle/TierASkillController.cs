using Cysharp.Threading.Tasks;
using Scripts.Common;
using Spine.Unity;
using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class TierASkillController : BaseExternalSkillController
    {
        [SerializeField] SkeletonAnimation skaFx;

        private async UniTaskVoid DoActSkill(Action<float> camAct)
        {
            PlayAnim(skaFx);
            var dur = GetAnimDur(skaFx);
            camAct?.Invoke(dur);
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            base.DoEndSkill().Forget();
            PoolController.Instance?.ReturnToPool(gameObject);
        }

        public override void InitInnerSkill(bool isInit = true, Action<float> camAct = null)
        {
            InitSka();
            if (IsFollow)
            {
                transform.SetParent(PlayerHeroController.transform);
            }
            else
            {
                transform.position = PlayerHeroController.transform.position;
            }

            if (isInit)
            {
                RegisterAnimEvent(AttackCallback);
            }

            DoActSkill(camAct).Forget();
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
            PlayerHeroController?.AttackByArea(PlayerHeroController.transform.position, rangeAtk, dameAtk);
        }

        public override void RegisterAnimEvent(Action<float,float> eventAct)
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
