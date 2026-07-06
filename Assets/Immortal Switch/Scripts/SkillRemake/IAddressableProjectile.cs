namespace Immortal_Switch.Scripts.Pooling
{
    public interface IAddressableProjectile
    {
        void OnProjectileSpawnedFromPool();

        void OnProjectileDespawnedToPool();
    }
}