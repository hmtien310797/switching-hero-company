using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Pooling;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public interface ISkillObjectSpawner
    {
        /// <summary>
        /// Spawn SkillRuntimeObject theo SpawnMode trong config.
        ///
        /// AddressablePool:
        ///     dùng AddressablePoolService.
        ///
        /// AddressableInstance:
        ///     dùng AddressableSpawnService.
        /// </summary>
        UniTask<SkillRuntimeObject> SpawnRuntimeAsync(
            SkillRuntimeObjectConfig config,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null);

        /// <summary>
        /// API local pool cũ.
        /// Giữ lại tạm thời để không phá các child object chưa migrate.
        /// </summary>
        T Spawn<T>(
            T prefab,
            Vector3 position,
            Quaternion rotation)
            where T : Component, IPoolable;

        /// <summary>
        /// API local pool cũ.
        /// </summary>
        void Despawn<T>(T obj)
            where T : Component, IPoolable;
    }

    public sealed class PoolSkillObjectSpawner : ISkillObjectSpawner
    {
        public async UniTask<SkillRuntimeObject> SpawnRuntimeAsync(
            SkillRuntimeObjectConfig config,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            if (config == null)
            {
                Debug.LogError(
                    $"[{nameof(PoolSkillObjectSpawner)}] " +
                    "Cannot spawn runtime object because config is null."
                );

                return null;
            }

            if (string.IsNullOrWhiteSpace(config.RuntimeAddressableKey))
            {
                Debug.LogError(
                    $"[{nameof(PoolSkillObjectSpawner)}] " +
                    "RuntimeAddressableKey is null or empty."
                );

                return null;
            }

            switch (config.SpawnMode)
            {
                case SkillRuntimeSpawnMode.AddressablePool:
                    return SpawnAddressablePool(
                        config.RuntimeAddressableKey,
                        position,
                        rotation,
                        parent
                    );

                case SkillRuntimeSpawnMode.AddressableInstance:
                    string keyAsset = config.RuntimeAddressableKey;
                    switch (config.RuntimeVisualType)
                    {
                        case SkillRuntimeVisualType.SpawnHomingProjectile:
                            keyAsset = SkillRuntimeObjectConfig.HomingBulletSpawnerKey;
                            break;
                        case SkillRuntimeVisualType.SpawnProjectilePatternBehavior:
                        case SkillRuntimeVisualType.HeroSpineObjectAndProjectile:
                            keyAsset = SkillRuntimeObjectConfig.BulletPatternSpawnerKey;
                            break;
                    }
                    return await SpawnAddressableInstanceAsync(
                        keyAsset,
                        position,
                        rotation,
                        parent
                    );

                default:
                    Debug.LogError(
                        $"[{nameof(PoolSkillObjectSpawner)}] " +
                        $"Unsupported SpawnMode={config.SpawnMode}"
                    );

                    return null;
            }
        }

        private static SkillRuntimeObject SpawnAddressablePool(
            string addressableKey,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
        {
            AddressablePoolService poolService =
                AddressablePoolService.Instance;

            if (poolService == null)
            {
                Debug.LogError(
                    $"[{nameof(PoolSkillObjectSpawner)}] " +
                    "AddressablePoolService.Instance is null."
                );

                return null;
            }

            AddressablePoolHandle handle =
                poolService.SpawnWithHandle(
                    addressableKey,
                    position,
                    rotation,
                    parent
                );

            if (handle == null || handle.Instance == null)
                return null;

            if (!handle.Instance.TryGetComponent(
                    out SkillRuntimeObject runtimeObject))
            {
                Debug.LogError(
                    $"[{nameof(PoolSkillObjectSpawner)}] " +
                    $"Prefab does not contain {nameof(SkillRuntimeObject)} " +
                    $"on root. Key={addressableKey}"
                );

                handle.Despawn();
                return null;
            }

            /*
             * AddressableSkillRuntimePoolable.OnSpawned()
             * thường đã bind handle vào runtime object.
             *
             * Bind lại ở đây để bảo đảm runtime luôn giữ đúng handle.
             */
            runtimeObject.BindAddressablePoolSpawn(handle);

            return runtimeObject;
        }

        private static async UniTask<SkillRuntimeObject>
            SpawnAddressableInstanceAsync(
                string addressableKey,
                Vector3 position,
                Quaternion rotation,
                Transform parent)
        {
            SkillRuntimeObject runtimeObject =
                await AddressableSpawnService
                    .SpawnAsync<SkillRuntimeObject>(
                        prefix: string.Empty,
                        key: addressableKey,
                        position: position,
                        rotation: rotation,
                        parent: parent
                    );

            if (runtimeObject == null)
                return null;

            runtimeObject.BindAddressableInstanceSpawn();

            return runtimeObject;
        }

        #region Legacy Local Pool

        public T Spawn<T>(
            T prefab,
            Vector3 position,
            Quaternion rotation)
            where T : Component, IPoolable
        {
            if (prefab == null)
                return null;

            if (PoolManager.Instance != null)
            {
                return PoolManager.Instance.Spawn(
                    prefab,
                    position,
                    rotation
                );
            }

            T instance =
                Object.Instantiate(
                    prefab,
                    position,
                    rotation
                );

            instance.OnSpawnedFromPool();
            return instance;
        }

        public void Despawn<T>(T obj)
            where T : Component, IPoolable
        {
            if (obj == null)
                return;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(obj);
                return;
            }

            Object.Destroy(obj.gameObject);
        }

        #endregion
    }
}