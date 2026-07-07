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
        private readonly Dictionary<string, int> activeCounts = new();
        private readonly Dictionary<
                string,
                HashSet<AddressablePoolHandle>>
            activeHandles = new();

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

        public async UniTask<bool> CreatePoolAsync(
            string key,
            int warmupCount,
            Transform parent = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError(
                    "[AddressablePoolService] Cannot create pool: key is null or empty."
                );

                return false;
            }

            if (pools.ContainsKey(key))
                return true;

            AddressablePool pool = null;

            try
            {
                pool = new AddressablePool(key);

                pools.Add(key, pool);
                parents.Add(
                    key,
                    parent != null
                        ? parent
                        : defaultPoolParent
                );

                activeCounts.Add(key, 0);
                activeHandles.Add(
                    key,
                    new HashSet<AddressablePoolHandle>()
                );

                if (warmupCount > 0)
                {
                    await pool.WarmupAsync(warmupCount);
                }

                Debug.Log(
                    $"[AddressablePoolService] Pool ready. " +
                    $"Key={key}, WarmupCount={warmupCount}"
                );

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressablePoolService] Failed to create pool. Key={key}"
                );

                Debug.LogException(exception);

                if (pool != null)
                {
                    pool.Dispose();
                }

                pools.Remove(key);
                parents.Remove(key);
                activeCounts.Remove(key);
                activeHandles.Remove(key);

                return false;
            }
        }

        public int GetActiveCount(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return 0;

            return activeCounts.TryGetValue(key, out int count)
                ? count
                : 0;
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
            AddressablePoolHandle handle =
                SpawnWithHandle(
                    key,
                    position,
                    rotation,
                    parent
                );

            if (handle == null || handle.Instance == null)
                return null;

            T component = handle.Instance.GetComponent<T>();

            if (component != null)
                return component;

            Debug.LogError(
                $"[AddressablePoolService] Missing component " +
                $"{typeof(T).Name} on prefab root. " +
                $"Key={key}, Instance={handle.Instance.name}"
            );

            handle.Despawn();
            return null;
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

            PooledObject pooledObject; 
            
            try
            {
                pooledObject = pool.Use();
            }
            catch (Exception exception)
            {
                Debug.Log("Need More Pool");
                return null;
            }
            
            if (pooledObject == null || pooledObject.Instance == null)
            {
                Debug.LogError(
                    $"[AddressablePoolService] Pool returned an invalid object. Key={key}"
                );

                pooledObject?.Dispose();
                return null;
            }

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

            var handle = new AddressablePoolHandle(
                this,
                key,
                pooledObject
            );
            
            if (!activeHandles.TryGetValue(
                    key,
                    out HashSet<AddressablePoolHandle> handles))
            {
                handles = new HashSet<AddressablePoolHandle>();
                activeHandles[key] = handles;
            }

            handles.Add(handle);

            if (activeCounts.TryGetValue(key, out int activeCount))
            {
                activeCounts[key] = activeCount + 1;
            }
            else
            {
                activeCounts[key] = 1;
            }

            try
            {
                if (instance.TryGetComponent(out IAddressablePoolable poolable))
                {
                    poolable.OnSpawned(handle);
                }

                return handle;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressablePoolService] OnSpawned failed. " +
                    $"Key={key}, Instance={instance.name}"
                );

                Debug.LogException(exception);

                handle.Despawn();
                return null;
            }
        }

        internal void ReturnInternal(
            string key,
            PooledObject pooledObject,
            AddressablePoolHandle handle)
        {
            if (handle != null &&
                activeHandles.TryGetValue(
                    key,
                    out HashSet<AddressablePoolHandle> handles))
            {
                handles.Remove(handle);
            }

            if (pooledObject == null ||
                pooledObject.Instance == null)
            {
                DecreaseActiveCount(key);
                return;
            }

            GameObject instance = pooledObject.Instance;

            try
            {
                if (instance.TryGetComponent(
                        out IAddressablePoolable poolable))
                {
                    poolable.OnDespawned();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressablePoolService] OnDespawned failed. " +
                    $"Key={key}, Instance={instance.name}"
                );

                Debug.LogException(exception);
            }

            instance.SetActive(false);

            DecreaseActiveCount(key);

            if (pools.TryGetValue(
                    key,
                    out AddressablePool pool))
            {
                pool.Return(pooledObject);
            }
            else
            {
                /*
                 * Pool đã bị remove ngoài ý muốn.
                 * Không còn nơi để trả object nên dispose thẳng.
                 */
                pooledObject.Dispose();
            }
        }
        
        private void DecreaseActiveCount(string key)
        {
            if (!activeCounts.TryGetValue(
                    key,
                    out int activeCount))
            {
                return;
            }

            activeCounts[key] = Mathf.Max(
                0,
                activeCount - 1
            );
        }
        
        public int DespawnAllActive(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return 0;

            if (!activeHandles.TryGetValue(
                    key,
                    out HashSet<AddressablePoolHandle> handles))
            {
                return 0;
            }

            if (handles.Count == 0)
                return 0;

            /*
             * Bắt buộc tạo snapshot vì handle.Despawn()
             * sẽ xóa handle khỏi HashSet gốc.
             */
            AddressablePoolHandle[] snapshot =
                new AddressablePoolHandle[handles.Count];

            handles.CopyTo(snapshot);

            int despawnedCount = 0;

            for (int i = 0; i < snapshot.Length; i++)
            {
                AddressablePoolHandle handle =
                    snapshot[i];

                if (handle == null ||
                    handle.IsDisposed)
                {
                    continue;
                }

                try
                {
                    handle.Despawn();
                    despawnedCount++;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[AddressablePoolService] Failed to force despawn " +
                        $"an active instance. Key={key}"
                    );

                    Debug.LogException(exception);
                }
            }

            return despawnedCount;
        }
        
        public bool DespawnAndDisposePool(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!pools.TryGetValue(
                    key,
                    out AddressablePool pool))
            {
                /*
                 * Pool không tồn tại thì xem như đã cleanup.
                 */
                RemovePoolTracking(key);
                return true;
            }

            int despawnedCount =
                DespawnAllActive(key);

            int remainingActiveCount =
                GetActiveCount(key);

            if (remainingActiveCount > 0)
            {
                Debug.LogError(
                    $"[AddressablePoolService] Cannot dispose pool because " +
                    $"some active instances could not be despawned. " +
                    $"Key={key}, RemainingActive={remainingActiveCount}"
                );

                return false;
            }

            try
            {
                /*
                 * Lúc này toàn bộ active đã được Return() về pool.
                 * pool.Dispose() sẽ dispose toàn bộ inactive object,
                 * bao gồm các object vừa được thu hồi.
                 */
                pool.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressablePoolService] Failed to dispose pool. " +
                    $"Key={key}"
                );

                Debug.LogException(exception);
                return false;
            }

            RemovePoolTracking(key);

            Debug.Log(
                $"[AddressablePoolService] Despawned active instances " +
                $"and disposed pool. " +
                $"Key={key}, DespawnedActive={despawnedCount}"
            );

            return true;
        }
        
        private void RemovePoolTracking(string key)
        {
            pools.Remove(key);
            parents.Remove(key);
            activeCounts.Remove(key);
            activeHandles.Remove(key);
        }

        public bool DisposePool(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!pools.TryGetValue(
                    key,
                    out AddressablePool pool))
            {
                RemovePoolTracking(key);
                return true;
            }

            int activeCount =
                GetActiveCount(key);

            if (activeCount > 0)
            {
                Debug.LogWarning(
                    $"[AddressablePoolService] Cannot dispose pool " +
                    $"while instances are active. " +
                    $"Key={key}, ActiveCount={activeCount}. " +
                    $"Use {nameof(DespawnAndDisposePool)} if force cleanup is intended."
                );

                return false;
            }

            pool.Dispose();

            RemovePoolTracking(key);

            Debug.Log(
                $"[AddressablePoolService] Disposed pool. Key={key}"
            );

            return true;
        }

        public void DisposeAll()
        {
            if (pools.Count == 0)
            {
                ClearAllTracking();
                return;
            }

            string[] keys =
                new string[pools.Count];

            pools.Keys.CopyTo(keys, 0);

            int disposedPoolCount = 0;

            for (int i = 0; i < keys.Length; i++)
            {
                if (DespawnAndDisposePool(keys[i]))
                {
                    disposedPoolCount++;
                }
            }

            ClearAllTracking();

            initialized = false;

            Debug.Log(
                $"[AddressablePoolService] Disposed all pools. " +
                $"Count={disposedPoolCount}"
            );
        }
        
        private void ClearAllTracking()
        {
            pools.Clear();
            parents.Clear();
            activeCounts.Clear();
            activeHandles.Clear();
        }
        
        public bool HasPool(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && pools.ContainsKey(key);
        }

        private void OnDestroy()
        {
            DisposeAll();
        }
    }
}