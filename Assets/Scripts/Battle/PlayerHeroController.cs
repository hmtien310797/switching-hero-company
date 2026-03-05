using Cysharp.Threading.Tasks;
using Scripts.UI;
using Spine.Unity;
using System;
using UnityEngine;
using static Scripts.Battle.PvEBattleController;

namespace Scripts.Battle
{
    public class UIHeroBehavior
    {
        public bool IsAutoSwitch = false;
        public bool IsAutoSkills = false;
        public int SwitchIdx = -1;
        public int SkillIdx = -1;
    }

    public class PlayerHeroController : BaseCharacterController<PlayerHeroController>
    {
        [SerializeField] PlayerCamController playerCamController;
        [SerializeField] SkeletonAnimation hero;
        [SerializeField] Material hideMat;

        private Material originalMaterial;

        private PvEBattleController pvEBattleController;
        private MonsterScrepController monsterStarget;
        private ScrepData heroData = new ScrepData();

        public SpawnState SpawnState = new SpawnState();
        public IdleState IdleState = new IdleState();
        public MoveState MoveState = new MoveState();
        public AttackState AttackState = new AttackState();
        public SwitchState SwitchHeroState = new SwitchState();
        public InjuredState InjureedState = new InjuredState();
        public DeathState DeathState = new DeathState();
        public WinState WinState = new WinState();

        private Action endAct = null;
        private bool isInSkillAction = false;
        private bool isInSwitchAction = false;
        private readonly Vector3 heroSpawnPosition = new Vector3(0f, 0f, 12f);

        public MonsterScrepController MonsterStarget { get => monsterStarget; set => monsterStarget = value; }

        public void InitHero(PvEBattleController pbc)
        {
            pvEBattleController = pbc;
            playerCamController.InitCam(transform);

            InitHeroData();
            SwitchState(SpawnState);
            InitUIHeroBattle();
            ResiterBattleResult();
        }

        public void ResetHeroData()
        {
            InitHeroData();
            SwitchState(SpawnState);
            transform.position = heroSpawnPosition;
        }

        private void ResiterBattleResult()
        {
            TopMainView.Instance?.GetBattleResultIntance().RegisterConfirmAction(() => pvEBattleController.NextStageCallback().Forget());
        }

