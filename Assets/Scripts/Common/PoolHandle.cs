using Sirenix.OdinInspector;
using UnityEngine;

namespace Common
{
    public sealed class PoolHandle : MonoBehaviour
    {
        public GameObject PrefabKey { get; private set; }
        public ObjectPool OwnerPool { get; private set; }
        [ShowInInspector]
        public bool IsInPool { get; private set; }

        public void Init(GameObject prefabKey, ObjectPool ownerPool)
        {
            PrefabKey = prefabKey;
            OwnerPool = ownerPool;
            IsInPool = false;
        }

        public void MarkSpawned()
        {
            IsInPool = false;
        }

        public void MarkDespawned()
        {
            IsInPool = true;
        }

        public void Despawn()
        {
            if (OwnerPool == null)
            {
                Debug.LogWarning($"[PoolHandle] {name} has no owner pool. Destroy instead.");
                Destroy(gameObject);
                return;
            }

            OwnerPool.Despawn(gameObject);
        }
    }
}