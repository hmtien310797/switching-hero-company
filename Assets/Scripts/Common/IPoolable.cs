namespace Common
{
    public interface IPoolable
    {
        bool DeSpawnedOnStart { get; }
        void OnCreatedByPool(PoolHandle handle);
        void OnSpawnedFromPool();
        void OnDespawnedToPool();
    }
}