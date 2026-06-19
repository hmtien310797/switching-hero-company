using Common;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

public class BulletProjectile : PoolableBehaviour
{
    [Header("Collision")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool despawnOnHit = true;

    private Vector3 direction;
    private float speed;
    private float lifeTime;
    private float timer;
    private bool isInitialized;
    private float damage;
    private ICombatUnit sourceCombatUnit;
    

    public void Setup(ICombatUnit source, Vector3 moveDirection, float bulletSpeed, float bulletLifeTime, float damage)
    {
        direction = moveDirection.normalized;
        speed = bulletSpeed;
        lifeTime = bulletLifeTime;
        timer = 0f;
        isInitialized = true;
        this.damage = damage;
        sourceCombatUnit = source;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    public override void OnSpawnedFromPool()
    {
        base.OnSpawnedFromPool();

        timer = 0f;
        isInitialized = false;
    }

    public override void OnDespawnedToPool()
    {
        base.OnDespawnedToPool();

        isInitialized = false;
        timer = 0f;
        direction = Vector3.zero;
        speed = 0f;
        lifeTime = 0f;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        float dt = Time.deltaTime;

        transform.position += direction * speed * dt;

        timer += dt;
        if (timer >= lifeTime)
        {
            DespawnSelf();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized)
            return;

        if (!IsInLayerMask(other.gameObject.layer, enemyLayer))
            return;

        ICombatUnit combatUnit = other.GetComponent<ICombatUnit>();
        if(combatUnit == null)
            return;
        
        DamageResult damageResult = DamageCalculator.CalculateDamage(sourceCombatUnit, combatUnit, damage);
        combatUnit.TakeDamage(damageResult);
        
        if (despawnOnHit)
        {
            DespawnSelf();
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}