using Common;
using UnityEngine;
using Battle;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillProjectileRuntime : PoolableBehaviour
    {
        private SkillRuntimeContext context;
        private SkillProjectileData data;
        private SkillExecutor executor;
        private ISkillObjectSpawner spawner;
        private ICombatUnit target;
        private Vector3 direction;
        private float lifeTimer;
        private int hitCount;
        private bool initialized;

        public void Init(
            SkillRuntimeContext context,
            SkillProjectileData data,
            SkillExecutor executor,
            ISkillObjectSpawner spawner)
        {
            this.context = context;
            this.data = data;
            this.executor = executor;
            this.spawner = spawner;
            target = context.MainTarget;
            lifeTimer = Mathf.Max(0.01f, data.LifeTime);
            hitCount = 0;
            initialized = true;

            if (target != null)
            {
                direction = target.Position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    direction.Normalize();
            }
            else
            {
                direction = transform.forward;
            }
        }

        private void Update()
        {
            if (!initialized || data == null)
                return;

            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                DespawnSelf();
                return;
            }

            Move();
            CheckHit();
        }

        private void Move()
        {
            if (data.MoveType == SkillProjectileMoveType.Homing && target != null && !target.IsDead)
            {
                Vector3 toTarget = target.Position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                    direction = toTarget.normalized;
            }

            transform.position += direction * data.Speed * Time.deltaTime;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void CheckHit()
        {
            if (target == null || target.IsDead)
                return;

            if (data.HitDetectionType != SkillProjectileHitDetectionType.DistanceToTarget)
                return;

            Vector3 self = transform.position;
            Vector3 targetPos = target.Position;
            self.y = 0f;
            targetPos.y = 0f;

            if ((targetPos - self).sqrMagnitude > data.HitRadius * data.HitRadius)
                return;

            Hit(target);
        }

        private void Hit(ICombatUnit hitTarget)
        {
            if (executor == null || context == null || hitTarget == null)
                return;

            executor.ExecuteNestedActions(context.CloneForTarget(hitTarget), data.OnHitActions);
            hitCount++;

            if (!data.Pierce || hitCount > data.PierceCount)
                DespawnSelf();
        }

        public override void OnDespawnedToPool()
        {
            initialized = false;
            context = null;
            data = null;
            executor = null;
            spawner = null;
            target = null;
            direction = Vector3.zero;
            hitCount = 0;
        }
    }
}
