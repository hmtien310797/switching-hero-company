using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public sealed class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        [SerializeField] private int defaultPrewarmCount = 0;

        private readonly Dictionary<GameObject, ObjectPool> pools = new();
        private readonly Dictionary<GameObject, Transform> poolParents = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Prewarm<T>(T prefab, int count) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Prewarm failed. Prefab is null.");
                return;
            }

            ObjectPool pool = GetOrCreatePool(prefab.gameObject);
            pool.Prewarm(count);
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Spawn failed. Prefab is null.");
                return null;
            }

            ObjectPool pool = GetOrCreatePool(prefab.gameObject);
            return pool.Spawn<T>(position, rotation);
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Spawn failed. Prefab is null.");
                return null;
            }

            ObjectPool pool = GetOrCreatePool(prefab.gameObject);
            return pool.Spawn<T>(position, rotation, parent);
        }

        public void Despawn<T>(T obj) where T : Component, IPoolable
        {
            if (obj == null)
                return;

            PoolHandle handle = obj.GetComponent<PoolHandle>();

            if (handle == null || handle.OwnerPool == null)
            {
                Debug.LogWarning($"[PoolManager] {obj.name} does not belong to any pool. Destroy instead.");
                Destroy(obj.gameObject);
                return;
            }

            handle.OwnerPool.Despawn(obj.gameObject);
        }

        public void ClearPool<T>(T prefab) where T : Component, IPoolable
        {
            if (prefab == null)
                return;

            GameObject prefabObj = prefab.gameObject;

            if (pools.TryGetValue(prefabObj, out ObjectPool pool))
            {
                pool.Clear();
                pools.Remove(prefabObj);
            }

            if (poolParents.TryGetValue(prefabObj, out Transform parent))
            {
                if (parent != null)
                {
                    Destroy(parent.gameObject);
                }

                poolParents.Remove(prefabObj);
            }
        }

        private ObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out ObjectPool existingPool))
            {
                return existingPool;
            }

            if (!prefab.TryGetComponent(out IPoolable _))
            {
                Debug.LogError($"[PoolManager] Prefab {prefab.name} must implement IPoolable to use pool.");
                return null;
            }

            Transform parent = CreatePoolParent(prefab);
            ObjectPool newPool = new ObjectPool(prefab, parent, defaultPrewarmCount);

            pools.Add(prefab, newPool);

            return newPool;
        }

        private Transform CreatePoolParent(GameObject prefab)
        {
            GameObject parentObj = new GameObject($"{prefab.name}_Pool");
            parentObj.transform.SetParent(transform);

            Transform parent = parentObj.transform;
            poolParents.Add(prefab, parent);

            return parent;
        }
    }
}