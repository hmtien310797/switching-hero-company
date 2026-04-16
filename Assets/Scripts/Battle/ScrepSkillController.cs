using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Battle
{
    public class ScrepSkillController : BaseSkillController
    {
        [SerializeField] MonsterScrepController monsterScrep;

        public override void DoAttack(Action endAct)
        {
            monsterScrep?.IsLookRight();
            base.DoAttack(endAct);
            StartCoroutine(CoDoAttack(endAct));
        }
        
        private void DoAttackFx()
        {
            SkaFx.transform.position = new Vector3(monsterScrep?.Target.transform.position.x??.5f, 0.5f, monsterScrep?.Target.transform.position.z ?? 0f);
            SkaFx.gameObject.SetActive(true);
            PlayAmin(StandAnimName.Attack1, 1, false);
        }

        private void PlayAmin(string name, float speed = 1, bool isLooped = true)
        {
            if (SkaFx == null) return;

            SkaFx.timeScale = speed;
            SkaFx.AnimationState.SetAnimation(0, name, isLooped);
        }

        public IEnumerator CoDoAttack(Action endAct)
        {
            var dur = BaseAnimController.GetDurByAnimName(StandAnimName.Attack1);
            yield return new WaitForSeconds(dur/2);
            DoAttackFx();
            monsterScrep?.AttackBySpecific();
            yield return new WaitForSeconds(dur / 2);
            endAct?.Invoke();
            SkaFx.gameObject.SetActive(false);
        }

        public override void DoDeath(Action endAct)
        {
            base.DoDeath(endAct);
            StartCoroutine(CoDoDeath(endAct));
        }

        private IEnumerator CoDoDeath(Action endAct)
        {
            var dur = BaseAnimController?.GetDurByAnimName(StandAnimName.Die) ?? 0f;
            yield return new WaitForSeconds(dur);
            endAct?.Invoke();
        }

        public override void DoSpawn(Action endAct)
        {
            base.DoSpawn(endAct);
            CoDoSpawn(endAct).Forget();
        }

        private async UniTask CoDoSpawn(Action endAct)
        {
            var dur = BaseAnimController?.GetDurByAnimName(StandAnimName.Spawn) ?? 0f;
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            endAct?.Invoke();
        }

        public override void DoSkillByIdx(HeroSkills skillIdx, Action endAct)
        {
            switch (skillIdx)
            {
                case HeroSkills.Idle:
                    DoIdle(endAct);
                    break;
                case HeroSkills.Run:
                    DoRun(endAct);
                    break;
                case HeroSkills.Die:
                    DoDeath(endAct);
                    break;
                case HeroSkills.Attack:
                    DoAttack(endAct);
                    break;
                case HeroSkills.Skill1:
                    DoSkill01(endAct);
                    break;
                case HeroSkills.Skill2:
                    DoSkill02(endAct);
                    break;
                case HeroSkills.Skill3:
                    DoSkill03(endAct);
                    break;
                case HeroSkills.Skill4:
                    DoSkill04(endAct);
                    break;
                case HeroSkills.Skill5:
                    DoSkill05(endAct);
                    break;
                case HeroSkills.Spawn:
                    DoSpawn(endAct);
                    break;
            }
        }
    }
}
