using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public sealed class ObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Queue<GameObject> inactiveObjects = new Queue<GameObject>();

        public GameObject Prefab => prefab;
        public int InactiveCount => inactiveObjects.Count;

        public ObjectPool(GameObject prefab, Transform parent, int prewarmCount)
        {
            this.prefab = prefab;
            this.parent = parent;

            ValidatePrefab();
            Prewarm(prewarmCount);
        }

        private void ValidatePrefab()
        {
            if (!prefab.TryGetComponent(out IPoolable _))
            {
                Debug.LogError($"[ObjectPool] Prefab {prefab.name} does not implement IPoolable.");
            }
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewObject();
                Despawn(obj);
            }
        }

        private GameObject CreateNewObject()
        {
            if (!prefab.TryGetComponent(out IPoolable _))
            {
                Debug.LogError($"[ObjectPool] Cannot create pooled object. Prefab {prefab.name} does not implement IPoolable.");
                return null;
            }

            GameObject obj = Object.Instantiate(prefab, parent);
            obj.name = prefab.name;

            PoolHandle handle = obj.GetComponent<PoolHandle>();
            if (handle == null)
            {
                handle = obj.AddComponent<PoolHandle>();
            }

            handle.Init(prefab, this);

            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable.OnCreatedByPool(handle);

            obj.SetActive(false);

            return obj;
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform customParent = null)
        {
            GameObject obj = inactiveObjects.Count > 0
                ? inactiveObjects.Dequeue()
                : CreateNewObject();

            if (obj == null)
                return null;

            Transform objTransform = obj.transform;

            objTransform.SetParent(customParent != null ? customParent : parent, false);
            objTransform.SetPositionAndRotation(position, rotation);

            PoolHandle handle = obj.GetComponent<PoolHandle>();
            handle.MarkSpawned();

            obj.SetActive(true);

            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable.OnSpawnedFromPool();

            return obj;
        }

        public T Spawn<T>(Vector3 position, Quaternion rotation, Transform customParent = null) where T : Component, IPoolable
        {
            GameObject obj = Spawn(position, rotation, customParent);

            if (obj == null)
                return null;

            return obj.GetComponent<T>();
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null)
                return;

            if (!obj.TryGetComponent(out IPoolable poolable))
            {
                Debug.LogWarning($"[ObjectPool] {obj.name} does not implement IPoolable. Destroy instead.");
                Object.Destroy(obj);
                return;
            }

            PoolHandle handle = obj.GetComponent<PoolHandle>();

            if (handle == null)
            {
                Debug.LogWarning($"[ObjectPool] {obj.name} has no PoolHandle. Destroy instead.");
                Object.Destroy(obj);
                return;
            }

            if (handle.IsInPool)
                return;
            
            poolable.OnDespawnedToPool();

            handle.MarkDespawned();

            obj.SetActive(false);
            obj.transform.SetParent(parent, false);

            inactiveObjects.Enqueue(obj);
        }

        public void Clear()
        {
            while (inactiveObjects.Count > 0)
            {
                GameObject obj = inactiveObjects.Dequeue();

                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
        }
    }
}