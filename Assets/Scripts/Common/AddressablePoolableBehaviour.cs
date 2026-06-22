using System;
using Cysharp.Threading.Tasks;
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

        public virtual async UniTask DespawnToPool(float delaySpawn)
        {
            if (delaySpawn >= 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySpawn));
            }
            PoolHandle?.Despawn();
        }
    }
}