using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;

namespace Scripts.Battle
{
    public class PlayerInternalNearSkillController : PlayerSkillController
    {
        public override void InitSkill(List<int> bEscs, Transform soTrans)
        {
            base.InitSkill(bEscs, soTrans);

            RegisterHitAttactEvent();
        }

        public override void DoAttack(Action endAct)
        {
            DoAnimAttack(endAct, out var animName);
            CoDoAttack(endAct, animName).Forget();
        }

        public async UniTaskVoid CoDoAttack(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: DisableCts.Token);
            endAct?.Invoke();
            SkaFx.gameObject.SetActive(false);
        }

        protected void RegisterHitAttactEvent()
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack1, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack2, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventFinalAttack, DoAttackByEvent);
        }

        protected void DoAttackByEvent(bool isFinal = false)
        {
            DoAttackFx();
            PlayerHeroController?.AttackBySpecific();
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
