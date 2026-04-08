using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using Spine.Unity;
using UnityEngine;

namespace Battle
{
    public enum SkillSlot
    {
        Slot1 = 0, Slot2 = 1, Slot3 = 2, Slot4 = 3, Slot5 = 4,
    }

    public class Player01SkillScontroller : BaseSkillController
    {
        [SerializeField] PlayerHeroController playerHeroController;
        [SerializeField] List<BaseExternalSkillController> fxSkills;
        [SerializeField] SkeletonAnimation winFx;
        [SerializeField] Transform skillTrans;
        [SerializeField] BaseExternalSkillController externalSkillController;
        [SerializeField] Transform arrowTrans;
        [SerializeField] Transform oArrowTrans;
        public string boneArraw = "BOW_ARROW";
        public string boneAiming = "AIMING";

        private int attackIdx = 0;
        public const string eventAttack = "hit";
        public const string eventFinalAttack = "finalhit";
        private Dictionary<SkillSlot, SkillDataSO> skillDataSOs = new Dictionary<SkillSlot, SkillDataSO>();
        
        private Spine.Bone handBone;
        private Spine.Bone aimBone;

        private CancellationTokenSource _disableCts;

        private void Awake()
        {
            if(arrowTrans != null) 
                arrowTrans.gameObject.SetActive(false);
            GameEventManager.Subscribe(GameEvents.OnStageLost, StopAllCoroutines);
        }

        private void Start()
        {
            handBone = BaseAnimController.GetBaseSka().Skeleton.FindBone(boneArraw);
            aimBone = BaseAnimController.GetBaseSka().Skeleton.FindBone(boneAiming);
        }

        public void InitSkillData(List<int>skillIds)
        {
            skillDataSOs.Clear();

            for (int i = 0; i < skillIds.Count; i++)
            {
                if (i >= 5) break;

                var skillId = skillIds[i];
                skillDataSOs[(SkillSlot)i] = MasterDataCache.Instance?.GetSkillDataById(skillId);
            }
        }

        public override void ChangeSkillBySlot(int slotId, int skillId)
        {
            if(skillDataSOs.ContainsKey((SkillSlot)slotId))
            {
                skillDataSOs[(SkillSlot)slotId] = MasterDataCache.Instance?.GetSkillDataById(skillId);
            }
        }
        
        public override void InitSkill(List<int> skillIds, Transform soTrans)
        {
            skillTrans = soTrans;

            InitSkillData(skillIds);
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
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack1, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack1Back, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack2, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack2Back, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3Back, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventFinalAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3Back, eventFinalAttack, DoAttackByEvent);
        }
                
