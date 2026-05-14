using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Runtime;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using Scripts.Battle;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;

namespace Battle
{
    public class HeroCoolingTimeDefine
    {
        public float intervalSkill1;
        public float intervalSkill2;
        public float intervalSkill3;
        public float intervalSkill4;
        public float intervalSkill5;
        public float intervalSwitch;

        public HeroCoolingTimeDefine() { }

        public HeroCoolingTimeDefine(float timer1, float timer2, float timer3, float timer4, float timer5, float timerS) 
        { 
            intervalSkill1 = timer1; intervalSkill2 = timer2; intervalSkill3 = timer3; intervalSkill4 = timer4; intervalSkill5 = timer5;intervalSwitch = timerS;
        }
    }

    public class PlayerHeroController : BaseCharacterController<PlayerHeroController>, ICombatUnit
    {
        [SerializeField] SkeletonAnimation hero;
        [SerializeField] private HeroProgressionRuntimeBridge progressionBridge;
        [SerializeField] private HeroEquipmentRuntimeBridge equipmentBridge;
        [SerializeField] private HeroBehaviorParams behaviorParams;
        [SerializeField] float distFlashConst = 5;
        [SerializeField] float distSkillRange = 5;
        [SerializeField] float intervalSwitch = 20;
        [SerializeField] float switchArea = 5;

        private Material originalMaterial;

        private PlayerCamController playerCamController;
        private PvEBattleController pvEBattleController;
        private MonsterScrepController _monsterTarget;
        private HeroDataSO baseHeroData;
        private HeroCoolingTimeDefine intervalSkill = null; 
        private Vector3 targetPos = Vector3.zero;
        private FollowHeroController followHeroController;
        private Dictionary<SkillSlot, int> skillIdDict = new Dictionary<SkillSlot, int>();
        private Transform skillRootTrans;
        private SpawnState SpawnState = new SpawnState();
        private IdleState IdleState = new IdleState();
        private MoveState MoveState = new MoveState();
        private FlashState FlashState = new FlashState();
        private AttackState AttackState = new AttackState();
        private SwitchState SwitchHeroState = new SwitchState();
        private DeathState DeathState = new DeathState();
        private WinState WinState = new WinState();

        [ShowInInspector]
        public Dictionary<SkillSlot, int> SkillIdDict => skillIdDict;
        public MonsterScrepController MonsterTarget { get => _monsterTarget;
            private set => _monsterTarget = value; }
        public FollowHeroController FollowHeroController { get => followHeroController; set => followHeroController = value; }
        public HeroClass HeroClass { get; private set; }
        public Sprite HeroIcon { get; private set; }
        public int HeroIndex { get; private set; } = -1;

        protected override void Awake()
        {
            base.Awake();
            GameEventManager.Subscribe(GameEvents.OnStageCleared, RegisterBattleResult);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnGameLose);
        }

        public void InitHero(HeroDataSO data, PvEBattleController pbc, PlayerCamController pcc, Transform soTrans, FollowHeroController fHc, int index)
        {
            HeroIndex = index;
            skillRootTrans = soTrans;
            behaviorParams.IsValid = true;
            behaviorParams.HeroId = data.Id;
            pvEBattleController = pbc;
            playerCamController = pcc;
            behaviorParams.IsInSwitchAction = false;
            behaviorParams.IsInSkillAction = false;
            baseHeroData = data;
            followHeroController = fHc;
            followHeroController.SetFollowTarget(transform);
            HeroClass = data.HeroClass;
            HeroIcon = data.PortraitIcon;
            
            var skillIds = UserDataCache.Instance.GetEquippedSkills(behaviorParams.HeroId);
            SetIntervalSkills(skillIds);
            playerCamController?.InitCam(transform, HeroIndex);
            InitSkill(skillIds, soTrans);
            SetTargetPos();
            InitHeroData();
            SwitchState(SpawnState);
            InitUIHeroBattle();
        }

        public float GetSwitchArea => behaviorParams.SwitchArea;
        
        public bool IsValid()
        {
            return behaviorParams.IsValid;
        }

        private void SetTargetPos()
        {
            targetPos = behaviorParams.IsPriorityNearTarget ? GroupFlashController.Instance?.GetNearestPoint()?? transform.position : GroupFlashController.Instance?.GetFarestPoint()??transform.position;
        }

