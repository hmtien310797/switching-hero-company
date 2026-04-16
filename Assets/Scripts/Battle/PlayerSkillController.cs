using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Battle
{
    public enum SkillSlot
    {
        Slot1 = 0, Slot2 = 1, Slot3 = 2, Slot4 = 3, Slot5 = 4,
    }

    public class PlayerSkillController : BaseSkillController
    {
        [SerializeField] PlayerHeroController playerHeroController;
        [SerializeField] List<BaseExternalSkillController> fxSkills;
        [SerializeField] SkeletonAnimation winFx;
        [SerializeField] Transform skillTrans;
        [SerializeField] BaseExternalSkillController externalSkillController;

        private int attackIdx = 0;
        public const string eventAttack = "hit";
        public const string eventFinalAttack = "finalhit";
        private Dictionary<SkillSlot, SkillDataSO> skillDataSOs = new Dictionary<SkillSlot, SkillDataSO>();
       

        private CancellationTokenSource _disableCts;

        public PlayerHeroController PlayerHeroController { get => playerHeroController; set => playerHeroController = value; }
        public int AttackIdx { get => attackIdx; set => attackIdx = value; }
        public CancellationTokenSource DisableCts { get => _disableCts; set => _disableCts = value; }

        private void Awake()
        {
            GameEventManager.Subscribe(GameEvents.OnStageLost, StopAllCoroutines);
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
        }

        private void DoShakeCam(float dur, int viration, ShakeType shakeType = ShakeType.Shake)
        {
            playerHeroController?.DoShakeCam(dur, viration, shakeType);
        }

        protected string GetAttackAnimNameByIdx(int idx, bool isAfter = true)
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

        protected void DoAnimAttack(Action endAct, out string animOut)
        {
            var isRight = playerHeroController?.IsLookRight()?? false;
            playerHeroController?.DoRotate(isRight);
            var isAfterTarger = playerHeroController.IsAfterTarget();
            animOut = GetAttackAnimNameByIdx(attackIdx, isAfterTarger);
            BaseAnimController?.PlayAmin(animOut);
        }

        protected void DoAttackFx()
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

        public override async void DoSkill03(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot3], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
        }

        public override void DoSkill04(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot4], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
        }

        public override async void DoSkill05(Action endAct)
        {
            DoUISkillAnim();
            externalSkillController?.InitSkill(playerHeroController, skillDataSOs[SkillSlot.Slot5], endAct, (dur) => DoShakeCam(dur, 40, ShakeType.Shake));
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
