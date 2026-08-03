using System;
using System.Collections.Generic;
using System.Threading;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.Sound;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AddressableProjectilePoolable))]
public class BulletProjectile :
    MonoBehaviour,
    IAddressableProjectile
{
    [Header("Collision")]
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected bool despawnOnHit = true;

    protected AddressableProjectilePoolable addressablePoolable;

    protected Vector3 direction;
    protected float speed;
    protected float lifeTime;
    protected float timer;
    protected bool isInitialized;
    protected float damage;

    protected SkillRuntimeObject controller;
    protected ICombatUnit sourceCombatUnit;
    protected CancellationTokenRegistration _endStageCancelRegistration;
    protected CancellationToken _endStageCancellationToken;
    protected SkillRuntimeObjectConfig Config;
    protected SkillRuntimeContext Context;
    protected SkillExecutor Executor;
    protected SkillTargetResolver skillTargetResolver;
    protected ISkillObjectSpawner SkillObjectSpawner;
    
    protected void Awake()
    {
        addressablePoolable =
            GetComponent<AddressableProjectilePoolable>();

        if (addressablePoolable == null)
        {
            Debug.LogError(
                $"[{nameof(BulletProjectile)}] Missing " +
                $"{nameof(AddressableProjectilePoolable)}.",
                this
            );
        }
    }

    public virtual void Setup(SkillRuntimeObject controller,
        ICombatUnit source, SkillRuntimeContext context, SkillRuntimeObjectConfig Config, SkillExecutor executor, SkillTargetResolver targetResolver,
        ISkillObjectSpawner skillObjectSpawner,
        Vector3 moveDirection,
        float bulletSpeed,
        float bulletLifeTime,
        float damage)
    {
        /*
         * Luôn ghi source mới, kể cả source là null.
         *
         * Nếu chỉ assign khi source != null, pooled bullet có thể
         * giữ sourceCombatUnit từ lần sử dụng trước.
         */
        sourceCombatUnit = source;
        this.Config = Config;
        direction =
            moveDirection.sqrMagnitude > 0.0001f
                ? moveDirection.normalized
                : Vector3.zero;

        this.controller = controller;
        Context = context;
        Executor = executor;
        skillTargetResolver = targetResolver;
        SkillObjectSpawner = skillObjectSpawner;
        speed = bulletSpeed;
        lifeTime = bulletLifeTime;
        this.damage = damage;

        timer = 0f;
        isInitialized = true;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up
                );
        }
        
        if (BattleFlowController.Instance.endStageSessionCancellationTokenSource != null)
        {
            _endStageCancellationToken =
                BattleFlowController.Instance.endStageSessionCancellationTokenSource.Token;
            _endStageCancelRegistration =
                _endStageCancellationToken.Register(DespawnSelf);
        }
    }

    public void OnProjectileSpawnedFromPool()
    {
        ResetRuntimeData();
    }

    public void OnProjectileDespawnedToPool()
    {
        ResetRuntimeData();
    }
    
    protected virtual void Update()
    {
        if (!isInitialized)
            return;

        float deltaTime = Time.deltaTime;

        transform.position +=
            direction * speed * deltaTime;

        timer += deltaTime;

        if (timer >= lifeTime)
        {
            DespawnSelfAsync();
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isInitialized)
            return;

        ICombatUnit targetCombatUnit =
            other.GetComponent<ICombatUnit>();

        if (targetCombatUnit == null)
            return;

        // FLAGGED PvP: không hit caster + team-aware. Khi bật Bullet×Player trong Physics matrix,
        // hero địch (Player layer) nằm trong opposing registry → hit; hero đồng đội (Player layer,
        // không trong registry, không trong enemyLayer mask) → skip (chống friendly fire). PvE giữ
        // nguyên: creep/boss nằm trong registry + Enemy/Boss layer → hit.
        if (ReferenceEquals(targetCombatUnit, sourceCombatUnit))
            return;

        if (!IsValidTarget(targetCombatUnit, other.gameObject.layer))
            return;

        /*
         * Giữ nguyên logic cũ:
         * DamageCalculator nhận source và target để tính damage.
         */
        DamageResult damageResult =
            DamageCalculator.CalculateDamage(
                sourceCombatUnit,
                targetCombatUnit,
                damage
            );

        targetCombatUnit.TakeDamage(damageResult);
        if (despawnOnHit)
        {
            DespawnSelfAsync().Forget();
        }
    }

    /// <summary>
    /// Team-aware target filter (FLAGGED PvP). Hit nếu target nằm trong hostile registry của caster
    /// (PvE: creep/boss; PvP: hero đối phương ở Player layer) HOẶC khớp legacy <see cref="enemyLayer"/>
    /// (Enemy/Boss). Hero đồng đội (Player layer, không trong registry + không trong mask) → skip.
    /// </summary>
    protected bool IsValidTarget(ICombatUnit target, int layer)
    {
        IBattleTargetRegistry registry = Context?.BattleContext?.TargetRegistry;
        if (registry != null)
        {
            IReadOnlyList<ICombatUnit> hostiles = registry.HostileTargets;
            if (hostiles != null)
            {
                for (int i = 0; i < hostiles.Count; i++)
                    if (ReferenceEquals(hostiles[i], target))
                        return true;
            }
        }

        return IsInLayerMask(layer, enemyLayer);
    }

    protected void DespawnSelf()
    {
        DespawnSelfAsync(0f).Forget();
    }

    protected virtual async UniTask DespawnSelfAsync(float delay = 0f)
    {
        if (!isInitialized)
            return;

        if (delay > 0)
        {
            // Gắn token end-stage: nếu stage kết thúc (Give Up / clear / lost)
            // trong lúc đang chờ delay, delay sẽ bị huỷ và không resume despawn cũ.
            // Tránh despawn "zombie" chạy trễ vô tình giết bullet đã được spawn lại
            // cho stage kế tiếp.
            await UniTask.Delay(
                TimeSpan.FromSeconds(delay),
                cancellationToken: _endStageCancellationToken);
        }

        /*
         * Trong lúc chờ delay, bullet có thể đã bị despawn qua đường khác
         * (registration end-stage, lifetime, hoặc trigger khác). Tránh gọi
         * Despawn lần thứ hai khi PoolHandle đã bị clear.
         */
        if (!isInitialized)
            return;
        
        /*
         * Chặn bullet tiếp tục Update hoặc xử lý trigger khác
         * trong lúc đang được trả về pool.
         */
        isInitialized = false;

        if (addressablePoolable == null)
        {
            addressablePoolable =
                GetComponent<AddressableProjectilePoolable>();
        }

        if (addressablePoolable == null)
        {
            Debug.LogError(
                $"[{nameof(BulletProjectile)}] Cannot return bullet " +
                $"to Addressable Pool because " +
                $"{nameof(AddressableProjectilePoolable)} is missing.",
                this
            );

            gameObject.SetActive(false);
            return;
        }

        _endStageCancelRegistration.Dispose();
        addressablePoolable.Despawn();
    }

    protected void ResetRuntimeData()
    {
        isInitialized = false;

        sourceCombatUnit = null;

        direction = Vector3.zero;

        speed = 0f;
        lifeTime = 0f;
        timer = 0f;
        damage = 0f;
        
    }

    protected static bool IsInLayerMask(
        int layer,
        LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}