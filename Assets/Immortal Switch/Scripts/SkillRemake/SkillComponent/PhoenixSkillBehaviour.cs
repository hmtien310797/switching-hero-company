using System;
using System.Threading;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public class PhoenixSkillBehaviour : SkillMultiSpawnRuntimeObject
    {
        protected override async UniTask SpawnChild(int index, CancellationToken cancellationToken)
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

            cancellationToken.ThrowIfCancellationRequested();
            
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

            await UniTask.Delay(
                TimeSpan.FromSeconds(1.7f)
            );

            /*
             * Sau thời gian bay lên, child hoặc controller đều có thể
             * đã kết thúc. Unity object đã destroy/despawn sẽ trả về null
             * qua operator ==.
             */
            if (child == null ||
                Context == null ||
                Context.Caster == null ||
                Context.Caster.IsDead)
            {
                return;
            }

            PvEBattleController battleController =
                PvEBattleController.Instance;

            if (battleController == null)
                return;

            var currentTarget =
                battleController.GetRandomEnemyAlive();

            if (currentTarget == null ||
                currentTarget.IsDead)
            {
                return;
            }

            Context.Caster.SetTarget(currentTarget);

            child.transform.position =
                currentTarget.Position;
        }
        
         protected override async UniTask SpawnChildrenAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                SkillMultiSpawnConfig multiSpawnConfig =
                    MultiSpawnConfig;

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

                int spawnCount = Mathf.Max(
                    1,
                    multiSpawnConfig.SpawnCount
                );

                for (int i = 0; i < spawnCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SpawnChild(
                        i,
                        cancellationToken
                    ).Forget();

                    if (multiSpawnConfig.SpawnInterval > 0f &&
                        i < spawnCount - 1)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(
                                multiSpawnConfig.SpawnInterval
                            ),
                            cancellationToken: cancellationToken
                        );
                    }
                }

                if (!multiSpawnConfig.DespawnControllerAfterSpawn)
                    return;

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
                // Task bị hủy khi object despawn, re-init hoặc bị destroy.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}