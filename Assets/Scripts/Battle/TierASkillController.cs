using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class TierASkillController : BaseExternalSkillController
    {
        public void DoSkill()
        {

        }

        public override void InitSkill(Action<float,float> hitAct = null)
        {
            base.InitSkill();
            RegisterAnimEvent(hitAct);
        }

        public override void RegisterAnimEvent(Action<float,float> eventAct)
        {
            SkaFx.AnimationState.Event += (entry, e) =>
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