        private void SetIntervalSkills(List<int> skillIds)
        {
            skillIdDict.Clear();

            if (skillIds == null || skillIds.Count == 0)
            {
                intervalSkill ??= new HeroCoolingTimeDefine();
                intervalSkill.intervalSkill1 = 10f;
                intervalSkill.intervalSkill2 = 10f;
                intervalSkill.intervalSkill3 = 10f;
                intervalSkill.intervalSkill4 = 10f;
                intervalSkill.intervalSkill5 = 10f;
                intervalSkill.intervalSwitch = behaviorParams.IntervalSwitch;
                return;
            }

            for (int i = 0; i < skillIds.Count && i < 5; i++)
            {
                skillIdDict[(SkillSlot)i] = skillIds[i];
            }

            intervalSkill ??= new HeroCoolingTimeDefine();
            intervalSkill.intervalSkill1 = GetCooldownBySlot(SkillSlot.Slot1);
            intervalSkill.intervalSkill2 = GetCooldownBySlot(SkillSlot.Slot2);
            intervalSkill.intervalSkill3 = GetCooldownBySlot(SkillSlot.Slot3);
            intervalSkill.intervalSkill4 = GetCooldownBySlot(SkillSlot.Slot4);
            intervalSkill.intervalSkill5 = GetCooldownBySlot(SkillSlot.Slot5);
            intervalSkill.intervalSwitch = behaviorParams.IntervalSwitch;
        }

        private float GetCooldownBySlot(SkillSlot slot)
        {
            if (!skillIdDict.TryGetValue(slot, out var skillId))
                return 10f;

            return MasterDataCache.Instance.GetSkillDataById(skillId)?.CooldownTime ?? 10f;
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
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.BindPlayer(Stats);
            }
            
            if (progressionBridge == null)
                progressionBridge = GetComponent<HeroProgressionRuntimeBridge>();

            if (progressionBridge != null)
            {
                progressionBridge.Setup(baseHeroData, this);
                progressionBridge.RefreshFromProgression();
            }
            
            if (equipmentBridge == null)
                equipmentBridge = GetComponent<HeroEquipmentRuntimeBridge>();

