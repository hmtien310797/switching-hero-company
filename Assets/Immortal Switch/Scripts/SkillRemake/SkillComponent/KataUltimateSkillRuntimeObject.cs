using System;
using System.Collections.Generic;
using System.Threading;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.SkillRemake.SkillComponent
{
    /// <summary>
    /// Controller for Kata's ultimate skill (2-phase bombing).
    ///
    /// Phase 1 — Carpet Bombing:
    ///   Summon N bombs in the air. Each bomb targets a random enemy
    ///   (prefers enemies not yet bombed this turn). Bombs drop with
    ///   a configured interval and deal AoE damage + Armor Break on hit.
    ///
    /// Phase 2 — Surprise Bomb:
    ///   After a brief delay, summon 1 large bomb at the area with the
    ///   highest concentration of enemies.
    ///
    /// Each child bomb is the "kata_ultimate_spine_skill" prefab with a
    /// SpineSkillRuntimeObject that plays the configured animation
    /// (ulti_bomb / ulti_final) and emits Spine events ("hit" / "finalhit")
    /// which trigger the phase actions configured in SkillDataSO.
    /// </summary>
    public class KataUltimateSkillRuntimeObject : SkillMultiSpawnRuntimeObject
    {
        [Header("Phase 1 — Carpet Bombing")]
        [SerializeField, Min(1)]
        private int phase1BombCount = 4;

        [SerializeField, Min(0f)]
        private float phase1DropInterval = 0.2f;

        [SerializeField]
        private string phase1AnimationName = "ulti_bomb";

        [Header("Phase 2 — Surprise Bomb")]
        [SerializeField, Min(0f)]
        private float phase2Delay = 0.8f;

        [SerializeField]
        private string phase2AnimationName = "ulti_final";

        [SerializeField, Min(0f)]
        private float phase2SearchRadius = 8f;

        // Track which enemies were targeted in Phase 1 so we can
        // prefer untargeted enemies for subsequent bombs.
        private readonly List<ICombatUnit> targetedEnemies = new();
        private readonly List<ICombatUnit> enemyBuffer = new();

        protected override async UniTask SpawnChildrenAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                SkillMultiSpawnConfig multiSpawnConfig = MultiSpawnConfig;

                if (multiSpawnConfig == null)
                    return;

                if (multiSpawnConfig.StartDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(
                            multiSpawnConfig.StartDelay
                        ),
                        cancellationToken: cancellationToken
                    );
                }

                targetedEnemies.Clear();

                // ── Phase 1: Carpet Bombing ──
                for (int i = 0; i < phase1BombCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await SpawnPhase1Bomb(i, cancellationToken);

                    if (i < phase1BombCount - 1 &&
                        phase1DropInterval > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(phase1DropInterval),
                            cancellationToken: cancellationToken
                        );
                    }
                }

                // ── Inter-phase delay ──
                if (phase2Delay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(phase2Delay),
                        cancellationToken: cancellationToken
                    );
                }

                // ── Phase 2: Surprise Bomb ──
                cancellationToken.ThrowIfCancellationRequested();

                await SpawnPhase2Bomb(cancellationToken);

                // ── Controller despawn ──
                if (multiSpawnConfig.DespawnDelayAfterLastSpawn > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(
                            multiSpawnConfig.DespawnDelayAfterLastSpawn
                        ),
                        cancellationToken: cancellationToken
                    );
                }

                cancellationToken.ThrowIfCancellationRequested();

                ForceDespawn();
            }
            catch (OperationCanceledException)
            {
                // Expected when object is despawned / re-initialised.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Spawn one Phase-1 bomb above a random enemy (prefers untargeted).
        /// </summary>
        private async UniTask SpawnPhase1Bomb(
            int index,
            CancellationToken cancellationToken)
        {
            ICombatUnit target = GetRandomEnemyPreferUntargeted();

            if (target == null ||
                !target.IsUnityAlive() ||
                target.IsDead)
            {
                return;
            }

            targetedEnemies.Add(target);

            // Spawn above the target so the bomb "drops" from the air.
            Vector3 spawnPosition = target.Position;
            spawnPosition.y += 5f;

            await SpawnBombWithAnimation(
                spawnPosition,
                phase1AnimationName,
                cancellationToken
            );
        }

        /// <summary>
        /// Spawn one Phase-2 bomb at the area with the most enemies.
        /// </summary>
        private async UniTask SpawnPhase2Bomb(
            CancellationToken cancellationToken)
        {
            Vector3 targetPosition = FindDensestEnemyArea();

            await SpawnBombWithAnimation(
                targetPosition,
                phase2AnimationName,
                cancellationToken
            );
        }

        /// <summary>
        /// Core spawn helper: instantiate a child bomb prefab at <paramref name="position"/>
        /// with the given <paramref name="animationName"/>, initialise it with a cloned
        /// context, and attach it to the skill pipeline.
        /// </summary>
        private async UniTask SpawnBombWithAnimation(
            Vector3 position,
            string animationName,
            CancellationToken cancellationToken)
        {
            SkillMultiSpawnConfig multiSpawnConfig = MultiSpawnConfig;

            if (Context == null ||
                Spawner == null ||
                multiSpawnConfig == null)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            Quaternion rotation = Quaternion.identity;

            SkillRuntimeObjectConfig childConfig =
                BuildChildConfig(multiSpawnConfig);

            if (childConfig == null)
                return;

            // Override the animation name so the child Spine object
            // plays the correct animation for this phase.
            childConfig.AnimationName = animationName;

            SkillRuntimeObject child =
                await Spawner.SpawnRuntimeAsync(
                    childConfig,
                    position,
                    rotation
                );

            if (child == null)
                return;

            if (cancellationToken.IsCancellationRequested)
            {
                child.ForceDespawn();
                return;
            }

            if (Context == null ||
                Executor == null ||
                TargetResolver == null ||
                Spawner == null)
            {
                child.ForceDespawn();
                return;
            }

            SkillRuntimeContext childContext =
                Context.CloneForRuntimeObject(child, position);

            child.Init(
                childContext,
                childConfig,
                Executor,
                TargetResolver,
                Spawner
            );
        }

        // ─────────────────────────────────────────────────────
        //  Targeting helpers
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Pick a random alive enemy, preferring one that hasn't been
        /// targeted by a previous Phase-1 bomb this cast.
        /// Falls back to a random enemy if all have been targeted.
        /// </summary>
        private ICombatUnit GetRandomEnemyPreferUntargeted()
        {
            enemyBuffer.Clear();

            IBattleTargetRegistry registry =
                Context?.BattleContext?.TargetRegistry;

            if (registry == null)
                return null;

            IReadOnlyList<ICombatUnit> allEnemies =
                registry.HostileTargets;

            for (int i = 0; i < allEnemies.Count; i++)
            {
                ICombatUnit enemy = allEnemies[i];

                if (enemy.IsUnityAlive() && !enemy.IsDead)
                    enemyBuffer.Add(enemy);
            }

            if (enemyBuffer.Count == 0)
                return null;

            // Prefer untargeted enemies.
            List<ICombatUnit> untargeted = new List<ICombatUnit>();

            for (int i = 0; i < enemyBuffer.Count; i++)
            {
                if (!targetedEnemies.Contains(enemyBuffer[i]))
                    untargeted.Add(enemyBuffer[i]);
            }

            if (untargeted.Count > 0)
                return untargeted[
                    Random.Range(0, untargeted.Count)
                ];

            // All enemies have been targeted → pick any.
            return enemyBuffer[
                Random.Range(0, enemyBuffer.Count)
            ];
        }

        /// <summary>
        /// Find the position with the highest density of alive enemies
        /// within <see cref="phase2SearchRadius"/>.
        /// </summary>
        private Vector3 FindDensestEnemyArea()
        {
            IBattleTargetRegistry registry =
                Context?.BattleContext?.TargetRegistry;

            if (registry == null)
                return transform.position;

            IReadOnlyList<ICombatUnit> allEnemies =
                registry.HostileTargets;

            Vector3 bestPosition = transform.position;
            int bestCount = 0;

            for (int i = 0; i < allEnemies.Count; i++)
            {
                ICombatUnit enemy = allEnemies[i];

                if (!enemy.IsUnityAlive() || enemy.IsDead)
                    continue;

                int count = TargetResolver.CountEnemiesInRange(
                    Context,
                    enemy.Position,
                    phase2SearchRadius
                );

                if (count > bestCount)
                {
                    bestCount = count;
                    bestPosition = enemy.Position;
                }
            }

            return bestPosition;
        }
    }
}