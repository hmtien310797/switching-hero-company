using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake.SkillComponent
{
    public class VoidThunderFallSkillMultiSpawnRuntimeObjectConfig : SkillMultiSpawnRuntimeObject
    {
        protected override async UniTask SpawnChild(
            int index,
            CancellationToken cancellationToken)
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

            if (index == multiSpawnConfig.SpawnCount - 1)
            {
                child.gameObject.transform.localScale = Vector3.one * 2f;
            }

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
        }
        
        protected override Vector3 GetChildSpawnPosition(int index)
        {
            SkillMultiSpawnConfig multiSpawnConfig = MultiSpawnConfig;

            if (multiSpawnConfig == null)
                return transform.position;

            Vector3 center = transform.position;

            if (index == multiSpawnConfig.SpawnCount - 1)
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
    }
}