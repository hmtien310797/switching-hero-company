using System;
using Battle;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Runtime;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

public enum HeroStateId
{
    Idle,
    Run,
    Attack,
    Ultimate,
    Passive,
    Dead,
    Win,
    Spawn,
    BossSpawn,
    ManualMove,
    TimeOut
}

public enum HeroMoveMode
{
    Auto,
    Manual,
    FollowFormation
}

public enum HeroAttackMode
{
    Melee,
    Ranged
}

public class HeroActor : MonoBehaviour, ICombatUnit
{
    [Header("Components")]
    [SerializeField] private StatsController stats;
    [SerializeField] private HeroLocomotion locomotion;
    [SerializeField] private HeroAnimationDriver animationDriver;
    [SerializeField] private HeroSkillController skillController;
    [SerializeField] private HealthBarController healthBarController;
    [SerializeField] private HeroAutoSkillController autoSkillController;
    [SerializeField] private HeroProgressionRuntimeBridge progressionBridge;
    [SerializeField] private HeroEquipmentRuntimeBridge equipmentBridge;

    [Header("Attack")]
    [SerializeField] private HeroAttackMode attackMode = HeroAttackMode.Melee;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private HeroProjectile projectilePrefab;

    [SerializeField] private float targetSearchRange = 8f;
    [SerializeField] private float targetSearchInterval = 0.2f;

    [Header("Ability")]
    [SerializeField] private float ultimateHitNormalizedTime = 0.5f;
    [SerializeField] private float passiveHitNormalizedTime = 0.5f;
    
    [SerializeField] private GameObject winFx;
    
    [Header("Properties")]
    [field: SerializeField] public ActorType ActorType { get; private set;}

    [ShowInInspector, ReadOnly]
    private ICombatUnit currentTarget;
    [SerializeField]
    private HeroStateMachine stateMachine;
    private PvEBattleController pveBattleController;
    private HeroTeamController heroTeamController;
    private HeroDataSO heroData;
    private int attackComboIndex;
    private float nextTargetSearchTime;
    public event Action<HeroActor> OnDead;

    public HeroDataSO HeroData => heroData;

    public StatsController Stats => stats;
    public Element Element => heroData.Element;
    public HealthBarController HealthBarController => healthBarController;

    public Transform Transform => transform;

    public Vector3 Position => transform.position;

    public HeroLocomotion Locomotion => locomotion;

    public HeroAnimationDriver Anim => animationDriver;

    public ICombatUnit CurrentTarget => currentTarget;

    public HeroMoveMode MoveMode { get; private set; } = HeroMoveMode.Auto;
    public bool IsChosen { get; private set;}

    public bool IsUnderPlayerControl { get; private set; }

    public bool IsActionLocked { get; private set; }

    public bool IsDead => stats != null &&
                          stats.HealthModule != null &&
                          stats.HealthModule.IsDead;

    public float CurrentHp => stats != null && stats.HealthModule != null
        ? stats.HealthModule.CurrentHP
        : 0f;

    public float MaxHp => stats != null && stats.HealthModule != null
        ? stats.HealthModule.MaxHP
        : 0f;

    public float Attack => stats.StatModule.GetFinalStat(StatType.Atk);

    public float AttackRange => stats.StatModule.GetFinalStat(StatType.AttackRange);

    public float AttackSpeed => stats.StatModule.GetFinalStat(StatType.AttackSpeed);

    public HeroAttackMode AttackMode => attackMode;

    public HeroClass HeroClass => heroData != null ? heroData.HeroClass : HeroClass.Warrior;

    public float UltimateHitNormalizedTime => ultimateHitNormalizedTime;

    public float PassiveHitNormalizedTime => passiveHitNormalizedTime;

    public int GetHeroId() => heroData.Id;

    public Sprite HeroIcon => heroData.PortraitIcon;
    
    public HeroStateMachine StateMachine => stateMachine;

    public void SetAutoSkill(bool active)
    {
        autoSkillController.AutoCastEnabled = active;
    }

