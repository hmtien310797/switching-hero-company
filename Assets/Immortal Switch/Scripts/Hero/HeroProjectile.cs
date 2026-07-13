using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

public class HeroProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float hitDistance = 0.15f;
    [SerializeField] private float lifeTime = 3f;

    private ICombatUnit target;
    private ICombatUnit attacker;
    private float damage;
    private float timer;

    public void Init(ICombatUnit target, ICombatUnit attacker, float damage)
    {
        this.target = target;
        this.attacker = attacker;
        this.damage = damage;
        timer = lifeTime;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!IsTargetValid())
        {
            Destroy(gameObject);
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.Position;
        targetPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        float sqrDistance = (targetPosition - transform.position).sqrMagnitude;

        if (sqrDistance <= hitDistance * hitDistance)
        {
            DamageResult damageResult = DamageCalculator.CalculateDamage(attacker, target);
            target.TakeDamage(damageResult);
            HitEffectManager.Instance.Play(target);
            SkillEventBus.Raise(new SkillEventContext
            {
                EventType = SkillTriggerEventType.OnHit,
                Owner = attacker as HeroActor,
                Source = attacker,
                Target = target,
                Skill = null,

                DamageResult = damageResult
            });
            Destroy(gameObject);
        }
    }

    private bool IsTargetValid()
    {
        if (!target.IsUnityAlive())
        {
            return false;
        }
        
        return !target.IsDead;
    }
}