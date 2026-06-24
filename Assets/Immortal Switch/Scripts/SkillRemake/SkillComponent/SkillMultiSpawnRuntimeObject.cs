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
        protected SkillMultiSpawnConfig MultiSpawnConfig
        {
            get
            {
                return Config != null
                    ? Config.MultiSpawnConfig
                    : null;
            }
        }
        protected Coroutine spawnRoutine;

        protected override void OnRuntimeInitialized(object arg)
        {
            base.OnRuntimeInitialized(arg);

            SkillMultiSpawnConfig multiSpawnConfig = MultiSpawnConfig;

            if (multiSpawnConfig == null)
            {
                Debug.LogWarning(
                    $"[{nameof(SkillMultiSpawnRuntimeObject)}] Missing MultiSpawnConfig on skill data.",
                    this
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    multiSpawnConfig.ChildRuntimeAddressableKey))
            {
                Debug.LogWarning(
                    $"[{nameof(SkillMultiSpawnRuntimeObject)}] " +
                    $"Missing ChildRuntimeAddressableKey in MultiSpawnConfig.",
                    this
                );

                return;
            }

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            spawnRoutine = StartCoroutine(SpawnChildrenRoutine());
        }

        private IEnumerator SpawnChildrenRoutine()
        {
            SkillMultiSpawnConfig multiSpawnConfig = MultiSpawnConfig;

            if (multiSpawnConfig == null)
            {
                spawnRoutine = null;
                yield break;
            }

            if (multiSpawnConfig.StartDelay > 0f)
            {
                yield return new WaitForSeconds(
                    multiSpawnConfig.StartDelay
                );
            }

            int spawnCount = Mathf.Max(
                1,
                multiSpawnConfig.SpawnCount
            );

            for (int i = 0; i < spawnCount; i++)
            {
                yield return SpawnChild(i).ToCoroutine();

                if (multiSpawnConfig.SpawnInterval > 0f &&
                    i < spawnCount - 1)
                {
                    yield return new WaitForSeconds(
                        multiSpawnConfig.SpawnInterval
                    );
                }
            }

            spawnRoutine = null;

            if (!multiSpawnConfig.DespawnControllerAfterSpawn)
                yield break;

            if (multiSpawnConfig.DespawnDelayAfterLastSpawn > 0f)
            {
                yield return new WaitForSeconds(
                    multiSpawnConfig.DespawnDelayAfterLastSpawn
                );
            }

            ForceDespawn();
        }

        protected virtual async UniTask SpawnChild(int index)
        {
            SkillMultiSpawnConfig multiSpawnConfig =
                MultiSpawnConfig;

            if (Context == null ||
                Spawner == null ||
                multiSpawnConfig == null ||
                string.IsNullOrWhiteSpace(
                    multiSpawnConfig.ChildRuntimeAddressableKey))
            {
                return;
            }

            Vector3 spawnPosition =
                GetChildSpawnPosition(index);

            Quaternion rotation =
                multiSpawnConfig.RandomizeYRotation
                    ? Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f
                    )
                    : Quaternion.identity;

            SkillRuntimeObjectConfig childConfig =
                BuildChildConfig(multiSpawnConfig);

            if (childConfig == null)
                return;

            SkillRuntimeObject child =
                await Spawner.SpawnRuntimeAsync(
                    childConfig,
                    spawnPosition,
                    rotation
                );

            if (child == null)
                return;

            // Sau await phải kiểm tra lại controller.
            if (Context == null ||
                Executor == null ||
                TargetResolver == null ||
                Spawner == null)
            {
                child.ForceDespawn();
                return;
            }

            SkillRuntimeContext childContext =
                Context.CloneForRuntimeObject(
                    child,
                    spawnPosition
                );

            child.Init(
                childContext,
                childConfig,
                Executor,
                TargetResolver,
                Spawner
            );
        }

        protected Vector3 GetChildSpawnPosition(int index)
        {
            SkillMultiSpawnConfig multiSpawnConfig = MultiSpawnConfig;

            if (multiSpawnConfig == null)
                return transform.position;

            Vector3 center = transform.position;

            if (multiSpawnConfig.IncludeCenterAsFirstSpawn &&
                index == 0)
            {
                return center +
                       multiSpawnConfig.ChildSpawnOffset;
            }

            Vector2 offset2D;

            if (multiSpawnConfig.RandomInsideCircle)
            {
                offset2D =
                    Random.insideUnitCircle *
                    multiSpawnConfig.SpawnRadius;
            }
            else
            {
                float angle = Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

                offset2D = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * multiSpawnConfig.SpawnRadius;
            }

            // Mặt phẳng gameplay là XZ.
            Vector3 offset = new Vector3(
                offset2D.x,
                0f,
                offset2D.y
            );

            return center +
                   offset +
                   multiSpawnConfig.ChildSpawnOffset;
        }

        protected SkillRuntimeObjectConfig BuildChildConfig(
            SkillMultiSpawnConfig multiSpawnConfig)
        {
            if (multiSpawnConfig == null)
                return null;

            return new SkillRuntimeObjectConfig
            {
                RuntimeVisualType =
                    SkillRuntimeVisualType.SpawnedSkillObject,

                SpawnMode =
                    multiSpawnConfig.ChildSpawnMode,

                RuntimeAddressableKey =
                    multiSpawnConfig.ChildRuntimeAddressableKey,

                SpawnPositionType =
                    SkillSpawnPositionType.CastPosition,

                FollowType =
                    SkillFollowType.None,

                SpawnOffset =
                    Vector3.zero,

                UseLifeTime =
                    multiSpawnConfig.ChildUseLifeTime,

                LifeTime =
                    multiSpawnConfig.ChildLifeTime,

                DespawnOnAnimationComplete =
                    multiSpawnConfig.ChildDespawnOnAnimationComplete,

                AnimationName =
                    multiSpawnConfig.ChildAnimationName,

                LoopAnimation =
                    multiSpawnConfig.ChildLoopAnimation,

                LockCasterWhileAlive =
                    false,

                LockCasterDuringHeroAnimation =
                    false
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
            SkillMultiSpawnConfig multiSpawnConfig =
                MultiSpawnConfig;

            if (multiSpawnConfig == null ||
                !multiSpawnConfig.DebugDrawSpawnRadius)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            Vector3 center = transform.position;
            const int segments = 64;

            float spawnRadius =
                multiSpawnConfig.SpawnRadius;

            Vector3 previous =
                center +
                new Vector3(
                    spawnRadius,
                    0f,
                    0f
                );

            for (int i = 1; i <= segments; i++)
            {
                float angle =
                    (float)i /
                    segments *
                    Mathf.PI *
                    2f;

                Vector3 next =
                    center +
                    new Vector3(
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