        private void InitUIHeroBattle()
        {
            UIHeroBattleController.Instance?.SetPlayerHeroInstance(this);

            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSwitchBtn, null, 5);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.AutoSkillBtn, null, 5);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill1Btn, () => DoIntoSkill(HeroSkills.Skill1, EndAction), 10);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill2Btn, () => DoIntoSkill(HeroSkills.Skill2, EndAction), 15);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill3Btn, () => DoIntoSkill(HeroSkills.Skill3, EndAction), 12);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill4Btn, () => DoIntoSkill(HeroSkills.Skill4, EndAction), 18);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.Skill5Btn, () => DoIntoSkill(HeroSkills.Skill5, EndAction), 23);
            UIHeroBattleController.Instance?.RegisterActionByIdx(HeroNameAction.SwithBtn, () => SwitchState(SwitchHeroState), 45);
        }

        private void InitHeroData()
        {
            heroData.Health = 1000;
            heroData.RemainHealth = heroData.Health;
            heroData.RangeAttack = 2.5f;
            heroData.IdleIntervalTime = 1f;
            heroData.IdleStateTime = heroData.IdleIntervalTime;
        }

        private void Start()
        {
            originalMaterial = hero.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;
        }

        public override void Update()
        {
            base.Update();

            if (monsterStarget != null && !isInSwitchAction && !isInSkillAction && IsInAttackRange(heroData.RangeAttack, monsterStarget.transform.position))
            {
                UIHeroBattleController.Instance?.AutoActiveSkill((r) =>
                {
                    switch (r)
                    {
                        case HeroNameAction.Skill1Btn:
                            DoIntoSkill(HeroSkills.Skill1, EndAction);
                            return 1;
                        case HeroNameAction.Skill2Btn:
                            DoIntoSkill(HeroSkills.Skill2, EndAction);
                            return 1;
                        case HeroNameAction.Skill3Btn:
                            DoIntoSkill(HeroSkills.Skill3, EndAction);
                            return 1;
                        case HeroNameAction.Skill4Btn:
                            DoIntoSkill(HeroSkills.Skill4, EndAction);
                            return 1;
                        case HeroNameAction.Skill5Btn:
                            DoIntoSkill(HeroSkills.Skill5, EndAction);
                            return 1;
                        case HeroNameAction.None:
                        default:
                            //SwitchState(AttackState);
                            return 0;
                    }
                });
            }

            if(!isInSkillAction && !isInSwitchAction && monsterStarget != null)
            {
                UIHeroBattleController.Instance?.AutoActiveSwitch((r) =>
                {
                    if (r == HeroNameAction.SwithBtn)
                    {
                        SwitchState(SwitchHeroState);
                    }
                });
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

        public override void OnReceiveDamage(float damage, Action endAct)
        {
            //base.OnReceiveDamage(damage, endAct);
        }

        public void SetTarget(MonsterScrepController eTarget)
        {
            monsterStarget = eTarget;
        }

        public void DoIdleCallback()
        {
            if (heroData.IdleStateTime > 0)
            {
                heroData.IdleStateTime -= Time.deltaTime;
                return;
            }

            if(monsterStarget == null || monsterStarget.IsDead())
                SetTarget(pvEBattleController?.GetNearestMonster(transform.position));

            if (IsConditionValid())
            {
                if (IsInAttackRange(heroData.RangeAttack, monsterStarget.transform.position))
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

        public void DoMoveCallBack()
        {
            if (monsterStarget == null || monsterStarget.IsDead())
            {
                SetTarget(pvEBattleController?.GetNearestMonster(transform.position));
                return;
            }

            DoMoveToTarget();

            if (IsInAttackRange(heroData.RangeAttack, monsterStarget.transform.position))
            {
                SwitchState(AttackState);
            }
        }

        private void DoMoveToTarget()
        {
            if(monsterStarget == null) return;

            var isRight = transform.position.x < monsterStarget.transform.position.x;
            DoRotate(isRight);
            transform.position = Vector3.MoveTowards(transform.position, monsterStarget.transform.position, Time.deltaTime * 3.5f);
        }

        public void ResetIdleStateTime()
        {
            heroData.IdleStateTime = heroData.IdleIntervalTime;
        }

        public bool IsLookRight()
        {
            return monsterStarget.transform.position.x > transform.position.x;
        }

        public bool IsConditionValid()
        {
            return monsterStarget != null;
        }

        public override void DoIntoSkill(HeroSkills skillIdx, Action endAct)
        {
            SetIsInActionState(skillIdx != HeroSkills.Switch);
            isInSwitchAction = skillIdx == HeroSkills.Switch;

            base.DoIntoSkill(skillIdx, endAct);
        } 

        public void EndAction()
        {
            SetIsInActionState(false);
        }

        public void DoChangeState()
        {
            if(isInSwitchAction) return; 

            if(pvEBattleController?.State == BattleState.StageCleared) return;
            ResetIdleStateTime();
            SwitchState(IdleState);
        }

        public void DoEndSwitchSkill()
        {
            isInSwitchAction = false;
            DoChangeState();
        }

        public Action SetEndAction(Action act)
        {
            //SetIsInActionState(true);
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
            MonsterStarget?.OnReceiveDamage(50, ResetTarget);
        }

        private void ResetTarget()
        {
            if(monsterStarget?.IsBoss()?? false)
            {
                TopMainView.Instance?.GetBattleTimerIntance().HideTimer();
                SwitchState(WinState);
                pvEBattleController?.NotifyBossDeath();
            }
            monsterStarget = null;
        }

        public void AttackByArea(Vector3 pos, float factorRange = 2, float factorDam = 1)
        {
            var targets = pvEBattleController?.GetNearestMonstesInRange(pos, heroData.RangeAttack*factorRange);
            if (targets == null || targets.Count == 0) return;

            foreach (var t in targets)
            {
                t.OnReceiveDamage(30*factorDam, ResetTarget);
            }
        }

        public void DoWinCallback()
        {
            TopMainView.Instance?.GetBattleResultIntance().ShowBattleResult();
        }
    }

    public class SpawnState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoSpawn(state.DoChangeState);
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

    public class AttackState : ICharacterState<PlayerHeroController>
    {
        public void EndState(PlayerHeroController state)
        {
        }

        public void StartState(PlayerHeroController state)
        {
            state.DoIntoAttack(state.DoChangeState);
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