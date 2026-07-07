namespace Common
{
    public interface IPoolable
    {
        int PoolSize { get; set; }
        bool DeSpawnedOnStart { get; }
        void OnCreatedByPool(PoolHandle handle);
        void OnSpawnedFromPool();
        void OnDespawnedToPool();
    }
}