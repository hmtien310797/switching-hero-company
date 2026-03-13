using System;
using UnityEngine;

namespace Scripts.Battle
{
    public class TierS2SkillController : BaseExternalSkillController
    {
        public override void InitSkill(Action<float, float> hitAct = null)
        {
            base.InitSkill(hitAct);
        }

        public override void RegisterAnimEvent(Action<float, float> eventAct)
        {
            
        }

    }
}
