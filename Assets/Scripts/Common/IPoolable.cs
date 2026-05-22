namespace Common
{
    public interface IPoolable
    {
        void OnCreatedByPool(PoolHandle handle);
        void OnSpawnedFromPool();
        void OnDespawnedToPool();
    }
}