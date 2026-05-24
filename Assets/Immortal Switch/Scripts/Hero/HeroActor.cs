using System;
using Battle;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public enum HeroStateId
{
    Idle,
    Run,
    Attack,
    Ultimate,
    Passive,
    Dead,
    Win
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

    [Header("Attack")]
    [SerializeField] private HeroAttackMode attackMode = HeroAttackMode.Melee;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private HeroProjectile projectilePrefab;

    [SerializeField] private float targetSearchRange = 8f;
    [SerializeField] private float targetSearchInterval = 0.2f;

    [Header("Ability")]
    [SerializeField] private float ultimateHitNormalizedTime = 0.5f;
    [SerializeField] private float passiveHitNormalizedTime = 0.5f;

    [ShowInInspector, ReadOnly]
    private ICombatUnit currentTarget;
    private HeroStateMachine stateMachine;
    private PvEBattleController pveBattleController;
    private HeroTeamController heroTeamController;
    private HeroDataSO heroData;
    
    private int attackComboIndex;
    private float nextTargetSearchTime;
    private bool isDeathEventBound;

    public HeroDataSO HeroData => heroData;

    public StatsController Stats => stats;

    public Transform Transform => transform;

    public Vector3 Position => transform.position;

    public HeroLocomotion Locomotion => locomotion;

    public HeroAnimationDriver Anim => animationDriver;

    public HeroSkillController SkillController => skillController;

    public ICombatUnit CurrentTarget => currentTarget;

    public HeroMoveMode MoveMode { get; private set; } = HeroMoveMode.Auto;

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

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<StatsController>();

        if (locomotion == null)
            locomotion = GetComponent<HeroLocomotion>();

        if (animationDriver == null)
            animationDriver = GetComponent<HeroAnimationDriver>();

        if (skillController == null)
            skillController = GetComponent<HeroSkillController>();

        stateMachine = new HeroStateMachine(this);
    }

    private void OnEnable()
    {
        BindDeathEvent();

        if (stateMachine != null)
            stateMachine.ChangeState(HeroStateId.Idle, true);
    }

    private void OnDisable()
    {
        skillController?.ResetRuntimeOnSwitchOut();
        UnbindDeathEvent();
    }

    private void Update()
    {
        if (SkillController != null && SkillController.IsSkillLocked)
            return;
        
        stateMachine?.Tick(Time.deltaTime);
    }

    public void Init(HeroDataSO data, PvEBattleController battleController, HeroTeamController heroTeamController)
    {
        heroData = data;

        InitializeStatsFromHeroData(data);

        BindDeathEvent();

        IsActionLocked = false;
        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;
        this.heroTeamController = heroTeamController;

        currentTarget = null;
        attackComboIndex = 0;
        nextTargetSearchTime = 0f;
        pveBattleController = battleController;
        skillController?.Init(this, battleController);
        stateMachine.ChangeState(HeroStateId.Idle, true);
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

    private void BindDeathEvent()
    {
        if (isDeathEventBound)
            return;

        if (stats == null || stats.HealthModule == null)
            return;

        stats.HealthModule.OnDead += Die;
        isDeathEventBound = true;
    }

    private void UnbindDeathEvent()
    {
        if (!isDeathEventBound)
            return;

        if (stats != null && stats.HealthModule != null)
            stats.HealthModule.OnDead -= Die;

        isDeathEventBound = false;
    }

    // =========================================================
    // Team Movement API
    // =========================================================

    public void ManualMoveByTeam(Vector3 direction, float moveSpeed)
    {
        if (IsDead || IsActionLocked)
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
            stateMachine.ChangeState(HeroStateId.Run);
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
        if (IsDead || IsActionLocked)
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
            stateMachine.ChangeState(HeroStateId.Run);
        }
        else
        {
            stateMachine.ChangeState(HeroStateId.Idle);
        }
    }

    public void StopTeamControl()
    {
        if (IsDead || IsActionLocked)
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
            currentTarget.TakeDamage(this, damageResult);
        }
    }

    private void SpawnProjectile()
    {
        if (!HasValidTarget())
            return;

        if (projectilePrefab == null)
        {
            DamageResult damageResult = DamageCalculator.CalculateDamage(this, currentTarget);
            currentTarget.TakeDamage(this, damageResult);
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

    public void CastUltimate()
    {
        if (IsDead || IsActionLocked)
            return;

        if (stats != null && !stats.CanCastSkill())
            return;

        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;

        ResetAttackCombo();

        stateMachine.ChangeState(HeroStateId.Ultimate, true);
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

        stateMachine.ChangeState(HeroStateId.Passive, true);
    }

    public void SetActionLocked(bool locked)
    {
        IsActionLocked = locked;
    }

    // =========================================================
    // Dead / Win
    // =========================================================

    public void Die()
    {
        if (stateMachine == null)
            return;

        ResetAttackCombo();

        IsActionLocked = false;
        IsUnderPlayerControl = false;
        MoveMode = HeroMoveMode.Auto;

        locomotion?.Stop();

        stateMachine.ChangeState(HeroStateId.Dead, true);
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