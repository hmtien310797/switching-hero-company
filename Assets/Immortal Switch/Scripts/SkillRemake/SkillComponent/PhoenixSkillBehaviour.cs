using System;
using System.Collections;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public class PhoenixSkillBehaviour: SkillMultiSpawnRuntimeObject
    {
        [SerializeField] private SkillRuntimeObjectConfig skillConfig;
        protected override async UniTask SpawnChild(int index)
        {
            SkillMultiSpawnConfig multiSpawnConfig =
                MultiSpawnConfig;

            if (Context == null ||
                Spawner == null ||
                multiSpawnConfig == null ||
                multiSpawnConfig.ChildRuntimePrefab == null)
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

            SkillRuntimeObject child = Spawner.Spawn(
                multiSpawnConfig.ChildRuntimePrefab,
                spawnPosition,
                rotation
            );

            if (child == null)
                return;

            SkillRuntimeObjectConfig childConfig =
                BuildChildConfig(multiSpawnConfig);

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

            if (Context == null ||
                Context.Caster == null ||
                child == null)
            {
                return;
            }

            var currentTarget =
                PvEBattleController.Instance
                    .GetRandomEnemyAlive();

            if (currentTarget == null ||
                currentTarget.IsDead)
            {
                return;
            }

            Context.Caster.SetTarget(currentTarget);
            child.transform.position =
                currentTarget.Position;
        }
    }
}