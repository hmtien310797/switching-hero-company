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
            if (Context == null || Spawner == null || childRuntimePrefab == null)
                return;

            Vector3 spawnPosition = GetChildSpawnPosition(index);
            Quaternion rotation = randomizeYRotation
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : Quaternion.identity;

            SkillRuntimeObject child = Spawner.Spawn(childRuntimePrefab, spawnPosition, rotation);
            if (child == null)
                return;
            
            SkillRuntimeContext childContext = Context.CloneForRuntimeObject(child, spawnPosition);

            child.Init(childContext, skillConfig, Executor, TargetResolver, Spawner);

            await UniTask.Delay(TimeSpan.FromSeconds(1.7f));

            var currentTarget = PvEBattleController.Instance.GetRandomEnemyAlive();
            Context.Caster.SetTarget(currentTarget);
            
            child.transform.position = currentTarget.Position;
            
        }
    }
}