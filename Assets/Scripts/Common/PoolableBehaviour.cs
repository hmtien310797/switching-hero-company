using UnityEngine;

namespace Common
{
    public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
    {
        protected PoolHandle PoolHandle { get; private set; }

        [field: SerializeField] public int PoolSize { get; set; }
        [field: SerializeField] public bool DeSpawnedOnStart { get; private set; } = true;

        public void OnCreatedByPool(PoolHandle handle)
        {
            PoolHandle = handle;
            OnCreated();
        }

        protected virtual void OnCreated()
        {
        }

        public virtual void OnSpawnedFromPool()
        {
        }

        public virtual void OnDespawnedToPool()
        {
        }

        protected void DespawnSelf(float delay = 0f)
        {
            if (PoolHandle != null)
            {
                PoolHandle.Despawn(delay);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}