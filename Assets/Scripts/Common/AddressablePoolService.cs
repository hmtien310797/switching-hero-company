using System;
using System.Collections.Generic;
using Addler.Runtime.Core.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pooling
{
    public class AddressablePoolService : MonoBehaviour
    {
        public static AddressablePoolService Instance { get; private set; }

        [SerializeField] private AddressablePoolConfigSO config;
        [SerializeField] private Transform defaultPoolParent;

        private readonly Dictionary<string, AddressablePool> pools = new();
        private readonly Dictionary<string, Transform> parents = new();

        private bool initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (defaultPoolParent == null)
                defaultPoolParent = transform;
        }

        public async UniTask InitializeAsync()
        {
            if (initialized)
                return;

            initialized = true;

            if (config == null || config.entries == null)
            {
                Debug.LogWarning("[AddressablePoolService] Missing pool config.");
                return;
            }

            for (int i = 0; i < config.entries.Length; i++)
            {
                AddressablePoolEntry entry = config.entries[i];

                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;

                await CreatePoolAsync(entry.key, entry.warmupCount, entry.parent);
            }
        }

        public async UniTask CreatePoolAsync(string key, int warmupCount, Transform parent = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[AddressablePoolService] Key is null or empty.");
                return;
            }

            if (pools.ContainsKey(key))
                return;

            var pool = new AddressablePool(key);

            pools.Add(key, pool);
            parents.Add(key, parent != null ? parent : defaultPoolParent);

            if (warmupCount > 0)
                await pool.WarmupAsync(warmupCount);

            Debug.Log($"[AddressablePoolService] Warmup pool: {key}, count: {warmupCount}");
        }

        public GameObject Spawn(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            AddressablePoolHandle handle = SpawnWithHandle(key, position, rotation, parent);
            return handle?.Instance;
        }

        public T Spawn<T>(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
            where T : Component
        {
            AddressablePoolHandle handle = SpawnWithHandle(key, position, rotation, parent);

            if (handle == null || handle.Instance == null)
                return null;

            return handle.Instance.GetComponent<T>();
        }

        public AddressablePoolHandle SpawnWithHandle(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            if (!pools.TryGetValue(key, out AddressablePool pool))
            {
                Debug.LogError($"[AddressablePoolService] Pool not found: {key}. Did you forget warmup?");
                return null;
            }

            PooledObject pooledObject = pool.Use();

            GameObject instance = pooledObject.Instance;
            Transform targetParent = parent != null
                ? parent
                : parents.TryGetValue(key, out Transform registeredParent)
                    ? registeredParent
                    : defaultPoolParent;

            Transform tr = instance.transform;
            tr.SetParent(targetParent);
            tr.SetPositionAndRotation(position, rotation);

            instance.SetActive(true);

            var handle = new AddressablePoolHandle(this, key, pooledObject);

            if (instance.TryGetComponent(out IAddressablePoolable poolable))
                poolable.OnSpawned(handle);

            return handle;
        }

        internal void ReturnInternal(string key, PooledObject pooledObject)
        {
            if (pooledObject == null || pooledObject.Instance == null)
                return;

            GameObject instance = pooledObject.Instance;

            if (instance.TryGetComponent(out IAddressablePoolable poolable))
                poolable.OnDespawned();

            instance.SetActive(false);

            if (pools.TryGetValue(key, out AddressablePool pool))
            {
                pool.Return(pooledObject);
            }
            else
            {
                pooledObject.Dispose();
            }
        }

        public void DisposePool(string key)
        {
            if (!pools.TryGetValue(key, out AddressablePool pool))
                return;

            pool.Dispose();

            pools.Remove(key);
            parents.Remove(key);

            Debug.Log($"[AddressablePoolService] Dispose pool: {key}");
        }

        public void DisposeAll()
        {
            foreach (var pair in pools)
            {
                pair.Value.Dispose();
            }

            pools.Clear();
            parents.Clear();
            initialized = false;

            Debug.Log("[AddressablePoolService] Dispose all pools.");
        }

        private void OnDestroy()
        {
            DisposeAll();
        }
    }
}