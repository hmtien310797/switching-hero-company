using System.Collections;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public class HomingChainBulletSkillRuntimeObject
        : SkillRuntimeObject
    {
        private HomingChainBulletConfig config;
        private Coroutine spawnCoroutine;

        protected override void OnRuntimeInitialized(object arg)
        {
            base.OnRuntimeInitialized(arg);

            if (Context == null ||
                Context.SkillData == null)
            {
                Debug.LogError(
                    $"[{nameof(HomingChainBulletSkillRuntimeObject)}] " +
                    $"Missing runtime context or SkillData.",
                    this
                );

                ForceDespawn();
                return;
            }

            if (Context.SkillData.OwnerType ==
                SkillOwnerType.ClassSkill)
            {
                config = Context.SkillData
                    .BasePhases[0]
                    .Actions[0]
                    .Projectile
                    .HomingChainBulletConfig;
            }
            else
            {
                config = Context.SkillData
                    .Levels[Context.SkillLevel - 1]
                    .Phases[0]
                    .Actions[0]
                    .Projectile
                    .HomingChainBulletConfig;
            }

            if (config == null)
            {
                Debug.LogError(
                    $"[{nameof(HomingChainBulletSkillRuntimeObject)}] " +
                    $"Missing HomingChainBulletConfig.",
                    this
                );

                ForceDespawn();
                return;
            }

            TryFire();
        }

        [Button]
        private void TryFire()
        {
            StopSpawnCoroutine();

            spawnCoroutine =
                StartCoroutine(SpawnBulletRoutine());
        }

        private IEnumerator SpawnBulletRoutine()
        {
            int count =
                Mathf.Max(1, config.bulletCount);

            for (int i = 0; i < count; i++)
            {
                SpawnOneBullet();

                if (config.delayBetweenBullets > 0f &&
                    i < count - 1)
                {
                    yield return new WaitForSeconds(
                        config.delayBetweenBullets
                    );
                }
            }

            spawnCoroutine = null;

            // Controller chỉ chịu trách nhiệm spawn đạn.
            // Mỗi viên đạn tự quản lý lifetime và tự trả pool.
            ForceDespawn();
        }

        private void SpawnOneBullet()
        {
            if (config == null)
                return;

            if (string.IsNullOrWhiteSpace(
                    config.bulletAddressableKey))
            {
                Debug.LogError(
                    $"[{nameof(HomingChainBulletSkillRuntimeObject)}] " +
                    $"Bullet Addressable key is null or empty.",
                    this
                );

                return;
            }

            AddressablePoolService poolService =
                AddressablePoolService.Instance;

            if (poolService == null)
            {
                Debug.LogError(
                    $"[{nameof(HomingChainBulletSkillRuntimeObject)}] " +
                    $"{nameof(AddressablePoolService)}.Instance is null.",
                    this
                );

                return;
            }

            Vector3 spawnPosition =
                transform.position;

            Vector3 initialDirection =
                transform.forward;

            initialDirection.y = 0f;

            if (initialDirection.sqrMagnitude <= 0.0001f)
            {
                initialDirection =
                    Vector3.forward;
            }

            initialDirection.Normalize();

            Quaternion rotation =
                Quaternion.LookRotation(
                    initialDirection,
                    Vector3.up
                );

            HomingChainBulletProjectile bullet =
                poolService.Spawn<HomingChainBulletProjectile>(
                    config.bulletAddressableKey,
                    spawnPosition,
                    rotation
                );

            if (bullet == null)
                return;

            if (Context == null ||
                Context.Caster == null)
            {
                AddressableProjectilePoolable poolable =
                    bullet.GetComponent<
                        AddressableProjectilePoolable>();

                poolable?.Despawn();
                return;
            }

            bullet.Setup(
                Context.Caster, Config,
                spawnPosition,
                initialDirection,
                config, Context.BattleContext
            );
        }

        private void StopSpawnCoroutine()
        {
            if (spawnCoroutine == null)
                return;

            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        private void OnDisable()
        {
            StopSpawnCoroutine();
        }
    }
}