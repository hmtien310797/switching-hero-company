using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;
using Scripts.UI;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.PowerUpSystem;
using UnityEngine;
using Scripts.Common;

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

        public HeroCoolingTimeDefine() { }

        public HeroCoolingTimeDefine(float timer1, float timer2, float timer3, float timer4, float timer5, float timerS) 
        { 
            intervalSkill1 = timer1; intervalSkill2 = timer2; intervalSkill3 = timer3; intervalSkill4 = timer4; intervalSkill5 = timer5;intervalSwitch = timerS;
        }
    }

    public class PlayerHeroController : BaseCharacterController<PlayerHeroController>, ICombatUnit
    {
        [SerializeField] PlayerCamController playerCamController;
        [SerializeField] SkeletonAnimation hero;
        [SerializeField] HeroUIView heroUIView;
        [SerializeField] Material hideMat;
        [SerializeField] private HeroProgressionRuntimeBridge progressionBridge;
        [SerializeField] float distFlashConst = 5;
        [SerializeField] float distSkillRange = 5;
        [SerializeField] float intervalSwitch = 20;
        [SerializeField] float switchArea = 5;

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
        [SerializeField]
        private bool isMain = false;
        private Transform partnerTrans = null;
        private HeroDataSO baseHeroData;
        private HeroCoolingTimeDefine intervalSkill = null; 
        private bool isPriorityNearTarget = false;
        private Vector3 targetPos = Vector3.zero;
        private FollowHeroController followHeroController;
        private Dictionary<SkillSlot, int> skillIdDict = new Dictionary<SkillSlot, int>();
        
        public Dictionary<SkillSlot, int> SkillIdDict => skillIdDict;
        public MonsterScrepController MonsterTarget { get => _monsterTarget; set => _monsterTarget = value; }
        public FollowHeroController FollowHeroController { get => followHeroController; set => followHeroController = value; }
        public HeroClass HeroClass { get; private set; }
        public Sprite HeroIcon { get; private set; }
        private Transform skillRootTrans;
        public bool IsMainHero => isMain;

        protected override void Awake()
        {
            base.Awake();
            
            GameEventManager.Subscribe(GameEvents.OnStageCleared, RegisterBattleResult);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnGameLose);
        }

        public void InitHero(HeroDataSO data, PvEBattleController pbc, PlayerCamController pcc, Transform soTrans,
            Transform partnerTrans, FollowHeroController fHc, HeroClass heroClass, bool isSwitch = false, bool isMainHero = false)
        {
            skillRootTrans = soTrans;
            isMain = isMainHero;
            isPriorityNearTarget = isMain;
            isValid = true;
            heroId = data.Id;
            pvEBattleController = pbc;
            playerCamController = pcc;
            isInSwitchAction = false;
            isInSkillAction = false;
            baseHeroData = data;
            followHeroController = fHc;
            HeroClass = data.HeroClass;
            HeroIcon = data.PortraitIcon;

            SetPartner(partnerTrans);
            var skillIds = UserDataCache.Instance.GetSelectedSkillIdsByHeroId(heroId);
            SetIntervalSkills(skillIds);
            playerCamController?.InitCam(transform, isMain);
            InitSkill(skillIds, soTrans);
            SetTargetPos();
            InitHeroData();
            SwitchState(SpawnState);
            InitUIHeroBattle();
        }

        public HeroUIView UISprite => heroUIView;

        public float GetSwitchArea => switchArea;

        public void GotoInValid()
        {
            isValid = false;
        }

        public bool IsValid()
        {
            return isValid;
        }

        private void SetTargetPos()
        {
            targetPos = isPriorityNearTarget ? GroupFlashController.Instance?.GetNearestPoint()?? transform.position : GroupFlashController.Instance?.GetFarestPoint()??transform.position;
            Debug.Log($"target pos is: {isMain} and pos is: {targetPos}");
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
                intervalSkill.intervalSwitch = intervalSwitch;
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
            intervalSkill.intervalSwitch = intervalSwitch;
        }

        private float GetCooldownBySlot(SkillSlot slot)
        {
            if (!skillIdDict.TryGetValue(slot, out var skillId))
                return 10f;

            return MasterDataCache.Instance.GetSkillDataById(skillId)?.CooldownTime ?? 10f;
        }

        private void OnChageSkillEvent(int slotId, int skillId)
        {
            switch(slotId)
            {
                case 0:
                    skillIdDict[(SkillSlot)slotId] = skillId;
                    intervalSkill.intervalSkill1 = MasterDataCache.Instance.GetSkillDataById(skillId)?.CooldownTime ?? 10f;
                    UIHeroBattleController.Instance?.ChangeSkillByIdx(HeroNameAction.Skill1Btn, intervalSkill.intervalSkill1, heroId);
                    ChangeSkillBySlot(slotId, skillId);
                    break;
                case 1:
                    ChangeSkillCallback(slotId, skillId, ref intervalSkill.intervalSkill2);
                    break;
                case 2:
                    ChangeSkillCallback(slotId, skillId, ref intervalSkill.intervalSkill3);
                    break;
                case 3:
                    ChangeSkillCallback(slotId, skillId, ref intervalSkill.intervalSkill4);
                    break;
                case 4:
                    ChangeSkillCallback(slotId, skillId, ref intervalSkill.intervalSkill5);
                    break;
            }
        }

        private void ChangeSkillCallback(int slotId, int skillId, ref float interval)
        {
            skillIdDict[(SkillSlot)slotId] = skillId;
            interval = MasterDataCache.Instance.GetSkillDataById(skillId)?.CooldownTime ?? 10f;
            UIHeroBattleController.Instance?.ChangeSkillByIdx(HeroNameAction.Skill1Btn, interval, heroId);
            ChangeSkillBySlot(slotId, skillId);
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
        }

        private void Start()
        {
            originalMaterial = hero.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;
        }

        public override void Update()
        {
            if (!isValid) return;
            base.Update();

            if (!isInSkillAction && !isInSwitchAction && IsInSkillRange(distSkillRange))
            {
                UIHeroBattleController.Instance?.AutoActiveSwitch((r) =>
                {
                    return;
                }, isMain);
            }

            if (!isInSwitchAction && !isInSkillAction && IsInSkillRange(distSkillRange))
            {
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
            isValid = true;
            gameObject.SetActive(true);
            InitHeroData();
            SwitchState(SpawnState);
            MonsterTarget = null;
            transform.position = heroSpawnPosition;
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
            UIHeroBattleController.Instance?.SetPlayerHeroInstance(this, isMain, heroId, skillIdDict);
            UIHeroBattleController.Instance?.RegisterHeroSwitch(ChangeToMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSwitchBtn, null, 5, false, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSkillBtn, null, 5, false, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill1Btn, () => DoIntoSkill(HeroSkills.Skill1, EndAction), intervalSkill.intervalSkill1, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill2Btn, () => DoIntoSkill(HeroSkills.Skill2, EndAction), intervalSkill.intervalSkill2, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill3Btn, () => DoIntoSkill(HeroSkills.Skill3, EndAction), intervalSkill.intervalSkill3, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill4Btn, () => DoIntoSkill(HeroSkills.Skill4, EndAction), intervalSkill.intervalSkill4, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill5Btn, () => DoIntoSkill(HeroSkills.Skill5, EndAction), intervalSkill.intervalSkill5, true, isMain);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.SwithBtn, () =>
            {
                if(gameObject.activeInHierarchy)
                SwitchState(SwitchHeroState);
            }, intervalSkill.intervalSwitch, true, isMain);
        }

        private void ChangeToMain(int hid)
        {
            if (!isValid) return;

            isPriorityNearTarget = !isPriorityNearTarget;
            SetTargetPos();
            //if(isMain) pvEBattleController?.SwitchHero(heroId);
        }

        public bool GetPriorityNearTarget() { return isPriorityNearTarget; }

        public void SetPriorityNearTarget(bool isPriority)
        {
            isPriorityNearTarget = isPriority; 
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

            //if (_monsterTarget == null || _monsterTarget.IsDead)
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

        public bool IsAfterTarget ()
        {
            if (_monsterTarget == null || HeroClass != HeroClass.Archer) return true;

            return transform.position.z > _monsterTarget.transform.position.z;
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
            if (!isValid || !gameObject.activeInHierarchy) return;

            SetIsInActionState(skillIdx != HeroSkills.Switch);
            isInSwitchAction = skillIdx == HeroSkills.Switch;

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
            if(isInSwitchAction) return; 

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
            Debug.Log($"active skill is: {isActive}");
        }

        public bool IsInAction()
        {
            return isInSkillAction || isInSwitchAction;
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
                return pos + new Vector3( - baseStatData.AttackRange * .9f * (isMain ? 1 : -1), 0, - baseStatData.AttackRange * .5f);
            else
            {
                var posZ = baseStatData.AttackRange * .9f * (_monsterTarget.IsBoss() ? -1 : (isPriorityNearTarget ? 1 : -1));
                return pos + new Vector3(baseStatData.AttackRange * .35f * (isLeft ? -1 : 1), 0, posZ);
            }
        }

        private void ResetTarget()
        {
            _monsterTarget = null;
        }

        private void OnGameLose()
        {
            isValid = false;
            SwitchState(DeathState);
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

            Debug.Log($"hero is main {isMain} dame is {factorDam} monsters is {targets.Count}");
        }

        public void DoWinCallback()
        {
            if(isMain)
                pvEBattleController.NextStageCallback().Forget();
        }

        public void DoLoseCallback()
        {
            return;
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
            if (heroId <= 0)
                return;

            var selectedSkillIds = UserDataCache.Instance.GetSelectedSkillIdsByHeroId(heroId);
            SetIntervalSkills(selectedSkillIds);
            InitSkill(selectedSkillIds, skillRootTrans);
            InitUIHeroBattle();
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
        private Action endAct = null;
        public void EndState(PlayerHeroController state)
        {
            endAct = null;
        }

        public void StartState(PlayerHeroController state)
        {
            endAct = state.DoEndSwitchSkill;
            state.DoIntoSkill(HeroSkills.Switch, endAct);
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
            state.DoIntoSkill(HeroSkills.Die, state.DoLoseCallback);            
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