            if (equipmentBridge != null)
            {
                equipmentBridge.Setup(this);
                equipmentBridge.RefreshFromEquipment();
            }
        }

        public override void Update()
        {
            if (!behaviorParams.IsValid) return;
            base.Update();

            if (!behaviorParams.IsInSkillAction && !behaviorParams.IsInSwitchAction && IsInSkillRange(behaviorParams.DistSkillRange))
            {
                return;
                UIHeroBattleController.Instance?.AutoActiveSwitch(null, behaviorParams.IsMain);
            }

            if (!behaviorParams.IsInSwitchAction && !behaviorParams.IsInSkillAction && IsInSkillRange(behaviorParams.DistSkillRange))
            {
                return;
                UIHeroBattleController.Instance?.AutoActiveSkill((r) =>
                {
                    switch (r)
                    {
                        case HeroNameAction.Skill1Btn:
                            return 1;
                        case HeroNameAction.Skill2Btn:
                            return 1;
                        case HeroNameAction.Skill3Btn:
                            return 1;
                        case HeroNameAction.Skill4Btn:
                            return 1;
                        case HeroNameAction.Skill5Btn:
                            return 1;
                        case HeroNameAction.None:
                        default:
                            return 0;
                    }
                }, HeroIndex);
            }
        }

        public int GetHeroId()
        {
            return behaviorParams.HeroId;
        }

        public void ResetHeroData()
        {
            behaviorParams.IsValid = true;
            gameObject.SetActive(true);
            InitHeroData();
            SwitchState(SpawnState);
            MonsterTarget = null;
            transform.position = behaviorParams.HeroSpawnPosition;
        }

        private void RegisterBattleResult()
        {
            TopMainView.Instance?.GetBattleResultIntance().RegisterConfirmAction(() =>
            {
                if(gameObject.activeInHierarchy)
                    SwitchState(WinState);
            });
        }

        private void InitUIHeroBattle()
        {
            UIHeroBattleController.Instance?.SetPlayerHeroInstance(this, HeroIndex, behaviorParams.HeroId, skillIdDict);
            UIHeroBattleController.Instance?.RegisterHeroSwitch(ChangeToMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSwitchBtn, null, 0, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSkillBtn, null, 0, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill1Btn, () => DoIntoSkill(HeroSkills.Skill1, EndAction), intervalSkill.intervalSkill1, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill2Btn, () => DoIntoSkill(HeroSkills.Skill2, EndAction), intervalSkill.intervalSkill2, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill3Btn, () => DoIntoSkill(HeroSkills.Skill3, EndAction), intervalSkill.intervalSkill3, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill4Btn, () => DoIntoSkill(HeroSkills.Skill4, EndAction), intervalSkill.intervalSkill4, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill5Btn, () => DoIntoSkill(HeroSkills.Skill5, EndAction), intervalSkill.intervalSkill5, HeroIndex);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.SwitchBtn, () =>
            {
                if(gameObject.activeInHierarchy) SwitchState(SwitchHeroState);
            }, intervalSkill.intervalSwitch, HeroIndex);
        }

        private void ChangeToMain(int hid)
        {
            if (!behaviorParams.IsValid) return;

            behaviorParams.IsPriorityNearTarget = !behaviorParams.IsPriorityNearTarget;
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

        public bool IsExistTargetInRange()
        {
            return pvEBattleController?.IsExistMonsterInRange(transform.position, behaviorParams.DistSkillRange) ?? false;
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

            SetTarget(pvEBattleController?.GetNearestMonster(targetPos));

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

        public bool IsInSkillRange()
        {
            var nearestMonsterPos = GetNearestMonster();
            if (_monsterTarget == null) return false;

            return base.IsInAttackRange(behaviorParams.DistSkillRange, _monsterTarget.transform.position);
        }

        private bool IsBossInAttackRange(float rangeAttack, Vector3 target)
        {
            if (HeroClass == HeroClass.Assassin || HeroClass == HeroClass.Warrior)
            {
                var isValidX = Mathf.Pow(transform.position.x - target.x, 2) <= rangeAttack * rangeAttack;
                var isValidZ = Mathf.Pow(transform.position.z - target.z, 2) <= rangeAttack && transform.position.z <= target.z;

                return isValidX && isValidZ;
            }

            return (transform.position - target).sqrMagnitude <= baseHeroData.AttackRange * baseHeroData.AttackRange;
        }

        private bool IsCreepInAttackRange(float rangeAttack, Vector3 target)
        {
            if (HeroClass == HeroClass.Assassin || HeroClass == HeroClass.Warrior)
            {
                var isValidX = Mathf.Pow(transform.position.x - target.x, 2) <= rangeAttack * rangeAttack;
                var isValidZ = Mathf.Pow(transform.position.z - target.z, 2) <= rangeAttack;
                return isValidX && isValidZ;
            }

            return (transform.position - target).sqrMagnitude <= baseHeroData.AttackRange * baseHeroData.AttackRange;
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

            if ((transform.position - _monsterTarget.transform.position).sqrMagnitude > behaviorParams.DistFlashConst * behaviorParams.DistFlashConst)
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
                return (behaviorParams.IsMain ? Vector3.right : Vector3.left) * baseStatData.AttackRange * .9f + Vector3.back * .1f;
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

        public bool IsAfterTarget ()
        {
            if (_monsterTarget == null || HeroClass != HeroClass.Archer) return true;

            return transform.position.z + 5f > _monsterTarget.transform.position.z;
        }

        public bool IsValidTarget()
        {
            return _monsterTarget != null;
        }

        public override void DoIntoFlash(Action endAct)
        {
            behaviorParams.IsInFlashAction = true;
            base.DoIntoFlash(endAct);
        }

        public void EndFlashState()
        {
            behaviorParams.IsInFlashAction = false;
            if(!behaviorParams.IsValid) return;

            SwitchState(AttackState);
        }

        public override void DoIntoSkill(HeroSkills skillIdx, Action endAct)
        {
            if (!behaviorParams.IsValid || !gameObject.activeInHierarchy) return;
            if(skillIdx == HeroSkills.Die) behaviorParams.IsValid = false;

            SetIsInActionState(skillIdx != HeroSkills.Switch);
            behaviorParams.IsInSwitchAction = skillIdx == HeroSkills.Switch;

            DoSkillAsync(skillIdx, endAct);
        }

        private void DoSkillAsync(HeroSkills skillIdx, Action endAct)
        {
            base.DoIntoSkill(skillIdx, endAct);
        }

        public void EndAction()
        {
            SetIsInActionState(false);
        }

        public void DoChangeState()
        {
            if(behaviorParams.IsInSwitchAction || !behaviorParams.IsValid) return; 

            ResetIdleStateTime();
            SwitchState(IdleState);
        }

        public void DoEndSpawn()
        {
            if(behaviorParams.IsInSwitchAction)
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
            behaviorParams.IsInSwitchAction = false;
            DoChangeState();
        }

        public void ResetFlashParam()
        {
            behaviorParams.IsInFlashAction = false;
        }

        private void SetIsInActionState(bool isActive)
        {
            behaviorParams.IsInSkillAction = isActive;
        }

        public bool IsInAction()
        {
            return behaviorParams.IsInSkillAction || behaviorParams.IsInSwitchAction;
        }

        public bool IsInFlash()
        {
            return behaviorParams.IsInFlashAction;
        }

        public override void AttackBySpecific()
        {
            _monsterTarget?.OnReceiveDamage(1, ResetTarget, this);
        }

        public Vector3 GetMonsterPos()
        {
            if(_monsterTarget == null)
            {
                SetTarget(pvEBattleController.GetNearestMonster(transform.position));
            }

            return _monsterTarget?.transform.position?? transform.position;
        }    

        public Vector3 GetFlashPos()
        {
            if(_monsterTarget == null) return transform.position;

            var pos = _monsterTarget.transform.position;
            pos.y = transform.position.y;
            var isLeft = transform.position.x < pos.x;
            if(HeroClass is HeroClass.Assassin or HeroClass.Warrior)
                return pos + new Vector3( - baseStatData.AttackRange * .9f * (HeroIndex == 0 ? 1 : -1), 0, - baseStatData.AttackRange * .5f);
            else
            {
                var posZ = baseStatData.AttackRange * .9f * (_monsterTarget.IsBoss() ? -1 : (behaviorParams.IsPriorityNearTarget ? 1 : -1));
                return pos + new Vector3(baseStatData.AttackRange * .35f * (isLeft ? -1 : 1), 0, posZ);
            }
        }

        private void ResetTarget()
        {
            _monsterTarget = null;
        }

        private void OnGameLose()
        {
            SwitchState(DeathState);
            //isValid = false;
            GameStatView.Instance?.battleTimerController.HideTimer();
        }

        public void AttackByArea(Vector3 pos, float attackRange = 0, float factorDam = 1)
        {
            var targets = pvEBattleController?.GetNearestMonstesInRange(pos, attackRange == 0 ? baseStatData.AttackRange : attackRange);
            if (targets == null || targets.Count == 0) return;

            foreach (var t in targets)
            {
                t.OnReceiveDamage(factorDam, ResetTarget, this);
            }

            Debug.Log($"hero is main {gameObject.name} dame is {factorDam} monsters is {targets.Count}");
        }

        public void AttackSpecificByArea(Vector3 pos, out bool isDeath, float attackRange = 0, float factorDam = 1)
        {
            isDeath = false;
            var targets = pvEBattleController?.GetNearestMonstesInRange(pos, attackRange == 0 ? baseStatData.AttackRange : attackRange);
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            foreach (var t in targets)
            {
                t.OnReceiveDamage(Stats.StatModule.GetFinalStat(StatType.Atk) * factorDam, ResetTarget, this);
                if(isDeath == false && t.IsDead)
                {
                    isDeath = true;
                }
            }

            Debug.Log($"hero is main {gameObject.name} dame is {factorDam} monsters is {targets.Count}");
        }

        public Vector3 GetWeakestEnemy()
        {
            return pvEBattleController?.GetWeakestMonster(transform.position, behaviorParams.DistSkillRange) ??transform.position;
        }

        public void ChangeToPosWithFlash()
        {
            if(_monsterTarget != null && _monsterTarget.IsBoss())
            {
                transform.position = _monsterTarget.transform.position + Vector3.right * baseStatData.AttackRange * .9f;
            }
        }

        public void ChangeToPos(Vector3 pos)
        {
            transform.position = pos;
        }

        public void DoWinCallback()
        {
            if(behaviorParams.IsMain)
                pvEBattleController.NextStageCallback().Forget();
        }

        public void DoLoseCallback()
        {
            if (behaviorParams.IsMain)
                pvEBattleController?.OnStageFailed();
        }
        
        public IReadOnlyDictionary<SkillSlot, int> GetRuntimeSkillMap()
        {
            return skillIdDict;
        }

        public List<int> GetOrderedEquippedSkillIds()
        {
            var result = new List<int>(5);

            for (int i = 0; i < 5; i++)
            {
                if (skillIdDict.TryGetValue((SkillSlot)i, out var skillId) && skillId > 0)
                    result.Add(skillId);
            }

            return result;
        }
        
        public void RefreshSelectedSkillsRuntime()
        {
            var ids = UserDataCache.Instance.GetEquippedSkills(behaviorParams.HeroId);

            Debug.Log($"[HeroRuntime] Refresh skills hero={behaviorParams.HeroId} -> {string.Join(",", ids)}");

            SetIntervalSkills(ids);
            InitSkill(ids, skillRootTrans);
            InitUIHeroBattle();
        }
    }

    public class SpawnState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
            state.ResetEndAct();
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoSpawn(state.SetEndAction(state.DoEndSpawn));
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
        public void EndState(PlayerHeroController state)
        {
            state.ResetFlashParam();
            state.ResetEndAct();
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoFlash(state.SetEndAction(state.EndFlashState));
        }

        public void UpdateState(PlayerHeroController state)
        {
            
        }
    }

    public class AttackState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
            state.ResetEndAct();
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoAttack(state.SetEndAction(state.DoChangeState));
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
            state.DoIntoSkill(HeroSkills.Switch, state.SetEndAction(state.DoEndSwitchSkill));
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
            state.DoIntoSkill(HeroSkills.Die, state.SetEndAction(state.DoLoseCallback));            
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
            state.DoIntoWin(state.SetEndAction(state.DoWinCallback));
        }

        public void UpdateState(PlayerHeroController state)
        {
        }
    }
}