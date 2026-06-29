using System;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AddressableProjectilePoolable))]
public class BulletProjectile :
    MonoBehaviour,
    IAddressableProjectile
{
    [Header("Collision")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool despawnOnHit = true;

    private AddressableProjectilePoolable addressablePoolable;

    private Vector3 direction;
    private float speed;
    private float lifeTime;
    private float timer;
    private bool isInitialized;
    private float damage;

    private ICombatUnit sourceCombatUnit;

    private void Awake()
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

    public void Setup(
        ICombatUnit source,
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

        direction =
            moveDirection.sqrMagnitude > 0.0001f
                ? moveDirection.normalized
                : Vector3.zero;

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
    }

    public void OnProjectileSpawnedFromPool()
    {
        ResetRuntimeData();
    }

    public void OnProjectileDespawnedToPool()
    {
        ResetRuntimeData();
    }

    private void OnEnable()
    {
        GameEventManager.Subscribe(GameEvents.OnStageChange, DespawnSelf);
    }
    
    private void OnDisable()
    {
        GameEventManager.Unsubscribe(GameEvents.OnStageChange, DespawnSelf);
    }

    private void OnDestroy()
    {
        GameEventManager.Unsubscribe(GameEvents.OnStageChange, DespawnSelf);
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        float deltaTime = Time.deltaTime;

        transform.position +=
            direction * speed * deltaTime;

        timer += deltaTime;

        if (timer >= lifeTime)
        {
            DespawnSelf();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized)
            return;

        if (!IsInLayerMask(
                other.gameObject.layer,
                enemyLayer))
        {
            return;
        }

        ICombatUnit targetCombatUnit =
            other.GetComponent<ICombatUnit>();

        if (targetCombatUnit == null)
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
            DespawnSelf();
        }
    }

    private void DespawnSelf()
    {
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

        addressablePoolable.Despawn();
    }

    private void ResetRuntimeData()
    {
        isInitialized = false;

        sourceCombatUnit = null;

        direction = Vector3.zero;

        speed = 0f;
        lifeTime = 0f;
        timer = 0f;
        damage = 0f;
        
    }

    private static bool IsInLayerMask(
        int layer,
        LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}