using Cysharp.Threading.Tasks;
using Spine.Unity;
using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class TierSSkillController : BaseExternalSkillController
    {
        [SerializeField] SkeletonAnimation skaFx;

        public async UniTaskVoid DoSkill()
        {
            AtkAct?.Invoke(RangeSkill, DameSkillFactor);
        }

        public override void InitInnerSkill(bool isInit, Action<float> camAct)
        {
            /*base.InitSkill(pHc, skillData, endAct);
            base.InitSkill();
            RegisterAnimEvent(hitAct);*/
        }

        public override void RegisterAnimEvent(Action<float, float> eventAct)
        {
            /*SkaFx.AnimationState.Event += (entry, e) =>
            {
                if (AnimSkill == entry.Animation.Name && e.Data.Name == EnventHit)
                {
                    Debug.Log($"Anim event {EnventHit} triggered.");
                    eventAct?.Invoke(RangeSkill, DameSkillFactor);
                }
            };*/
        }
    }
}
