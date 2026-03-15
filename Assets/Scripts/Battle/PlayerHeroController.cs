using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Scripts.UI;
using Spine.Unity;
using System;
using System.Collections;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace Scripts.Battle
{
    public class UIHeroBehavior
    {
        public bool IsAutoSwitch = false;
        public bool IsAutoSkills = false;
        public int SwitchIdx = -1;
        public int SkillIdx = -1;
    }

    public class HeroCoolingTimeDefine
    {
        public float intervalSkill1;
        public float intervalSkill2;
        public float intervalSkill3;
        public float intervalSkill4;
        public float intervalSkill5;
        public float intervalSwitch;

        public HeroCoolingTimeDefine(float timer1, float timer2, float timer3, float timer4, float timer5, float timerS) 
        { 
            intervalSkill1 = timer1; intervalSkill2 = timer2; intervalSkill3 = timer3; intervalSkill4 = timer4; intervalSkill5 = timer5;intervalSwitch = timerS;
        }
    }

    public class PlayerHeroController : BaseCharacterController<PlayerHeroController>, ICombatUnit
    {
        [SerializeField] PlayerCamController playerCamController;
        [SerializeField] SkeletonAnimation hero;
        [SerializeField] Material hideMat;
        [SerializeField] float distFlashConst = 5;

        private Material originalMaterial;

        private PvEBattleController pvEBattleController;
        private MonsterScrepController _monsterTarget;
        private int heroId = -1;

        public SpawnState SpawnState = new SpawnState();
        public IdleState IdleState = new IdleState();
        public MoveState MoveState = new MoveState();
        public FlashState FlashState = new FlashState();
        public AttackState AttackState = new AttackState();
        public SwitchState SwitchHeroState = new SwitchState();
        public InjuredState InjureedState = new InjuredState();
        public DeathState DeathState = new DeathState();
        public WinState WinState = new WinState();

        private Action endAct = null;
        private bool isInSkillAction = false;
        private bool isInSwitchAction = false;
        private readonly Vector3 heroSpawnPosition = new Vector3(0f, 0f, 12f);
        private bool isValid = false;
        private bool isMain = false;
        private Transform partnerTrans = null;
        private HeroDataSO baseHeroData;
        private HeroCoolingTimeDefine intervalSkill = null; 
        private bool isPriorityNearTarget = false;
        private Vector3 targetPos = Vector3.zero;
        private FollowHeroController followHeroController;

        public MonsterScrepController MonsterTarget { get => _monsterTarget; set => _monsterTarget = value; }
        public FollowHeroController FollowHeroController { get => followHeroController; set => followHeroController = value; }

        protected override void Awake()
        {
            base.Awake();
            
            GameEventManager.Subscribe(GameEvents.OnStageCleared, RegisterBattleResult);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnGameLose);
        }

        public void InitHero(HeroDataSO data, PvEBattleController pbc, PlayerCamController pcc, Transform soTrans, Transform partnerTrans, FollowHeroController fHc, bool isSwitch = false, bool isMainHero = false)
        {
            isMain = isMainHero;
            isPriorityNearTarget = isMain;
            isValid = true;
            heroId = data.Id;
            pvEBattleController = pbc;
            playerCamController = pcc;
            isInSwitchAction = isSwitch;
            baseHeroData = data;
            followHeroController = fHc;
            SetPartner(partnerTrans);
            SetIntervalSkills();
            playerCamController?.InitCam(transform, isMain);
            InitSkill(null, soTrans);
            SetTargetPos();
            InitHeroData();
            SwitchState(SpawnState);
            InitUIHeroBattle();
        }

        private void SetTargetPos()
        {
            targetPos = isPriorityNearTarget ? GroupFlashController.Instance?.GetNearestPoint()?? transform.position : GroupFlashController.Instance?.GetFarestPoint()??transform.position;
            Debug.Log($"target pos is: {isPriorityNearTarget} and pos is: {targetPos}");
        }

        private void SetIntervalSkills()
        {
            if(isMain)
            {
                intervalSkill = new HeroCoolingTimeDefine(12, 10, 10, 10, 10, 15);
            }
            else
            {
                intervalSkill = new HeroCoolingTimeDefine(15, 12, 12, 12, 13, 18);
            }
        }

        public void SetPartner(Transform partnerTrans)
        {
            this.partnerTrans = partnerTrans;
        }

        public void DoSwitchHero(PlayerCamController pCc)
        {
            isValid = true;
            playerCamController = pCc;
            playerCamController?.InitCam(transform, isMain);
            InitUIHeroBattle();
            SwitchState(SpawnState);
        }

        private void InitHeroData()
        {
            baseStatData = new BaseStat
            {
                Health = baseHeroData.Health,
                AttackRange = baseHeroData.AttackRange,
                Attack = baseHeroData.Attack,
                Accuracy = 0,
                AttackSpeed = baseHeroData.AttackSpeed,
                CritChance = baseHeroData.CritChance,
                CritDamage = baseHeroData.CritDamage,
                Element = baseHeroData.Element,
                Defense = baseHeroData.Defense,
            };
             
            Stats.Initialize(baseStatData);
        }

        private void Start()
        {
            originalMaterial = hero.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;
        }

        public override void Update()
        {
            if (!isValid) return;
            base.Update();

            if (!isInSkillAction && !isInSwitchAction && IsInSkillRange(baseStatData.AttackRange * 1.5f))
            {
                UIHeroBattleController.Instance?.AutoActiveSwitch((r) =>
                {
                    /*if (r == HeroNameAction.SwithBtn)
                    {
                        SwitchState(SwitchHeroState);

                        return;
                    }*/
                    return;
                }, isMain);
            }

            if (!isInSwitchAction && !isInSkillAction && IsInSkillRange(baseStatData.AttackRange*1.5f))
            {
                UIHeroBattleController.Instance?.AutoActiveSkill((r) =>
                {
                    switch (r)
                    {
                        case HeroNameAction.Skill1Btn:
                            //DoIntoSkill(HeroSkills.Skill1, EndAction);
                            return 1;
                        case HeroNameAction.Skill2Btn:
                            //DoIntoSkill(HeroSkills.Skill2, EndAction);
                            return 1;
                        case HeroNameAction.Skill3Btn:
                            //DoIntoSkill(HeroSkills.Skill3, EndAction);
                            return 1;
                        case HeroNameAction.Skill4Btn:
                            //DoIntoSkill(HeroSkills.Skill4, EndAction);
                            return 1;
                        case HeroNameAction.Skill5Btn:
                            //DoIntoSkill(HeroSkills.Skill5, EndAction);
                            return 1;
                        case HeroNameAction.None:
                        default:
                            return 0;
                    }
                }, isMain);
            }
        }

        private void LateUpdate()
        {
            /*if (monsterStarget == null)
            {
                var renderer = hero.GetComponent<MeshRenderer>();
                renderer.sortingOrder = 0;
                hero.CustomMaterialOverride.Clear();
                return;
            }

            bool isBehind = hero.transform.position.z > monsterStarget.transform.position.z;

            if (isBehind)
            {
                var renderer = hero.GetComponent<MeshRenderer>();
                renderer.sortingOrder = 2;
                hero.CustomMaterialOverride.Clear();
                hero.CustomMaterialOverride.Add(originalMaterial, hideMat);
            }
            else
            {
                var renderer = hero.GetComponent<MeshRenderer>();
                renderer.sortingOrder = 0;
                hero.CustomMaterialOverride.Clear();
            }*/
        }

        public int GetHeroId()
        {
            return heroId;
        }

        public void ResetHeroData()
        {
            InitHeroData();
            SwitchState(SpawnState);
            MonsterTarget = null;
            transform.position = heroSpawnPosition;
        }

        private void RegisterBattleResult()
        {
            TopMainView.Instance?.GetBattleResultIntance().RegisterConfirmAction(() =>
            {
                SwitchState(WinState);
            });
        }

        private void InitUIHeroBattle()
        {
            UIHeroBattleController.Instance?.SetPlayerHeroInstance(this, isMain, heroId);
            UIHeroBattleController.Instance?.RegisterHeroSwitch(ChangeToMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSwitchBtn, null, 5, false, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSkillBtn, null, 5, false, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill1Btn, () => DoIntoSkill(HeroSkills.Skill1, EndAction), intervalSkill.intervalSkill1, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill2Btn, () => DoIntoSkill(HeroSkills.Skill2, EndAction), intervalSkill.intervalSkill2, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill3Btn, () => DoIntoSkill(HeroSkills.Skill3, EndAction), intervalSkill.intervalSkill3, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill4Btn, () => DoIntoSkill(HeroSkills.Skill4, EndAction), intervalSkill.intervalSkill4, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill5Btn, () => DoIntoSkill(HeroSkills.Skill5, EndAction), intervalSkill.intervalSkill5, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.SwithBtn, () => SwitchState(SwitchHeroState), intervalSkill.intervalSwitch, true, isMain);
        }

        private void ChangeToMain(int hid)
        {
            isPriorityNearTarget = !isPriorityNearTarget;
            SetTargetPos();
        }

        public void DoShakeCam(float dur, int viration, ShakeType shakeType)
        {
            playerCamController?.ShakeCamera(dur, viration, shakeType);
        }

        public void OnReceiveDamage(float damage, Action endAct, MonsterScrepController mcc)
        {
            if(mcc != null && mcc.IsDead && !mcc.IsBoss()) SetTarget(mcc);
        }

        public void SetTarget(MonsterScrepController eTarget)
        {
            _monsterTarget = eTarget;
        }

        public Vector3 GetNearestMonster()
        {
            if (IsValidTarget()) return _monsterTarget.transform.position;

            return transform.position;
        }

        public void DoIdleCallback()
        {
            if (baseStatData.IdleStateTime > 0)
            {
                baseStatData.IdleStateTime -= Time.deltaTime;
                return;
            }

            if (_monsterTarget == null || _monsterTarget.IsDead)
            {
                SetTarget(pvEBattleController?.GetNearestMonster(targetPos));
            }

            if (IsValidTarget())
            {
                if (IsInAttackRange(baseStatData.AttackRange, _monsterTarget.transform.position))
                {
                    SwitchState(AttackState);
                }
                else
                {
                    SwitchState(MoveState);
                }
            }
            else
                ResetIdleStateTime();
        }

        private bool IsInSkillRange(float rangeAttack)
        {
            if (_monsterTarget == null) return false;

            return base.IsInAttackRange(rangeAttack, _monsterTarget.transform.position);
        }

        private bool IsBossInAttackRange(float rangeAttack, Vector3 target)
        {
            var isValidX = Mathf.Pow(transform.position.x - target.x, 2) <= rangeAttack * rangeAttack;
            var isValidZ = Mathf.Pow(transform.position.z - target.z, 2) <= rangeAttack && transform.position.z <= target.z;

            return isValidX && isValidZ;
        }

        private bool IsCreepInAttackRange(float rangeAttack, Vector3 target)
        {
            var isValidX = Mathf.Pow(transform.position.x - target.x, 2) <= rangeAttack*rangeAttack;
            var isValidZ = Mathf.Pow(transform.position.z - target.z, 2) <= rangeAttack;

            return isValidX && isValidZ;
        }

        public override bool IsInAttackRange(float rangeAttack, Vector3 target)
        {
            if (!_monsterTarget.IsBoss())
            {
                return IsCreepInAttackRange(rangeAttack,target);
            }
            else
            {
                return IsBossInAttackRange(rangeAttack,target);
            }
        }

        public void DoMoveCallBack()
        {
            if (_monsterTarget == null || _monsterTarget.IsDead)
            {
                SetTarget(pvEBattleController?.GetNearestMonster(targetPos));
                if (_monsterTarget == null) SwitchState(IdleState);

                return;
            }

            DoMoveToTarget();

            if (IsInAttackRange(baseStatData.AttackRange, _monsterTarget.transform.position))
            {
                SwitchState(AttackState);
            }
        }

        private void DoMoveToTarget()
        {
            if(_monsterTarget == null) return;

            if ((transform.position - _monsterTarget.transform.position).sqrMagnitude > distFlashConst*distFlashConst)
            {
                SwitchState(FlashState);
            }
            
            var isRight = transform.position.x < _monsterTarget.transform.position.x;
            var offset = GetMonsterOffset(isRight);
            var pos = _monsterTarget.transform.position + offset;
            isRight = transform.position.x < pos.x;
            DoRotate(isRight);
            transform.position = Vector3.MoveTowards(transform.position, pos, Time.deltaTime * 3.5f);
        }

        public Vector3 GetMonsterOffset(bool isRight)
        {
            if (_monsterTarget.IsBoss())
                return (isMain ? Vector3.right : Vector3.left) * baseStatData.AttackRange * .9f + Vector3.back * .1f;
            else
                return (!isRight ? Vector3.right:Vector3.left) * baseStatData.AttackRange * .9f;
        }

        public void ResetIdleStateTime()
        {
            baseStatData.IdleStateTime = baseStatData.IdleIntervalTime;
        }

        public bool IsLookRight()
        {
            if(_monsterTarget == null) return true;

            return _monsterTarget.transform.position.x > transform.position.x;
        }

        public bool IsValidTarget()
        {
            return _monsterTarget != null;
        }

        public override void DoIntoFlash(Action endAct)
        {
            base.DoIntoFlash(endAct);
        }

        public void EndFlashState()
        {
            if(!isValid) return;

            SwitchState(AttackState);
        }

        public override void DoIntoSkill(HeroSkills skillIdx, Action endAct)
        {
            if (!isValid) return;

            SetIsInActionState(skillIdx != HeroSkills.Switch);
            isInSwitchAction = skillIdx == HeroSkills.Switch;

            StartCoroutine(DoSkillAsync(skillIdx, endAct));
        }

        private IEnumerator DoSkillAsync(HeroSkills skillIdx, Action endAct)
        {
            yield return null;
            base.DoIntoSkill(skillIdx, endAct);
        }

        public void EndAction()
        {
            SetIsInActionState(false);
        }

        public void DoChangeState()
        {
            if(isInSwitchAction) return; 

            //if(currentState == WinState) return;
            ResetIdleStateTime();
            SwitchState(IdleState);
        }

        public void DoEndSpawn()
        {
            if(isInSwitchAction)
            {
                SwitchState(SwitchHeroState);
            }
            else
            {
                DoChangeState();
            }
        }

        public void DoEndSwitchSkill()
        {
            isInSwitchAction = false;
            DoChangeState();
        }

        public Action SetEndAction(Action act)
        {
            endAct = act;

            return endAct;
        }

        private void SetIsInActionState(bool isActive)
        {
            isInSkillAction = isActive;
        }

        public bool IsInAction()
        {
            return isInSkillAction;
        }

        public override void AttackBySpecific()
        {
            _monsterTarget?.OnReceiveDamage(baseStatData.Attack, ResetTarget, this);
        }

        public Vector3 GetFlashPos()
        {
            if(_monsterTarget == null) return transform.position;

            var pos = _monsterTarget.transform.position;
            pos.y = transform.position.y;
            return pos + new Vector3( - baseStatData.AttackRange * .9f * (isMain ? 1 : -1), 0, - baseStatData.AttackRange * .5f);
        }

        private void ResetTarget()
        {
            _monsterTarget = null;
        }

        private void OnGameLose()
        {
            SwitchState(DeathState);
            GameStatView.Instance?.battleTimerController.HideTimer();
        }

        public void AttackByArea(Vector3 pos, float attackRange = 0, float factorDam = 1)
        {
            var targets = pvEBattleController?.GetNearestMonstesInRange(pos, attackRange == 0 ? baseStatData.AttackRange : attackRange);
            if (targets == null || targets.Count == 0) return;

            foreach (var t in targets)
            {
                t.OnReceiveDamage(baseStatData.Attack*factorDam, ResetTarget, this);
            }

            Debug.Log($"hero is main {isMain} dame is {50} monsters is {targets.Count}");
        }

        public void DoWinCallback()
        {
            if(isMain)
                pvEBattleController.NextStageCallback().Forget();
        }

        public void TakeDamage(float amount, DamageType damageType = DamageType.Normal)
        {
            
        }

        public void Heal(float amount)
        {
            
        }
    }

    public class SpawnState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoSpawn(state.DoEndSpawn);
        }

        public void UpdateState(PlayerHeroController state)
        {
            
        }
    }

    public class IdleState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoIdle();
        }

        public void UpdateState(PlayerHeroController state)
        {
            state.DoIdleCallback();
        }
    }

    public class MoveState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoMove();
        }

        public void UpdateState(PlayerHeroController state)
        {
            state.DoMoveCallBack();
        }
    }

    public class FlashState : ICharacterState<PlayerHeroController>
    {
        Action endAct = null;
        public void EndState(PlayerHeroController state)
        {
            endAct = null;
        }

        public void StartState(PlayerHeroController state)
        {
            endAct = state.EndFlashState;
            state.DoIntoFlash(endAct);
        }

        public void UpdateState(PlayerHeroController state)
        {
            
        }
    }

    public class AttackState : ICharacterState<PlayerHeroController>
    {
        private Action endAct = null;
        public void EndState(PlayerHeroController state)
        {
            endAct=null;
        }

        public void StartState(PlayerHeroController state)
        {
            endAct = state.DoChangeState;
            state.DoIntoAttack(endAct);
        }

        public void UpdateState(PlayerHeroController state)
        {
        }
    }

    public class SwitchState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoSkill(HeroSkills.Switch, () => state.DoEndSwitchSkill());
        }

        public void UpdateState(PlayerHeroController state)
        {
        }
    }

    public class InjuredState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
        }

        public void UpdateState(PlayerHeroController state)
        {
        }
    }

    public class DeathState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoSkill(HeroSkills.Die, null);
        }

        public void UpdateState(PlayerHeroController state)
        {
        }
    }

    public class WinState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoWin(state.DoWinCallback);
        }

        public void UpdateState(PlayerHeroController state)
        {
        }
    }
}