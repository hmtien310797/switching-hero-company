using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using Spine.Unity;
using UnityEngine;

namespace Battle
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
            //PoolController.Instance?.ReturnToPool(gameObject);
        }

        private async UniTaskVoid DoActSkillByNum(Action<float> camAct, int numAtk)
        {
            numAtk = numAtk < 1 ? 1 : numAtk;
            PlayAnim(skaFx);
            var dur = GetAnimDur(skaFx)/ numAtk;
            camAct?.Invoke(dur);
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            for (int i = 0; i < numAtk; i++)
            {
                //PlayerHeroController.AttackByArea(transform.position, RangeSkill, i == numAtk - 1? SkillData.FinalDame : SkillData.NomalDame);
                await UniTask.Delay(TimeSpan.FromSeconds(dur));
            }
            base.DoEndSkill().Forget();
            //PoolController.Instance?.ReturnToPool(gameObject);
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

            if (IsAtkEvent)
            {
                RegisterAnimEvent(AttackCallback);
            }
            else
            {
                DoActSkillByNum(camAct, 10).Forget();
                return;
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
            // skaFx.AnimationState.Event += (entry, e) =>
            // {
            //     if (AnimSkill == entry.Animation.Name && e.Data.Name == EnventHit)
            //     {
            //         Debug.Log($"Anim event {EnventHit} triggered.");
            //         eventAct?.Invoke(RangeSkill, SkillData.NomalDame);
            //     }
            //     if (AnimSkill == entry.Animation.Name && e.Data.Name == EnventFinalHit)
            //     {
            //         Debug.Log($"Anim event final {EnventHit} triggered.");
            //         eventAct?.Invoke(RangeSkill, SkillData.FinalDame);
            //     }
            // };
        }
        
        protected virtual bool TryExecuteNewSkillPhase(string eventName, Vector3 Pos)
        {
            // if (PlayerHeroController == null) return false;
            // if (SkillData == null) return false;
            //
            // var phase = SkillData.GetPhaseByEvent(defaultSkillLevel, eventName);
            // if (phase == null) return false;
            //
            // SkillExecutor.ExecutePhase(PlayerHeroController, phase, Pos, RangeSkill);
            return true;
        }
    }
}
