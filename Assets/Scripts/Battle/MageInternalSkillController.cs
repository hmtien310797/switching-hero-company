using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Battle
{
    public class MageInternalSkillController : PlayerInternalFarSkillController
    {
        public override void InitSkill(List<int> bEscs, Transform soTrans)
        {
            base.InitSkill(bEscs, soTrans);

            ResgisterHitSwitchEvent(DoHitSwitchEventAction);
        }

        private void ResgisterHitSwitchEvent(Action<bool> action)
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Switch, eventAttack, action);
        }

        private void DoHitSwitchEventAction(bool isFanal = false)
        {
            PlayerHeroController?.AttackByArea(transform.position, PlayerHeroController.GetSwitchArea, 1);
        }

        public override void DoSwitch(Action endAct)
        {
            BaseAnimController?.PlayAmin(StandAnimName.Switch, 1, false);
            CoDoSwitch(endAct, StandAnimName.Switch).Forget();
        }

        private async UniTaskVoid CoDoSwitch(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: DisableCts.Token);
            DoActivePassive(endAct);
        }

        private void DoActivePassive(Action endAct)
        {
            var isShow = UnityEngine.Random.Range(0, 3) == 0;
            if (isShow)
            {
                BaseAnimController.AddPassiveAnim(5f);
                endAct?.Invoke();
            }
            else
                endAct?.Invoke();
        }
    }
}
