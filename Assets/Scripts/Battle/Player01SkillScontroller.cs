using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Scripts.Common;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Scripts.Battle
{
    public class Player01SkillScontroller : BaseSkillController
    {
        [SerializeField] PlayerHeroController playerHeroController;
        [SerializeField] List<BaseExternalSkillController> fxSkills;
        [SerializeField] SkeletonAnimation winFx;
        [SerializeField] Transform skillTrans;
        [SerializeField] BaseExternalSkillController externalSkillController;
        [SerializeField] Transform arrowTrans;
        [SerializeField] Transform oArrowTrans;

        private int attackIdx = 0;
        private const string eventAttack = "hit";
        private Dictionary<int, SkillDataSO> skillDataSOs;
        public string boneArraw = "L_hand";
        private Spine.Bone handBone;

        private void Awake()
        {
            if(arrowTrans != null) 
                arrowTrans.gameObject.SetActive(false);
            GameEventManager.Subscribe(GameEvents.OnStageLost, StopAllCoroutines);
        }

        private void Start()
        {
            handBone = BaseAnimController.GetBaseSka().Skeleton.FindBone(boneArraw);
        }

        public void InitSkillData(List<int>skillIds)
        {
            if (skillDataSOs == null) skillDataSOs = new Dictionary<int, SkillDataSO>();
            skillDataSOs.Clear();

            for (int i = 0; i < skillIds.Count; i++)
            {
                var skillId = skillIds[i];
                skillDataSOs[i] = MasterDataCache.Instance?.GetSkillDataById(skillId);
                /*skillDataSOs[1] = MasterDataCache.Instance?.GetSkillDataById(3);
                skillDataSOs[2] = MasterDataCache.Instance?.GetSkillDataById(4);
                skillDataSOs[3] = MasterDataCache.Instance?.GetSkillDataById(5);
                skillDataSOs[4] = MasterDataCache.Instance?.GetSkillDataById(6);*/
            }
        }
        
        public override void InitSkill(List<int> skillIds, Transform soTrans)
        {
            InitSkillData(skillIds);
            //fxSkills = bEscs;
            skillTrans = soTrans;

            ResgisterHitSwitchEvent();
            RegisterHitAttactEvent();
        }

        private void DoShakeCam(float dur, int viration, ShakeType shakeType = ShakeType.Shake)
        {
            playerHeroController?.DoShakeCam(dur, viration, shakeType);
        }

        private void ResgisterHitSwitchEvent()
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Switch, eventAttack, DoHitSwitchEventAction);
        }

        private void RegisterHitAttactEvent()
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack1, eventAttack, DoAttackByArrowEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack2, eventAttack, DoAttackByArrowEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventAttack, DoAttackByArrowEvent);
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
            if (playerHeroController.HeroAttackType == HeroAttackType.Knight)
            {
                yield return new WaitForSeconds(dur / 2);
                DoAttackFx();
                playerHeroController?.AttackBySpecific();
                yield return new WaitForSeconds(dur / 2);
                endAct?.Invoke();
                SkaFx.gameObject.SetActive(false);
            }
            else if(playerHeroController.HeroAttackType == HeroAttackType.Archer)
            {
                yield return new WaitForSeconds(dur);
                endAct?.Invoke();
            }
        }

        private void DoAttackByArrowEvent()
        {
            if(playerHeroController.HeroAttackType != HeroAttackType.Archer) return;
            var (arrow, b) = PoolController.Instance.Get(arrowTrans, oArrowTrans.position);
            var targetPos = playerHeroController.GetMonsterPos() + Vector3.up;
            Vector3 direction = (targetPos - oArrowTrans.position).normalized;
            if (arrow != null)
            {
                if (handBone == null) handBone = BaseAnimController.GetBaseSka().skeleton.FindBone(boneArraw);
                Debug.Log("hand bone" + handBone == null);
                arrow.position = handBone != null ? handBone.GetWorldPosition(BaseAnimController.GetBaseSka().transform) : oArrowTrans.position;
                arrow.rotation = Quaternion.FromToRotation(Vector3.right, direction);
            }
            StartCoroutine(DoFlyAsync(arrow, targetPos));
        }

        private IEnumerator DoFlyAsync(Transform arrow,Vector3 target)
        {
            arrow.position = Vector3.MoveTowards(arrow.position, target, 20 * Time.deltaTime);
            while ((arrow.position - target).sqrMagnitude > 0.1f)
            {
                yield return null;
                arrow.position = Vector3.MoveTowards(arrow.position, target, 20 * Time.deltaTime);
                arrow.Rotate(Vector3.right, 30, Space.Self);
            }

            DoAttackFx();
            playerHeroController?.AttackBySpecific();
            PoolController.Instance.ReturnToPool(arrow.gameObject);
            yield return new WaitForSeconds(.2f);
            SkaFx.gameObject.SetActive(false);
        }

        private void DoAttackFx()
        {
            var pos = playerHeroController?.MonsterTarget?.transform.position ?? transform.position;
            SkaFx.transform.position = new Vector3(pos.x, .6f, pos.z);
            SkaFx.gameObject.SetActive(true);
            PlayAmin(StandAnimName.Attack1);
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

        private void DoUISkillAnim()
        {
            var pos = new Vector3(2,.6f,6);
            skillTrans.localPosition = pos;
            skillTrans.localScale = Vector3.one * .5f;
            float moveTime = .25f;
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(skillTrans.DOLocalMoveX(0, moveTime).SetEase(Ease.InQuart));
            mySequence.Join(skillTrans.DOScale(Vector3.one, moveTime).SetEase(Ease.InQuart));
            mySequence.AppendInterval(.35f);
            mySequence.OnComplete(() =>
            {
                skillTrans.DOLocalMoveX(2, moveTime).SetEase(Ease.InExpo);
                skillTrans.DOScale(Vector3.one*.5f, moveTime).SetEase(Ease.InQuart);
            });
        }

        public override void DoSkill01(Action endAct)
        {
            DoUISkillAnim();

            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[0], endAct, (dur)=>DoShakeCam(dur, 40, ShakeType.Punch));
            /*var pos = transform.position;
            var (fx, b) = PoolController.Instance.Get(fxSkills[0], pos);
            if(b)
            {
                fx.InitSkill((r, d) => playerHeroController?.AttackByArea(pos, r, d));
            }
            fx.DoSkill(ActiveKill1Callback, endAct, transform, this.GetCancellationTokenOnDestroy()).Forget();*/
        }

        public async UniTask ActiveKill1Callback(float dur, CancellationToken token)
        {
            DoShakeCam(dur, 50, ShakeType.Punch);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
        }

        public override void DoSkill02(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[1], endAct, (dur)=> DoShakeCam(dur, 40, ShakeType.Shake));
            /*var pos = playerHeroController.GetNearestMonster();
            var (fx, b) = PoolController.Instance.Get(fxSkills[1], pos);
            if (b)
            {
                fx.InitSkill();
            }
            fx.DoSkill(ActiveKill2Callback, endAct, transform, this.GetCancellationTokenOnDestroy()).Forget();*/
        }

        private async UniTask ActiveKill2Callback(float dur, CancellationToken token)
        {
            DoShakeCam(dur, 40, ShakeType.Punch);
            var hitCount = 6;
            for (int i = 0; i < hitCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount), cancellationToken: token);
                playerHeroController?.AttackByArea(transform.position, 2, 1f);
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount), cancellationToken: token);
        }

        public override async void DoSkill03(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[2], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
            /*DoUISkillAnim();
            var pos = playerHeroController.GetNearestMonster();
            var numAtk = 6;
            for (int i = 0; i < numAtk; i++)
            {
                var nPos = pos + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
                var (fx, b) = PoolController.Instance.Get(fxSkills[2], pos);
                if (b)
                {
                    fx.InitSkill((r, d) => playerHeroController?.AttackByArea(nPos, r, d));
                }
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: this.GetCancellationTokenOnDestroy());
                fx.DoSkill(nPos, ActiveKill3Callback, i < numAtk - 1 ? null : endAct, this.GetCancellationTokenOnDestroy()).Forget();
            }      */
        }

        private async UniTask ActiveKill3Callback(Vector3 targetPos, float dur, CancellationToken token, Action<Vector3> callAct)
        {
            DoShakeCam(dur, 100);
            callAct?.Invoke(targetPos);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
            //playerHeroController?.AttackByArea(targetPos, 1.5f);
        }

        public override void DoSkill04(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[3], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
            /*DoUISkillAnim();
            var pos = playerHeroController.GetNearestMonster();
            var (fx, b) = PoolController.Instance.Get(fxSkills[3], pos);
            if (b)
            {
                fx.InitSkill((r, d) => playerHeroController?.AttackByArea(pos, r, d));
            }
            fx.DoSkill(ActiveKill4Callback, endAct, pos, this.GetCancellationTokenOnDestroy()).Forget();*/
        }

        private async UniTask ActiveKill4Callback(Vector3 targetPos, float dur, CancellationToken token)
        {
            DoShakeCam(dur, 50);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
            /*var hitCount = 6;
            for (int i = 0; i < hitCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur / hitCount), cancellationToken: token);
                playerHeroController?.AttackByArea(targetPos, 2f, factorDam: 1);
            }*/
        }

        public override async void DoSkill05(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[4], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
            /*DoUISkillAnim();
            var pos = playerHeroController.GetNearestMonster();
            var numAtk = 10;
            for (int i = 0; i < numAtk; i++)
            {
                var nPos = pos + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
                var (fx, b) = PoolController.Instance.Get(fxSkills[4], pos);
                if (b)
                {
                    fx.InitSkill((r,d) => playerHeroController?.AttackByArea(nPos, r, d));
                }
                else
                {
                    fx.SetFxState(false);
                }    
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: this.GetCancellationTokenOnDestroy());
                fx.DoSkill(nPos, ActiveKill5Callback, i < numAtk - 1 ? null : endAct, this.GetCancellationTokenOnDestroy()).Forget();
            }*/
        }

        private async UniTask ActiveKill5Callback(Vector3 targetPos, float dur, CancellationToken token, Action<Vector3> callAct)
        {
            DoShakeCam(dur, 100);
            callAct?.Invoke(targetPos);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
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
            yield return DoActivePassive(endAct);
        }

        private IEnumerator DoActivePassive(Action endAct)
        {
            var isShow = UnityEngine.Random.Range(0, 3) == 0;
            if (isShow)
            {
                var dur = BaseAnimController.GetDurByAnimName(StandAnimName.PassiveSwitch);
                BaseAnimController?.PlayAmin(StandAnimName.PassiveSwitch, 1, false);
                yield return new WaitForSeconds(dur);
                endAct?.Invoke();
            }
            else
                endAct?.Invoke();
        }

        private void DoHitSwitchEventAction()
        {
            playerHeroController?.AttackByArea(transform.position, 2f, 1);
        }

        public override void DoWin(Action endAct)
        {
            StartCoroutine(CoDoWin(endAct, StandAnimName.Win));
        }

        private IEnumerator CoDoWin(Action endAct, string animName)
        {
            yield return new WaitForSeconds(.25f);
            BaseAnimController?.PlayAmin(animName, 1, false);
            var dur = BaseAnimController.GetDurByAnimName(animName);
            yield return new WaitForSeconds(dur);
            PlayAmin(winFx, "win", 1, true);
            Vector3[] path = new Vector3[] 
            {
                transform.position - Vector3.forward * 20 - Vector3.right * 10,
                transform.position - Vector3.forward * 35 + Vector3.up *5,
                transform.position - Vector3.forward * 20 + Vector3.right * 10,
                transform.position + Vector3.forward * 0 + Vector3.right * 15,
                Vector3.forward * 45,
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

        public override void DoFlash(Action endAct)
        {
            DoFlashAsync(endAct).Forget();
        }

        private async UniTaskVoid DoFlashAsync(Action endAct)
        {
            var pos = playerHeroController.GetFlashPos();
            var ske = BaseAnimController.GetBaseSka().skeleton;
            ske.A = 1f;
            while (ske.A > 0.1f)
            {
                ske.A -= Time.deltaTime * 2f;
                await UniTask.DelayFrame(1);
            }
            transform.position = pos;
            while (ske.A < 1)
            {
                ske.A += Time.deltaTime * 3f;
                await UniTask.DelayFrame(1);
            }

            ske.A = 1f;
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
                case HeroSkills.Flash:
                    DoFlash(endAct);
                    break;
                case HeroSkills.Die:
                    DoDeath(endAct);
                    break;
            }
        }
    }
}
