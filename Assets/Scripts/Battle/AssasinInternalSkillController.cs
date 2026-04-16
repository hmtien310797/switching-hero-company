using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Battle
{
    public class AssasinInternalSkillController : PlayerInternalNearSkillController
    {
        [SerializeField] float factorDamage = 2.5f;
        [SerializeField] int numAttack = 5;

        private bool enemyDeathBySwitch = false;

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
            PlayerHeroController?.AttackSpecificByArea(transform.position,out enemyDeathBySwitch, PlayerHeroController.GetSwitchArea, factorDamage);
        }

        public override void DoSwitch(Action endAct)
        {
            DoSwitchAsync(endAct, StandAnimName.Switch).Forget();
        }

        private async UniTaskVoid DoSwitchAsync(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            for (int i = 0; i < numAttack; i++)
            {
                BaseAnimController?.PlayAmin(StandAnimName.Switch, 1, false);
                var pos = PlayerHeroController.GetWeakestEnemy();
                await UniTask.Delay(TimeSpan.FromSeconds(dur / 3), cancellationToken: DisableCts.Token);
                PlayerHeroController.ChangeToPos(pos);
                await UniTask.Delay(TimeSpan.FromSeconds(2 * dur / 3), cancellationToken: DisableCts.Token);
                if(!enemyDeathBySwitch)
                {
                    break;
                }
                enemyDeathBySwitch = false;
            }

            DoActivePassive(endAct);
        }

        private void DoActivePassive(Action endAct)
        {
            var isShow = UnityEngine.Random.Range(0, 3) == 0;
            if (isShow)
            {
                PassiveCastAsync(endAct).Forget();
            }
            else
            {
                PlayerHeroController?.ChangeToPosWithFlash();
                endAct?.Invoke();
            }
        }

        private async UniTaskVoid PassiveCastAsync(Action endAct)
        {
            var dur = BaseAnimController.GetDurByAnimName(StandAnimName.PassiveCast);
            BaseAnimController?.PlayAmin(StandAnimName.PassiveCast, 1, false);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: DisableCts.Token);
            BaseAnimController.AddPassiveAnim(5f);
            PlayerHeroController?.ChangeToPosWithFlash();
            endAct?.Invoke();
        }
    }
}
