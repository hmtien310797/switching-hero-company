using System;
using System.Collections.Generic;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immortal_Switch.Scripts.Enemy
{
    public class EnemyActor : AddressablePoolableBehaviour, ICombatUnit
    {
        public enum EnemyState
        {
            Spawn,
            Idle,
            Run,
            Attack,
            Dead
        }
        
        [Header("Components")]
        [SerializeField] private StatsController stats;
        [SerializeField] private HealthBarController healthBarController;
        [SerializeField] private HeroAnimationDriver animationDriver;
        [SerializeField] private HeroLocomotion locomotion;
        [SerializeField] private GameObject spineAnimation;

        [Header("Death")]
        [SerializeField] private bool destroyOnDead = true;
        [SerializeField] private float destroyDelay = 1.2f;
        
        [Header("Properties")]
        [field: SerializeField] public ActorType ActorType { get; private set;}

        private readonly List<ICombatUnit> heroTargets = new();

        [ShowInInspector, ReadOnly]
        private ICombatUnit currentTarget;
        [ShowInInspector, ReadOnly]
        private EnemyState currentState;

        private CreepDataSo creepData;
        private float attackTimer;
        private float attackCooldown;
        private bool hasHitThisAttack;
        private float attackRange = 1.2f;
        private float attackDamage = 5f;
        private float attackSpeed = 1f;
        private float moveSpeed = 3f;

        public StatsController Stats => stats;
        public Element Element => creepData.Element;
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
        
        public event Action<EnemyActor> OnDead;

        private void Update()
        {
            if (IsDead)
                return;

            TickState(Time.deltaTime);
        }

        public void Init(CreepDataSo data, ICombatUnit heroA, ICombatUnit heroB)
        {
            Init(data, heroA, heroB, StageStatScale.Identity);
        }

        public void Init(CreepDataSo data, ICombatUnit heroA, ICombatUnit heroB, StageStatScale scale)
        {
            creepData = data;
            HealthBarController.ResetHealth();
            ApplyData(data, scale);
            SetHeroTargets(heroA, heroB);

            currentTarget = null;
            attackTimer = 0f;
            hasHitThisAttack = false;

            BindDeathEvent();
            BindAnimationEvents();

            ChangeState(EnemyState.Spawn);
        }
        
        public void Init(CreepDataSo data, ICombatUnit heroA, ICombatUnit heroB, BaseStat cachedBaseStat)
        {
            creepData = data;

            HealthBarController.ResetHealth();

            ApplyBaseStat(data, cachedBaseStat);

            SetHeroTargets(heroA, heroB);

            currentTarget = null;
            attackTimer = 0f;
            hasHitThisAttack = false;

            BindDeathEvent();
            BindAnimationEvents();

            ChangeState(EnemyState.Spawn);
        }

        private void ApplyBaseStat(CreepDataSo data, BaseStat baseStat)
        {
            if (data == null)
            {
                Debug.LogWarning($"{name}: CreepDataSo is null.");
                return;
            }

            attackDamage = baseStat.Attack;
            attackRange = baseStat.AttackRange;
            attackSpeed = Mathf.Max(0.1f, baseStat.AttackSpeed);
            moveSpeed = baseStat.MoveSpeed;
            attackCooldown = 1f / attackSpeed;

            if (stats == null)
                return;

            stats.Initialize(baseStat);
        }

        public void SetScale(float scale)
        {
            spineAnimation.transform.localScale = new Vector3(scale, scale, scale);
        }
        
        private void ApplyData(CreepDataSo data, StageStatScale scale)
        {
            if (data == null)
            {
                Debug.LogWarning($"{name}: CreepDataSo is null.");
                return;
            }

            scale.Normalize();

            float scaledHp = data.BaseHp * scale.HpMultiplier;
            float scaledAtk = data.BaseAtk * scale.AtkMultiplier;
            float scaledDef = data.BaseDef * scale.DefMultiplier;

            attackDamage = scaledAtk;
            attackRange = data.BaseRange;
            attackSpeed = Mathf.Max(0.1f, data.BaseAtkSpeed);
            moveSpeed = data.BaseMoveSpeed;
            attackCooldown = 1f / attackSpeed;

            if (stats == null)
                return;

            BaseStat baseStat = new BaseStat
            {
                Health = scaledHp,
                Attack = scaledAtk,
                Defense = scaledDef,
                AttackSpeed = data.BaseAtkSpeed,
                AttackRange = data.BaseRange,
                MoveSpeed = data.BaseMoveSpeed,
                Element = data.Element
            };

            stats.Initialize(baseStat);
        }

        private void BindDeathEvent()
        {
            if (stats == null || stats.HealthModule == null)
                return;

            stats.HealthModule.OnDead -= Die;
            stats.HealthModule.OnDead += Die;
        }

        private void BindAnimationEvents()
        {
            if (animationDriver == null)
                return;

            animationDriver.SpineEventTriggered -= OnSpineEvent;
            animationDriver.AnimationCompleted -= OnAnimationCompleted;

            animationDriver.SpineEventTriggered += OnSpineEvent;
            animationDriver.AnimationCompleted += OnAnimationCompleted;

            animationDriver.SkeletonAnim.LimitRuntimeUpdateRate = true;
            animationDriver.SkeletonAnim.TargetRuntimeFps = 30;
        }

        public void SetHeroTargets(ICombatUnit heroA, ICombatUnit heroB)
        {
            heroTargets.Clear();

            if (heroA != null)
                heroTargets.Add(heroA);

            if (heroB != null)
                heroTargets.Add(heroB);
        }

        public void Heal(float amount)
        {
            if (IsDead)
                return;

            if (stats == null || stats.HealthModule == null)
                return;

            stats.HealthModule.ApplyHeal(amount);
        }

        private void TickState(float deltaTime)
        {
            switch (currentState)
            {
                case EnemyState.Spawn:
                    break;

                case EnemyState.Idle:
                    TickIdle();
                    break;

                case EnemyState.Run:
                    TickRun();
                    break;

                case EnemyState.Attack:
                    TickAttack(deltaTime);
                    break;

                case EnemyState.Dead:
                    break;
            }
        }

        private void TickIdle()
        {
            currentTarget = FindNearestHero();

            if (currentTarget == null)
                return;

            if (IsTargetInAttackRange())
                ChangeState(EnemyState.Attack);
            else
                ChangeState(EnemyState.Run);
        }

        private void TickRun()
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (IsTargetInAttackRange())
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (stats != null && !stats.CanMove())
                return;

            Vector3 direction = currentTarget.Position - transform.position;
            direction.y = 0f;

            if (locomotion != null)
                locomotion.MoveByDirection(direction, moveSpeed);

            if (animationDriver != null && locomotion != null && locomotion.IsMoving)
                animationDriver.FaceDirection(locomotion.LastVelocity);
        }

        private void TickAttack(float deltaTime)
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (!IsTargetInAttackRange())
            {
                ChangeState(EnemyState.Run);
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

            if (animationDriver == null)
                return;

            animationDriver.FaceTarget(transform.position, currentTarget.Position);
            animationDriver.PlayAttack(0);
        }

        private void OnSpineEvent(string eventName)
        {
            if (currentState != EnemyState.Attack)
                return;

            if (hasHitThisAttack)
                return;
            
            hasHitThisAttack = true;

            if (currentTarget == null || currentTarget.IsDead)
                return;

            DamageResult damageResult = DamageCalculator.CalculateDamage(this, currentTarget);
            currentTarget.TakeDamage(damageResult);
        }

        private void OnAnimationCompleted(string animationName)
        {
            if (currentState == EnemyState.Spawn)
            {
                ChangeState(EnemyState.Idle);
            }
        }

        private ICombatUnit FindNearestHero()
        {
            ICombatUnit nearest = null;
            float nearestSqr = float.MaxValue;

            Vector3 selfPos = transform.position;
            selfPos.y = 0f;

            if (heroTargets.Count == 0)
            {
                HeroActor[] heroes = Object.FindObjectsByType<HeroActor>(FindObjectsSortMode.None);

                for (int i = 0; i < heroes.Length; i++)
                {
                    HeroActor hero = heroes[i];

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
            return sqr <= attackRange * attackRange;
        }

        private void ChangeState(EnemyState nextState)
        {
            if (currentState == nextState)
                return;

            currentState = nextState;

            switch (currentState)
            {
                case EnemyState.Spawn:
                    animationDriver?.PlaySpawn();
                    break;

                case EnemyState.Idle:
                    animationDriver?.PlayIdle();
                    break;

                case EnemyState.Run:
                    animationDriver?.PlayRun();
                    break;

                case EnemyState.Attack:
                    attackTimer = attackCooldown;
                    animationDriver?.PlayIdle();
                    break;

                case EnemyState.Dead:
                    OnDeadEvent();
                    break;
            }
        }

        private void OnDeadEvent()
        {
            animationDriver.PlayDead();
            DespawnToPool();
        }

        private void Die()
        {
            if (currentState == EnemyState.Dead)
                return;
            
            ChangeState(EnemyState.Dead);
            OnDead?.Invoke(this);
            locomotion?.Stop();
        }
        
        public void KillImmediately()
        {
            if (IsDead)
                return;
            if (stats == null || stats.HealthModule == null)
            {
                Die();
                return;
            }
            DamageResult damageResult = DamageCalculator.CalculateDamage(this, this, 999999999);
            stats.HealthModule.TakeDamage(damageResult);

        }

        public override void OnSpawned(AddressablePoolHandle handle)
        {
            base.OnSpawned(handle);
        }

        public override void OnDespawned()
        {
            base.OnDespawned();
        }
    }
}