        private string GetAttackAnimNameByIdx(int idx, bool isAfter = true)
        {
            var anim = (idx) switch
            {
                0 => isAfter ? StandAnimName.Attack1 : StandAnimName.Attack1Back,
                1 => isAfter ? StandAnimName.Attack2 : StandAnimName.Attack2Back,
                2 => isAfter ? StandAnimName.Attack3 : StandAnimName.Attack3Back,
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
            var isAfterTarger = playerHeroController.IsAfterTarget();
            var animName = GetAttackAnimNameByIdx(attackIdx, isAfterTarger);
            /*if (playerHeroController.HeroClass == HeroClass.Archer)
            { 
                animName = isAfterTarger ? StandAnimName.Attack1 : StandAnimName.Attack1Back;
                BaseAnimController?.PlayAmin(animName);
            }
            else*/
                BaseAnimController?.PlayAmin(animName);
            CoDoAttack(endAct, animName).Forget();
        }

        public async UniTaskVoid CoDoAttack(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            if (playerHeroController.HeroClass == HeroClass.Assassin || playerHeroController.HeroClass == HeroClass.Warrior)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: _disableCts.Token);
                endAct?.Invoke();
                SkaFx.gameObject.SetActive(false);
            }
            else if (playerHeroController.HeroClass == HeroClass.Archer)
            {
                var targetPos = playerHeroController.GetMonsterPos() + Vector3.up * 1.5f;
                if (aimBone == null) aimBone = BaseAnimController.GetBaseSka().skeleton.FindBone(boneAiming);

                var isAfterTarger = transform.position.z > targetPos.z;
                var camPos = Camera.main.transform.position;
                camPos.z *= (isAfterTarger ? -1 : 1);
                camPos.y -= (isAfterTarger ? 9 : 0);
                var dir = (targetPos - camPos).normalized;
                Ray ray = new Ray(camPos, dir);
                Plane planeZ = new Plane(Vector3.forward, transform.position);
                var point = transform.position;
                if (planeZ.Raycast(ray, out var distance))
                {
                    point = ray.GetPoint(distance);
                    point = transform.InverseTransformPoint(point);
                }

                aimBone.X = point.x;
                aimBone.Y = point.y;

                await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: _disableCts.Token);
                endAct?.Invoke();
            }
        }

        private void DoAttackByEvent(bool isFinal = false)
        {
            if (playerHeroController.HeroClass != HeroClass.Archer)
            {
                DoAttackFx();
                playerHeroController?.AttackBySpecific();
                return;
            }

            if (oArrowTrans == null) return;
            var (arrow, b) = PoolController.Instance.Get(arrowTrans, oArrowTrans.position);
            var targetPos = playerHeroController.GetMonsterPos() + Vector3.up * 1.5f;
            if (arrow != null)
            {
                if (handBone == null) handBone = BaseAnimController.GetBaseSka().skeleton.FindBone(boneArraw);
                arrow.position = handBone != null ? handBone.GetWorldPosition(BaseAnimController.GetBaseSka().transform) : oArrowTrans.position;
                Vector3 direction = (targetPos - arrow.position).normalized;
                arrow.rotation = Quaternion.FromToRotation(Vector3.right, direction);
            }

            DoFlyAsync(arrow, targetPos).Forget();
        }

        private async UniTaskVoid DoFlyAsync(Transform arrow,Vector3 target)
        {
            if (arrow == null) return;
            arrow.position = Vector3.MoveTowards(arrow.position, target, 30 * Time.deltaTime);
            while ((arrow.position - target).sqrMagnitude > 0.1f)
            {
                arrow.position = Vector3.MoveTowards(arrow.position, target, 30 * Time.deltaTime);
                arrow.Rotate(Vector3.right, 30, Space.Self);
                await UniTask.Yield(PlayerLoopTiming.Update, _disableCts.Token);
            }

            DoAttackFx();
            playerHeroController?.AttackBySpecific();
            PoolController.Instance.ReturnToPool(arrow.gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(.2f), cancellationToken: _disableCts.Token);
            SkaFx.gameObject.SetActive(false);
        }

        private void DoAttackFx()
        {
            var pos = playerHeroController?.MonsterTarget?.transform.position ?? transform.position;
            SkaFx.transform.position = new Vector3(pos.x, 1f, pos.z);
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

            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot1], endAct, (dur)=>DoShakeCam(dur, 40, ShakeType.Punch));
        }

        public async UniTask ActiveKill1Callback(float dur, CancellationToken token)
        {
            DoShakeCam(dur, 50, ShakeType.Punch);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
        }

        public override void DoSkill02(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot2], endAct, (dur)=> DoShakeCam(dur, 40, ShakeType.Shake));
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
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot3], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
        }

        private async UniTask ActiveKill3Callback(Vector3 targetPos, float dur, CancellationToken token, Action<Vector3> callAct)
        {
            DoShakeCam(dur, 100);
            callAct?.Invoke(targetPos);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
        }

        public override void DoSkill04(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot4], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
        }

        private async UniTask ActiveKill4Callback(Vector3 targetPos, float dur, CancellationToken token)
        {
            DoShakeCam(dur, 50);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
        }

        public override async void DoSkill05(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot5], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
        }

        private async UniTask ActiveKill5Callback(Vector3 targetPos, float dur, CancellationToken token, Action<Vector3> callAct)
        {
            DoShakeCam(dur, 100);
            callAct?.Invoke(targetPos);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token);
        }

        private async UniTaskVoid CoDoFlash(Action endAct)
        {
            var pos = GroupFlashController.Instance.GetRandPos();

            playerHeroController?.DoRotate(pos.x > transform.position.x);

            while (Vector3.Distance(transform.position, pos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, pos, 20 * Time.deltaTime);
                await UniTask.DelayFrame(1);
            }

            endAct?.Invoke();
        }

        public override void DoSwitch(Action endAct)
        {
            BaseAnimController?.PlayAmin(StandAnimName.Switch, 1, false);
            CoDoSwitch(endAct, StandAnimName.Switch).Forget();
        }

        private async UniTaskVoid CoDoSwitch(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: _disableCts.Token);
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

        private void DoHitSwitchEventAction(bool isFanal = false)
        {
            playerHeroController?.AttackByArea(transform.position, playerHeroController.GetSwitchArea, 1);
        }

        public override void DoWin(Action endAct)
        {
            CoDoWin(endAct, StandAnimName.Win).Forget();
        }

        private async UniTaskVoid CoDoWin(Action endAct, string animName)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(.25f), cancellationToken: _disableCts.Token);
            BaseAnimController?.PlayAmin(animName, 1, false);
            var dur = BaseAnimController.GetDurByAnimName(animName);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: _disableCts.Token);
            PlayAmin(winFx, "win", 1, true);
            Vector3[] path = new Vector3[] 
            {
                transform.position - Vector3.forward * 20 - Vector3.right * 10,
                transform.position - Vector3.forward * 35 + Vector3.up *5,
                transform.position - Vector3.forward * 20 + Vector3.right * 10,
                transform.position + Vector3.forward * 0 + Vector3.right * 15,
                PvEBattleController.Instance.GetMapEndPoint(),
            };

            transform.DOPath(path, 3f, PathType.CatmullRom).SetEase(Ease.InQuart).OnComplete(() =>
            {
                winFx.gameObject.SetActive(false);
                endAct?.Invoke();
            });

            await UniTask.Delay(TimeSpan.FromSeconds(.5f), cancellationToken: _disableCts.Token);
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

        public override void DoDeath(Action endAct)
        {
            base.DoDeath(endAct);
            var dur = BaseAnimController.GetDurByAnimName(StandAnimName.Die);
            DoDeathAsync(dur, endAct).Forget();
        }

        private async UniTaskVoid DoDeathAsync(float dur, Action endAct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: _disableCts.Token);
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

        private void OnEnable()
        {
            _disableCts = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            if (_disableCts != null)
            {
                _disableCts.Cancel();
                _disableCts.Dispose();
                _disableCts = null;
            }
        }
    }
}
