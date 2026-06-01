using System;
using System.Collections.Generic;
using Battle;
using Common;
using DG.Tweening;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    public class BossActor : PoolableBehaviour, ICombatUnit
    {
        public enum BossState
        {
            None = 0,
            Spawn,
            Idle,
            Run,
            Attack,
            CastSkill,
            Dead
        }
        
        [Header("Components")]
        [SerializeField] private StatsController stats;
        [SerializeField] private HeroAnimationDriver animationDriver;
        [SerializeField] private HeroLocomotion locomotion;
        [SerializeField] private HealthBarController healthBarController;

        [Header("Animation")]
        [SerializeField] private string hitEventName = "hit";
        [SerializeField] private string skillHitEventName = "skill_hit";

        [Header("Spawn")]
        [SerializeField] private bool useSpawnState = true;
        [SerializeField] private float spawnFallbackDuration = 0.8f;

        [Header("Death")]
        [SerializeField] private bool destroyOnDead = false;
        [SerializeField] private float destroyDelay = 2f;
        
        [Header("Properties")]
        [field: SerializeField] public ActorType ActorType { get; private set;}

        private readonly List<ICombatUnit> heroTargets = new();

        private BossState currentState;
        
        private ICombatUnit currentTarget;
        private IBossSkillLogic skillLogic;
        private BossDataSO bossData;

        private float attackCooldown = 1f;
        private float attackTimer;
        private float spawnTimer;
        private bool hasHitThisAttack;
        
        public event Action<BossActor> OnDead;
        
        public StatsController Stats => stats;
        public HealthBarController HealthBarController => healthBarController;

        public Transform Transform => transform;

        public Vector3 Position => transform.position;

        public bool IsDead => stats != null &&
                              stats.HealthModule != null &&
                              stats.HealthModule.IsDead;

        public float CurrentHp => stats != null && stats.HealthModule != null
            ? stats.HealthModule.CurrentHP
            : 0f;

        public float MaxHp => stats != null && stats.HealthModule != null
            ? stats.HealthModule.MaxHP
            : 0f;

        public int BossId => bossData != null ? bossData.Id : 0;

        public ICombatUnit Target => currentTarget;

        public int NormalAttackCount { get; private set; }

        public bool IsReady => currentState != BossState.Spawn && currentState != BossState.Dead;

        private void Awake()
        {
            if (stats == null)
                stats = GetComponent<StatsController>();

            if (animationDriver == null)
                animationDriver = GetComponent<HeroAnimationDriver>();

            if (locomotion == null)
                locomotion = GetComponent<HeroLocomotion>();
        }

        private void Update()
        {
            if (IsDead)
                return;

            TickState(Time.deltaTime);
        }

        public void Init(BossDataSO data, ICombatUnit heroA, ICombatUnit heroB)
        {
            bossData = data;

            ApplyData(data);
            SetHeroTargets(heroA, heroB);

            NormalAttackCount = 0;
            currentTarget = null;
            attackTimer = 0f;
            hasHitThisAttack = false;

            skillLogic = BossSkillLogicFactory.Create(BossId);
            skillLogic.Initialize(this);

            BindDeathEvent();
            BindHealthEvents();
            BindAnimationEvents();

            ChangeState(useSpawnState ? BossState.Spawn : BossState.Idle);

            skillLogic.OnBattleStart();
        }

        private void ApplyData(BossDataSO data)
        {
            if (data == null)
            {
                Debug.LogWarning($"{name}: BossDataSO is null.");
                return;
            }
            
            attackCooldown = 1f / data.AtkSpeed;

            if (stats == null)
                return;

            BaseStat baseStat = new BaseStat
            {
                Health = data.BaseHP,
                Attack = data.BaseAtk,
                Defense = data.BaseDef,
                AttackSpeed = data.AtkSpeed,
                AttackRange = data.AttackRange,
                MoveSpeed = data.MoveSpeed,
                Element = data.Element
            };

            stats.Initialize(baseStat);
        }

        public void SetHeroTargets(ICombatUnit heroA, ICombatUnit heroB)
        {
            heroTargets.Clear();

            if (heroA != null)
                heroTargets.Add(heroA);

            if (heroB != null)
                heroTargets.Add(heroB);
        }

        private void BindDeathEvent()
        {
            if (stats == null || stats.HealthModule == null)
                return;

            stats.HealthModule.OnDead -= Die;
            stats.HealthModule.OnDead += Die;
        }

        private void BindHealthEvents()
        {
            if (stats == null || stats.HealthModule == null)
                return;

            stats.HealthModule.OnDamaged -= OnDamaged;
            stats.HealthModule.OnHPChanged -= OnHpChanged;

            stats.HealthModule.OnDamaged += OnDamaged;
            stats.HealthModule.OnHPChanged += OnHpChanged;
        }

        private void BindAnimationEvents()
        {
            if (animationDriver == null)
                return;

            animationDriver.SpineEventTriggered -= OnSpineEvent;
            animationDriver.AnimationCompleted -= OnAnimationCompleted;

            animationDriver.SpineEventTriggered += OnSpineEvent;
            animationDriver.AnimationCompleted += OnAnimationCompleted;
        }

        private void OnDamaged(float damage, DamageType damageType)
        {
            if (IsDead)
            {
                return;
            }
            skillLogic?.OnHitTaken(damage);
        }

        private void OnHpChanged(float currentHp, float maxHp)
        {
            if (IsDead)
            {
                return;
            }
            skillLogic?.OnHpChanged();
        }

        private void TickState(float deltaTime)
        {
            switch (currentState)
            {
                case BossState.Idle:
                    TickIdle();
                    break;

                case BossState.Run:
                    TickRun();
                    break;

                case BossState.Attack:
                    TickAttack(deltaTime);
                    break;

                case BossState.CastSkill:
                    break;

                case BossState.Dead:
                    break;
            }
        }

        private void TickIdle()
        {
            currentTarget = FindNearestHero();

            if (currentTarget == null)
                return;

            if (IsTargetInAttackRange())
                ChangeState(BossState.Attack);
            else
                ChangeState(BossState.Run);
        }

        private void TickRun()
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                ChangeState(BossState.Idle);
                return;
            }

            if (IsTargetInAttackRange())
            {
                ChangeState(BossState.Attack);
                return;
            }

            if (stats != null && !stats.CanMove())
                return;

            Vector3 direction = currentTarget.Position - transform.position;
            direction.y = 0f;

            animationDriver?.FaceTarget(transform.position, currentTarget.Position);
            locomotion?.MoveByDirection(direction, bossData.MoveSpeed);
        }

        private void TickAttack(float deltaTime)
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                ChangeState(BossState.Idle);
                return;
            }

            if (!IsTargetInAttackRange())
            {
                ChangeState(BossState.Run);
                return;
            }

            if (stats != null && !stats.CanAttack())
                return;

            attackTimer += deltaTime;

            if (attackTimer >= attackCooldown)
            {
                StartAttack();
            }
        }

        private void StartAttack()
        {
            if (currentTarget == null || currentTarget.IsDead)
                return;

            attackTimer = 0f;
            hasHitThisAttack = false;

            animationDriver?.FaceTarget(transform.position, currentTarget.Position);
            animationDriver?.PlayAttack(0);
        }

        private void OnSpineEvent(string eventName)
        {
            if (currentState == BossState.Attack && eventName == hitEventName)
            {
                OnNormalAttackHit();
                return;
            }

            if (currentState == BossState.CastSkill && eventName == skillHitEventName)
            {
                // Skill effect sau này có thể trigger ở đây.
            }
        }

        private void OnNormalAttackHit()
        {
            if (hasHitThisAttack)
                return;

            hasHitThisAttack = true;

            if (currentTarget == null || currentTarget.IsDead)
                return;

            DamageResult damageResult = DamageCalculator.CalculateDamage(this, currentTarget);
            currentTarget.TakeDamage(this, damageResult);

            NormalAttackCount++;
            skillLogic?.OnNormalAttack();
        }

        private void OnAnimationCompleted(string animationName)
        {
            if (currentState == BossState.Spawn)
            {
                ChangeState(BossState.Idle);
                return;
            }

            if (currentState == BossState.CastSkill)
            {
                ChangeState(BossState.Idle);
            }
        }

        private ICombatUnit FindNearestHero()
        {
            ICombatUnit nearest = null;
            float nearestSqr = float.MaxValue;

            Vector3 selfPos = transform.position;
            selfPos.y = 0f;

            for (int i = 0; i < heroTargets.Count; i++)
            {
                ICombatUnit hero = heroTargets[i];

                if (hero == null || hero.IsDead)
                    continue;

                Vector3 heroPos = hero.Position;
                heroPos.y = 0f;

                float sqr = (heroPos - selfPos).sqrMagnitude;

                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = hero;
                }
            }

            return nearest;
        }

        private bool IsTargetInAttackRange()
        {
            if (currentTarget == null || currentTarget.IsDead)
                return false;

            Vector3 self = transform.position;
            Vector3 target = currentTarget.Position;

            self.y = 0f;
            target.y = 0f;

            float sqr = (target - self).sqrMagnitude;
            return sqr <= bossData.AttackRange;
        }

        private void ChangeState(BossState nextState)
        {
            if (currentState == nextState)
                return;

            currentState = nextState;

            switch (currentState)
            {
                case BossState.Spawn:
                    spawnTimer = 0f;
                    GameEventManager.Trigger(GameEvents.OnBossSpawnAnimationComplete, false);
                    DOVirtual.DelayedCall(spawnFallbackDuration, OnBossSpawnAnimationComplete);
                    animationDriver?.PlaySpawn();
                    break;

                case BossState.Idle:
                    animationDriver?.PlayIdle();
                    break;

                case BossState.Run:
                    animationDriver?.PlayRun();
                    break;

                case BossState.Attack:
                    attackTimer = attackCooldown;
                    break;

                case BossState.CastSkill:
                    break;

                case BossState.Dead:
                    animationDriver?.PlayDead();
                    break;
            }
        }

        private void OnBossSpawnAnimationComplete()
        {
            GameEventManager.Trigger(GameEvents.OnBossSpawnAnimationComplete, true);
        }

        public void ResetNormalAttackCount()
        {
            NormalAttackCount = 0;
        }

        public void CastActiveSkillAnimation()
        {
            if (IsDead)
                return;

            if (stats != null && !stats.CanCastSkill())
                return;

            ChangeState(BossState.CastSkill);

            animationDriver?.PlayUltimate();

            skillLogic?.OnSkillCast();
        }

        public void DealDamageToTarget(ICombatUnit target, float damageMultiplierPercent)
        {
            if (target == null || target.IsDead)
                return;
            
            DamageResult damageResult = DamageCalculator.CalculateDamage(this, currentTarget, damageMultiplierPercent);
            target.TakeDamage(this, damageResult);
        }

        public void DealDamageToAllHeroTargets(float damageMultiplierPercent)
        {
            for (int i = 0; i < heroTargets.Count; i++)
            {
                ICombatUnit target = heroTargets[i];

                if (target == null || target.IsDead)
                    continue;
                DamageResult damageResult = DamageCalculator.CalculateDamage(this, target, damageMultiplierPercent);
                target.TakeDamage(this, damageResult);
            }
        }

        public void ApplyBuffToTarget(ICombatUnit target, BuffData buffData)
        {
            if (target == null || target.IsDead)
                return;

            if (target.Stats == null || target.Stats.BuffModule == null)
                return;

            target.Stats.BuffModule.ApplyBuff(buffData);
        }

        public void ApplyBuffToSelf(BuffData buffData)
        {
            if (stats == null || stats.BuffModule == null)
                return;

            stats.BuffModule.ApplyBuff(buffData);
        }
        
        public void Heal(float amount)
        {
            if (IsDead)
                return;

            if (stats == null || stats.HealthModule == null)
                return;

            stats.HealthModule.ApplyHeal(amount);
        }

        private void Die()
        {
            if (currentState == BossState.Dead)
                return;
            
            ChangeState(BossState.Dead);
            locomotion?.Stop();
            OnDead?.Invoke(this);
            DespawnSelf(destroyDelay);
        }
    }
}