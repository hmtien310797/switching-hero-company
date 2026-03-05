using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scripts.Battle
{
    public class Player01SkillScontroller : BaseSkillController
    {
        [SerializeField] PlayerHeroController playerHeroController;
        [SerializeField] List<BaseExternalSkillController> fxSkills;
        [SerializeField] SkeletonAnimation winFx;

        private int attackIdx = 0;
        private const string fxAttacName = "animation";
        private const string eventAttack = "hit";
        private List<BaseExternalSkillController> skillFxList = new List<BaseExternalSkillController>();

        private void Start()
        {
            InitSkillPrefab();
            ResgisterEvent();
        }

        private void ResgisterEvent()
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Switch, eventAttack, DoEventAction);
        }

        private void InitSkillPrefab()
        {
            foreach (var fx in fxSkills)
            {
                var fxObj = Instantiate(fx, Vector3.up * 100, Quaternion.identity);
                //fx.gameObject.SetActive(false);
                fx.InitSkill();
                skillFxList.Add(fxObj);
            }
        }

        private string GetAttackAnimNameByIdx(int idx)
        {
            var anim = (idx) switch
            {
                0 => StandAnimName.Attack1,
                1 => StandAnimName.Attack2,
                2 => StandAnimName.Attack3,
                _ => StandAnimName.Attack1,
            };
                
            attackIdx++;
            attackIdx = attackIdx > 2 ? 0 : attackIdx;

            return anim;
        }

        public override void DoAttack(Action endAct)
        {
            var isRight = playerHeroController?.IsLookRight()?? false;
            playerHeroController?.DoRotate(isRight);
            var animName = GetAttackAnimNameByIdx(attackIdx);
            BaseAnimController?.PlayAmin(animName);
            StartCoroutine(CoDoAttack(endAct, animName));
        }

        public IEnumerator CoDoAttack(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            yield return new WaitForSeconds(dur / 2);
            DoAttackFx();
            playerHeroController?.AttackBySpecific();
            yield return new WaitForSeconds(dur / 2);
            endAct?.Invoke();
            SkaFx.gameObject.SetActive(false);
        }

        private void DoAttackFx()
        {
            var pos = playerHeroController?.MonsterStarget?.transform.position ?? transform.position;
            SkaFx.transform.position = new Vector3(pos.x, .6f, pos.z);
            SkaFx.gameObject.SetActive(true);
            PlayAmin(fxAttacName);
        }

        private void PlayAmin(string name, float speed = 1, bool isLooped = true)
        {
            if (SkaFx == null) return;

            SkaFx.timeScale = speed;
            SkaFx.AnimationState.SetAnimation(0, name, isLooped);
        }

        private void PlayAmin(SkeletonAnimation ska, string name, float speed = 1, bool isLooped = true)
        {
            if (ska == null) return;

            ska.timeScale = speed;
            ska.AnimationState.SetAnimation(0, name, isLooped);
        }

        public override void DoSkill01(Action endAct)
        {
            skillFxList[0]?.DoSkill(ActiveKill1Callback, endAct, transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        public async UniTask ActiveKill1Callback(float dur, CancellationToken token)
        {
            var hitCount = 3;
            for (int i = 0; i < hitCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount), cancellationToken: token);
                playerHeroController?.AttackByArea(transform.position);
            }
        }

        public override void DoSkill02(Action endAct)
        {
            skillFxList[1]?.DoSkill(ActiveKill2Callback, endAct, transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ActiveKill2Callback(float dur, CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(dur/2), cancellationToken: token);
            playerHeroController?.AttackByArea(transform.position, 3, 3.5f);
            await UniTask.Delay(TimeSpan.FromSeconds(dur / 2), cancellationToken: token);
        }

        public override void DoSkill03(Action endAct)
        {
            skillFxList[2]?.DoSkill(ActiveKill3Callback, endAct, transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ActiveKill3Callback(float dur, CancellationToken token)
        {
            int hitCount = 5;
            for (int i = 0; i < hitCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount), cancellationToken: token);
                playerHeroController?.AttackByArea(transform.position);
            }
        }

        public override void DoSkill04(Action endAct)
        {
            skillFxList[3]?.DoSkill(ActiveKill4Callback, endAct, playerHeroController.MonsterStarget?.transform??transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ActiveKill4Callback(float dur, CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(dur / 2), cancellationToken: token);
            playerHeroController?.AttackByArea(transform.position, factorDam: 5);
            await UniTask.Delay(TimeSpan.FromSeconds(dur / 2), cancellationToken: token);
        }

        public override void DoSkill05(Action endAct)
        {
            skillFxList[4]?.DoSkill(ActiveKill5Callback, endAct, transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ActiveKill5Callback(float dur, CancellationToken token)
        {
            int hitCount = 3;
            for (int i = 0; i < hitCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount+1), cancellationToken: token);
                playerHeroController?.AttackByArea(transform.position, factorDam: 2);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount+1), cancellationToken: token);
        }

        private IEnumerator CoDoFlash(Action endAct)
        {
            var pos = GroupFlashController.Instance.GetRandPos();

            playerHeroController?.DoRotate(pos.x > transform.position.x);

            while (Vector3.Distance(transform.position, pos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, pos, 20 * Time.deltaTime);
                yield return null;
            }

            endAct?.Invoke();
        }

        public override void DoSwitch(Action endAct)
        {
            BaseAnimController?.PlayAmin(StandAnimName.Switch, 1, false);
            StartCoroutine(CoDoSwitch(endAct, StandAnimName.Switch));
        }

        private IEnumerator CoDoSwitch(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            yield return new WaitForSeconds(dur);          
            endAct?.Invoke();
        }

        private void DoEventAction()
        {
            playerHeroController?.AttackByArea(transform.position, 3.5f, 1);
        }

        public override void DoWin(Action endAct)
        {
            StartCoroutine(CoDoWin(endAct, StandAnimName.Win));
        }

        private IEnumerator CoDoWin(Action endAct, string animName)
        {
            yield return new WaitForSeconds(5f);
            BaseAnimController?.PlayAmin(animName, 1, false);
            var dur = BaseAnimController.GetDurByAnimName(animName);
            yield return new WaitForSeconds(dur);
            PlayAmin(winFx, "win", 1, true);
            Vector3[] path = new Vector3[] 
            {
                transform.position + Vector3.forward * 20 + Vector3.right * 10,   // Điểm uốn 1
                transform.position + Vector3.forward * 50 + Vector3.right * 15,
                transform.position + Vector3.forward * 80,
            };

            transform.DOPath(path, 3f, PathType.CatmullRom).SetEase(Ease.InQuart).OnComplete(() =>
            {
                winFx.gameObject.SetActive(false);
                endAct?.Invoke();
            });

            yield return new WaitForSeconds(.5f);
            winFx.gameObject.SetActive(true);
        }

        public override void DoSpawn(Action endAct)
        {
            base.DoSpawn(endAct);
            DoSpawnAsync(endAct).Forget();
        }

        private async UniTaskVoid DoSpawnAsync(Action endAct)
        {
            var dur = BaseAnimController.GetDurByAnimName(StandAnimName.Spawn);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: this.GetCancellationTokenOnDestroy());
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
                case HeroSkills.Switch:
                    DoSwitch(endAct);
                    break;
                case HeroSkills.Win:
                    DoWin(endAct);
                    break;
                case HeroSkills.Spawn:
                    DoSpawn(endAct);
                    break;
            }
        }
    }
}
