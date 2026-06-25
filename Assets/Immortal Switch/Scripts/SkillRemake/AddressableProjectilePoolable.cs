using UnityEngine;

namespace Immortal_Switch.Scripts.Pooling
{
    [DisallowMultipleComponent]
    public sealed class AddressableProjectilePoolable
        : AddressablePoolableBehaviour
    {
        private IAddressableProjectile projectile;
        private bool isDespawning;

        private void Awake()
        {
            CacheProjectile();
        }

        public override void OnSpawned(
            AddressablePoolHandle handle)
        {
            base.OnSpawned(handle);

            isDespawning = false;

            CacheProjectile();

            projectile?.OnProjectileSpawnedFromPool();
        }

        public override void OnDespawned()
        {
            CacheProjectile();

            projectile?.OnProjectileDespawnedToPool();

            isDespawning = false;

            base.OnDespawned();
        }

        /// <summary>
        /// Projectile gọi hàm này khi hết lifetime,
        /// hết target hoặc hoàn thành đường bay.
        /// </summary>
        public void Despawn()
        {
            if (isDespawning)
                return;

            isDespawning = true;

            AddressablePoolHandle handle = PoolHandle;

            if (handle == null)
            {
                Debug.LogError(
                    $"[{nameof(AddressableProjectilePoolable)}] " +
                    $"Missing AddressablePoolHandle. Object={name}",
                    this
                );

                gameObject.SetActive(false);
                isDespawning = false;
                return;
            }

            handle.Despawn();
        }

        private void CacheProjectile()
        {
            if (projectile != null)
                return;

            projectile =
                GetComponent<IAddressableProjectile>();

            if (projectile == null)
            {
                Debug.LogError(
                    $"[{nameof(AddressableProjectilePoolable)}] " +
                    $"Missing component implementing " +
                    $"{nameof(IAddressableProjectile)} on root. " +
                    $"Object={name}",
                    this
                );
            }
        }
    }
}