using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    /// <summary>
    /// Spawn và khởi tạo Enemy/Boss dùng chung cho PvE và Dungeon.
    /// Không chứa Chapter logic, Dungeon rule, counter, UI hoặc reward.
    /// </summary>
    public sealed class BattleEnemySpawnService
    {
        public EnemyActor SpawnCreep(
            CreepDataSo creepData,
            Vector3 position,
            IEnemyTargetProvider targetProvider,
            IBattleTargetRegistry targetRegistry,
            BaseStat cachedBaseStat,
            Action<EnemyActor> onDead = null)
        {
            if (!CanSpawnCreep(creepData, targetProvider, targetRegistry))
                return null;

            EnemyActor creep = AddressablePoolService.Instance.Spawn<EnemyActor>(
                creepData.CreepAddressKey,
                position,
                Quaternion.identity
            );

            if (creep == null)
            {
                Debug.LogError(
                    $"[BattleEnemySpawnService] Failed to spawn creep. " +
                    $"EnemyId={creepData.Id}, PoolKey={creepData.CreepAddressKey}"
                );
                return null;
            }

            creep.name = $"Creep_{creepData.Id}_{creep.transform.GetInstanceID()}";
            creep.Init(creepData, targetProvider, cachedBaseStat);
            BindCreepDeath(creep, onDead);
            targetRegistry.RegisterHostile(creep);
            return creep;
        }

        public EnemyActor SpawnCreep(
            CreepDataSo creepData,
            Vector3 position,
            IEnemyTargetProvider targetProvider,
            IBattleTargetRegistry targetRegistry,
            StageStatScale scale,
            Action<EnemyActor> onDead = null)
        {
            if (!CanSpawnCreep(creepData, targetProvider, targetRegistry))
                return null;

            EnemyActor creep = AddressablePoolService.Instance.Spawn<EnemyActor>(
                creepData.CreepAddressKey,
                position,
                Quaternion.identity
            );

            if (creep == null)
            {
                Debug.LogError(
                    $"[BattleEnemySpawnService] Failed to spawn creep. " +
                    $"EnemyId={creepData.Id}, PoolKey={creepData.CreepAddressKey}"
                );
                return null;
            }

            creep.name = $"Creep_{creepData.Id}_{creep.transform.GetInstanceID()}";
            creep.Init(creepData, targetProvider, scale);
            BindCreepDeath(creep, onDead);
            targetRegistry.RegisterHostile(creep);
            return creep;
        }

        public async UniTask<BossActor> SpawnBossAsync(
            BossDataSO bossData,
            Vector3 position,
            IEnemyTargetProvider targetProvider,
            IBattleTargetRegistry targetRegistry,
            BaseStat cachedBaseStat,
            Action<BossActor> onDead = null)
        {
            if (!CanSpawnBoss(bossData, targetProvider, targetRegistry))
                return null;

            BossActor boss = await SpawnBossInstanceAsync(bossData, position);
            if (boss == null)
                return null;

            boss.Init(bossData, targetProvider, cachedBaseStat);
            BindBossDeath(boss, onDead);
            targetRegistry.RegisterHostile(boss);
            return boss;
        }

        public async UniTask<BossActor> SpawnBossAsync(
            BossDataSO bossData,
            Vector3 position,
            IEnemyTargetProvider targetProvider,
            IBattleTargetRegistry targetRegistry,
            StageStatScale scale,
            Action<BossActor> onDead = null)
        {
            if (!CanSpawnBoss(bossData, targetProvider, targetRegistry))
                return null;

            BossActor boss = await SpawnBossInstanceAsync(bossData, position);
            if (boss == null)
                return null;

            boss.Init(bossData, targetProvider, scale);
            BindBossDeath(boss, onDead);
            targetRegistry.RegisterHostile(boss);
            return boss;
        }

        private static bool CanSpawnCreep(
            CreepDataSo creepData,
            IEnemyTargetProvider targetProvider,
            IBattleTargetRegistry targetRegistry)
        {
            if (creepData == null)
            {
                Debug.LogError("[BattleEnemySpawnService] CreepData is null.");
                return false;
            }

            if (string.IsNullOrEmpty(creepData.CreepAddressKey))
            {
                Debug.LogError(
                    $"[BattleEnemySpawnService] CreepAddressKey is empty. EnemyId={creepData.Id}"
                );
                return false;
            }

            if (targetProvider == null || targetRegistry == null)
            {
                Debug.LogError("[BattleEnemySpawnService] Target provider or registry is null.");
                return false;
            }

            return true;
        }

        private static bool CanSpawnBoss(
            BossDataSO bossData,
            IEnemyTargetProvider targetProvider,
            IBattleTargetRegistry targetRegistry)
        {
            if (bossData == null)
            {
                Debug.LogError("[BattleEnemySpawnService] BossData is null.");
                return false;
            }

            if (string.IsNullOrEmpty(bossData.BossAddressKey))
            {
                Debug.LogError(
                    $"[BattleEnemySpawnService] BossAddressKey is empty. BossId={bossData.Id}"
                );
                return false;
            }

            if (targetProvider == null || targetRegistry == null)
            {
                Debug.LogError("[BattleEnemySpawnService] Target provider or registry is null.");
                return false;
            }

            return true;
        }

        private static async UniTask<BossActor> SpawnBossInstanceAsync(
            BossDataSO bossData,
            Vector3 position)
        {
            BossActor boss = await AddressableSpawnService.SpawnAsync<BossActor>(
                string.Empty,
                bossData.BossAddressKey,
                position,
                Quaternion.identity
            );

            if (boss == null)
            {
                Debug.LogError(
                    $"[BattleEnemySpawnService] Failed to spawn boss. " +
                    $"BossId={bossData.Id}, AddressKey={bossData.BossAddressKey}"
                );
            }

            return boss;
        }

        private static void BindCreepDeath(
            EnemyActor creep,
            Action<EnemyActor> onDead)
        {
            if (onDead == null)
                return;

            creep.OnDead -= onDead;
            creep.OnDead += onDead;
        }

        private static void BindBossDeath(
            BossActor boss,
            Action<BossActor> onDead)
        {
            if (onDead == null)
                return;

            boss.OnDead -= onDead;
            boss.OnDead += onDead;
        }
    }
}
