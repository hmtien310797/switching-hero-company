using System.Collections;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Runtime object used for skills that spawn many child skill objects over time.
    /// Example: Holy Sword Rain spawns 10 sword Spine objects around the target area.
    ///
    /// The parent/controller does not deal damage directly. Each child runtime object receives
    /// the same SkillDataSO/context, so child Spine events like "hit" execute the phases/actions
    /// configured on the parent SkillDataSO.
    /// </summary>
    public class SkillMultiSpawnRuntimeObject : SkillRuntimeObject
    {
        [Header("Child Runtime Object")]
        [SerializeField] protected SkillRuntimeObject childRuntimePrefab;
        [SerializeField] protected string childAnimationName;
        [SerializeField] protected bool childLoopAnimation;
        [SerializeField] protected bool childUseLifeTime = true;
        [SerializeField] protected float childLifeTime = 1.5f;
        [SerializeField] protected bool childDespawnOnAnimationComplete = true;

        [Header("Spawn Pattern")]
        [SerializeField, Min(1)] protected int spawnCount = 10;
        [SerializeField, Min(0f)] protected float startDelay = 0f;
        [SerializeField, Min(0f)] protected float spawnInterval = 0.15f;
        [SerializeField, Min(0f)] protected float spawnRadius = 2.5f;
        [SerializeField] protected bool randomInsideCircle = true;
        [SerializeField] protected bool includeCenterAsFirstSpawn = true;

        [Header("Position Offset")]
        [SerializeField] protected Vector3 childSpawnOffset;
        [SerializeField] protected bool randomizeYRotation;

        [Header("Controller Lifetime")]
        [SerializeField] protected bool despawnControllerAfterSpawn = true;
        [SerializeField, Min(0f)] protected float despawnDelayAfterLastSpawn = 0.25f;

        [Header("Debug")]
        [SerializeField] protected bool debugDrawSpawnRadius;

        protected Coroutine spawnRoutine;

        protected override void OnRuntimeInitialized()
        {
            base.OnRuntimeInitialized();

            if (childRuntimePrefab == null)
            {
                Debug.LogWarning($"[{nameof(SkillMultiSpawnRuntimeObject)}] Missing childRuntimePrefab on {name}.");
                return;
            }

            if (spawnRoutine != null)
                StopCoroutine(spawnRoutine);

            spawnRoutine = StartCoroutine(SpawnChildrenRoutine());
        }

        private IEnumerator SpawnChildrenRoutine()
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnChild(i);

                if (spawnInterval > 0f && i < spawnCount - 1)
                    yield return new WaitForSeconds(spawnInterval);
            }

            spawnRoutine = null;

            if (despawnControllerAfterSpawn)
            {
                if (despawnDelayAfterLastSpawn > 0f)
                    yield return new WaitForSeconds(despawnDelayAfterLastSpawn);

                ForceDespawn();
            }
        }

        protected virtual async UniTask SpawnChild(int index)
        {
            if (Context == null || Spawner == null || childRuntimePrefab == null)
                return;

            Vector3 spawnPosition = GetChildSpawnPosition(index);
            Quaternion rotation = randomizeYRotation
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : Quaternion.identity;

            SkillRuntimeObject child = Spawner.Spawn(childRuntimePrefab, spawnPosition, rotation);
            if (child == null)
                return;

            SkillRuntimeObjectConfig childConfig = BuildChildConfig();
            SkillRuntimeContext childContext = Context.CloneForRuntimeObject(child, spawnPosition);

            child.Init(childContext, childConfig, Executor, TargetResolver, Spawner);
        }

        protected Vector3 GetChildSpawnPosition(int index)
        {
            Vector3 center = transform.position;

            if (includeCenterAsFirstSpawn && index == 0)
                return center + childSpawnOffset;

            Vector2 offset2D;

            if (randomInsideCircle)
            {
                offset2D = Random.insideUnitCircle * spawnRadius;
            }
            else
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                offset2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            }

            // Game plane is XZ.
            Vector3 offset = new Vector3(offset2D.x, 0f, offset2D.y);
            return center + offset + childSpawnOffset;
        }

        protected SkillRuntimeObjectConfig BuildChildConfig()
        {
            // Child object reuses the same skill context/data, but has its own visual/lifetime config.
            // Its Spine events will execute phases from Context.SkillData.
            return new SkillRuntimeObjectConfig
            {
                RuntimeVisualType = SkillRuntimeVisualType.SpawnedSkillObject,
                RuntimePrefab = childRuntimePrefab,
                SpawnPositionType = SkillSpawnPositionType.CastPosition,
                FollowType = SkillFollowType.None,
                SpawnOffset = Vector3.zero,

                UseLifeTime = childUseLifeTime,
                LifeTime = childLifeTime,
                DespawnOnAnimationComplete = childDespawnOnAnimationComplete,

                AnimationName = childAnimationName,
                LoopAnimation = childLoopAnimation,
                LockCasterWhileAlive = false
            };
        }

        public override void ForceDespawn()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            base.ForceDespawn();
        }

        public override void OnDespawnedToPool()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            base.OnDespawnedToPool();
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawSpawnRadius)
                return;

            Gizmos.color = Color.yellow;

            Vector3 center = transform.position;
            const int segments = 64;
            Vector3 previous = center + new Vector3(spawnRadius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(
                    Mathf.Cos(angle) * spawnRadius,
                    0f,
                    Mathf.Sin(angle) * spawnRadius
                );

                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
