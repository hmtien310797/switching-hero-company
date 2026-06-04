using UnityEngine;

namespace Immortal_Switch.Scripts.Pooling
{
    public abstract class AddressablePoolableBehaviour : MonoBehaviour, IAddressablePoolable
    {
        protected AddressablePoolHandle PoolHandle { get; private set; }

        public virtual void OnSpawned(AddressablePoolHandle handle)
        {
            PoolHandle = handle;
        }

        public virtual void OnDespawned()
        {
            PoolHandle = null;
        }

        public void Despawn()
        {
            PoolHandle?.Despawn();
        }
    }
}