    private void Awake()
    {
        stateMachine = new HeroStateMachine(this);
    }

    private void Start()
    {
        GameEventManager.Subscribe<bool>(GameEvents.OnBossSpawnAnimationComplete, OnBossSpawnAnimationComplete);
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, (_) =>
        {
            ActiveHealthBar(false);
            stateMachine.ChangeState(HeroStateId.Win);
        });
        GameEventManager.Subscribe(GameEvents.OnStageLost, () =>
        {
            stateMachine.ChangeState(HeroStateId.Dead);
        });
    }

    private void OnDestroy()
    {
        skillController?.ResetRuntimeOnSwitchOut();
    }

    private void Update()
    {
        stateMachine?.Tick(Time.deltaTime);
    }

    private void OnBossSpawnAnimationComplete(bool completed)
    {
        if (stateMachine.CurrentStateId == HeroStateId.Ultimate)
        {
            return;
        }
        stateMachine.ChangeState(!completed ? HeroStateId.BossSpawn : HeroStateId.Idle);
    }

    public void Init(HeroDataSO data, PvEBattleController battleController, HeroTeamController heroTeamController)
    {
        heroData = data;
        
        this.heroTeamController = heroTeamController;
        pveBattleController = battleController;
        skillController?.Init(this, battleController);

        ResetData();
    }

    public void SetChosen(bool chosen)
    {
        IsChosen = chosen;
    }

    public void EnableWinFx(bool enable)
    {
        winFx.SetActive(enable);
    }

    public void ResetData()
    {
        ActiveVisual(true);
        InitializeStatsFromHeroData(heroData);
        PowerUpManager.Instance.BindPlayer(Stats);
        HealthBarController.ResetHealth();
        IsActionLocked = false;
        IsUnderPlayerControl = false;{}
        MoveMode = HeroMoveMode.Auto;
        currentTarget = null;
        attackComboIndex = 0;
        nextTargetSearchTime = 0f;
        stateMachine.ChangeState(HeroStateId.Spawn);
        BindDeathEvent();
        // progressionBridge.Setup(heroData, this);
        // progressionBridge.RefreshFromProgression();
        // equipmentBridge.Setup(this);
        // equipmentBridge.RefreshFromEquipment();
    }
    
    public void ResetSpawnPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void ActiveVisual(bool active)
    {
        animationDriver.ActiveVisual(active);
    }

    public void ActiveHealthBar(bool active)
    {
        healthBarController.gameObject.SetActive(active);
    }
    
    private void InitializeStatsFromHeroData(HeroDataSO data)
    {
        if (stats == null)
            return;

        BaseStat baseStat = new BaseStat
        {
            Health = data.Health,
            Attack = data.Attack,
            Defense = data.Defense,
            AttackRange = data.AttackRange,
            AttackSpeed = data.AttackSpeed,
            CritChance = data != null ? data.CritChance : 0f,
            CritDamage = data != null ? data.CritDamage : 0f,
            Accuracy = data != null ? data.Accuracy : 0f,
            Element = data != null ? data.Element : default
        };

        stats.Initialize(baseStat);
    }

    /// <summary>
    /// Ghi đè base stats bằng giá trị server trả về (hp, atk, def, ...).
    /// Gọi sau Init() khi có HeroInstance từ UserDataCache.
    /// </summary>
    public void ApplyInstanceStats(HeroInstance instance)
    {
        if (stats == null || instance == null || heroData == null) return;

        var baseStat = new BaseStat
        {
            Health      = instance.Hp          > 0 ? instance.Hp          : heroData.Health,
            Attack      = instance.Atk         > 0 ? instance.Atk         : heroData.Attack,
            Defense     = instance.Def         > 0 ? instance.Def         : heroData.Defense,
            AttackRange = instance.AttackRange > 0 ? instance.AttackRange : heroData.AttackRange,
            AttackSpeed = instance.AtkSpd      > 0 ? instance.AtkSpd      : heroData.AttackSpeed,
            CritChance  = instance.CritChance  > 0 ? instance.CritChance  : heroData.CritChance,
            CritDamage  = instance.CritDamage  > 0 ? instance.CritDamage  : heroData.CritDamage,
            Accuracy    = heroData.Accuracy,
            Element     = heroData.Element,
        };

        stats.Initialize(baseStat);
        HealthBarController.ResetHealth();
    }

    private void BindDeathEvent()
    {
        stats.HealthModule.OnDead -= Die;
        stats.HealthModule.OnDead += Die;
    }
    
    // =========================================================
    // Team Movement API
    // =========================================================

    public void ManualMoveByTeam(Vector3 direction, float moveSpeed)
    {
        if (IsDead || IsActionLocked || stateMachine.CurrentStateId == HeroStateId.Ultimate)
            return;

        if (stats != null && !stats.CanMove())
            return;

        if (locomotion == null)
            return;

        IsUnderPlayerControl = true;
        MoveMode = HeroMoveMode.Manual;

        ResetAttackCombo();

        locomotion.MoveByDirection(direction, moveSpeed);

        if (locomotion.IsMoving)
        {
            animationDriver?.FaceDirection(locomotion.LastVelocity);
            stateMachine.ChangeState(HeroStateId.ManualMove);
        }
        else
        {
            stateMachine.ChangeState(HeroStateId.Idle);
        }
    }

    public void FollowByTeam(
        Vector3 targetPosition,
        float followSpeed,
        float stopDistance,
        float smoothTime,
        ref Vector3 velocity
    )
    {
        if (IsDead || IsActionLocked || stateMachine.CurrentStateId == HeroStateId.Ultimate)
            return;

        if (stats != null && !stats.CanMove())
            return;

        if (locomotion == null)
            return;

        IsUnderPlayerControl = true;
        MoveMode = HeroMoveMode.FollowFormation;

        ResetAttackCombo();

        locomotion.FollowPositionSmooth(
            targetPosition,
            followSpeed,
            stopDistance,
            smoothTime,
            ref velocity
        );

        if (locomotion.IsMoving)
        {
            animationDriver?.FaceDirection(locomotion.LastVelocity);
            stateMachine.ChangeState(HeroStateId.ManualMove);
        }
        else
        {
            stateMachine.ChangeState(HeroStateId.Idle);
        }
    }

    public void StopTeamControl()
    {
        if (IsDead || IsActionLocked || stateMachine.CurrentStateId == HeroStateId.Ultimate)
            return;

        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;

        locomotion?.Stop();

        stateMachine.ChangeState(HeroStateId.Idle);
    }

    public void WarpTeamPosition(Vector3 position)
    {
        if (IsDead)
            return;

        position.y = transform.position.y;
        transform.position = position;

        locomotion?.Stop();
    }

    // =========================================================
    // Auto Movement / Combat
    // =========================================================

    public void MoveTowards(Vector3 targetPosition)
    {
        if (IsDead || IsActionLocked)
            return;

        if (stats != null && !stats.CanMove())
            return;

        if (locomotion == null)
            return;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        float moveSpeed = heroTeamController.TeamMoveSpeed;

        locomotion.MoveByDirection(direction, moveSpeed);

        if (locomotion.IsMoving)
            animationDriver?.FaceDirection(locomotion.LastVelocity);
    }
    
    // =========================================================
    // Target
    // =========================================================

    public void SetTarget(ICombatUnit target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    public bool HasValidTarget()
    {
        return currentTarget != null && !currentTarget.IsDead;
    }

    public bool IsTargetInAttackRange()
    {
        if (!HasValidTarget())
            return false;

        Vector3 self = transform.position;
        Vector3 target = currentTarget.Position;

        self.y = 0f;
        target.y = 0f;

        float sqrDistance = (target - self).sqrMagnitude;
        return sqrDistance <= AttackRange * AttackRange;
    }

    public void SearchTarget()
    {
        if (Time.time < nextTargetSearchTime)
            return;

        nextTargetSearchTime = Time.time + targetSearchInterval;

        if (pveBattleController == null)
        {
            currentTarget = null;
            return;
        }

        currentTarget = pveBattleController.GetNearestEnemy(transform.position);
    }

    // =========================================================
    // Attack Combo
    // =========================================================

    public int GetCurrentAttackComboIndex()
    {
        return attackComboIndex;
    }
    
    public void RefreshSelectedSkillsRuntime()
    {
        // var ids = UserDataCache.Instance.GetEquippedSkills(HeroId);
        //
        // Debug.Log($"[HeroRuntime] Refresh skills hero={behaviorParams.HeroId} -> {string.Join(",", ids)}");
        //
        // SetIntervalSkills(ids);
        // InitSkill(ids, skillRootTrans);
        // InitUIHeroBattle();
    }

    public void AdvanceAttackCombo()
    {
        attackComboIndex++;

        if (attackComboIndex >= 3)
            attackComboIndex = 0;
    }

    public void ResetAttackCombo()
    {
        attackComboIndex = 0;
    }

    // =========================================================
    // Damage / Heal
    // =========================================================

    public void DealAttackDamage()
    {
        if (!HasValidTarget())
            return;

        if (AttackMode == HeroAttackMode.Ranged)
        {
            SpawnProjectile();
        }
        else
        {
            DamageResult damageResult = DamageCalculator.CalculateDamage(this, currentTarget);
            currentTarget.TakeDamage(damageResult);
            HitEffectManager.Instance.Play(currentTarget);
        }
    }

    private void SpawnProjectile()
    {
        if (!HasValidTarget())
            return;

        if (projectilePrefab == null)
        {
            DamageResult damageResult = DamageCalculator.CalculateDamage(this, currentTarget);
            currentTarget.TakeDamage(damageResult);
            return;
        }

        Vector3 spawnPosition = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + Vector3.up * 0.8f;

        HeroProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        projectile.Init(currentTarget, this, Attack);
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (stats == null || stats.HealthModule == null)
            return;

        stats.HealthModule.ApplyHeal(amount);
    }

    // =========================================================
    // Skill / Ability
    // =========================================================

    public void CastingUltimate(bool isCastingUltimate)
    {
        if (IsDead || IsActionLocked)
            return;
        
        ResetAttackCombo();
        if (isCastingUltimate)
        {
            stateMachine.ChangeState(HeroStateId.Ultimate);
            return;
        }

        if (stateMachine.CurrentStateId == HeroStateId.Win)
        {
            return;
        }
        
        stateMachine.ChangeState(HeroStateId.Idle);
        
    }

    public void CastPassive()
    {
        if (IsDead || IsActionLocked)
            return;

        if (stats != null && !stats.CanCastSkill())
            return;

        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;

        ResetAttackCombo();

        stateMachine.ChangeState(HeroStateId.Passive);
    }

    public void SetActionLocked(bool locked)
    {
        IsActionLocked = locked;
    }

    // =========================================================
    // Dead / Win
    // =========================================================

    private void Die()
    {
        OnDead?.Invoke(this);
        ResetAttackCombo();

        IsActionLocked = false;
        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;

        locomotion?.Stop();
        stateMachine.ChangeState(HeroStateId.Dead);
    }

    private void OnTimeOut()
    {
        
    }

    public void Win()
    {
        if (IsDead)
            return;

        IsActionLocked = false;
        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;
        locomotion?.Stop();
        stateMachine.ChangeState(HeroStateId.Win, true);
    }

    public void OnSpawnedFromPool()
    {
        throw new NotImplementedException();
    }

    public void OnDespawnedToPool()
    {
        throw new NotImplementedException();
    }
}

[Serializable]
public class BaseStat
{
    public float Health;
    public float IdleStateTime;
    public float IdleIntervalTime;
    public float AttackRange;
    public float Defense;
    public float Attack;
    public float AttackSpeed;
    public float CritChance;
    public float CritDamage;
    public float Accuracy;
    public float MoveSpeed;
    public Element Element;
}