using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Immortal_Switch.Scripts.Pooling
{
    public static class AddressableSpawnService
    {
        public static async UniTask<GameObject> SpawnAsync(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[AddressableSpawnService] Address key is null or empty.");
                return null;
            }

            GameObject go = await Addressables
                .InstantiateAsync(key, position, rotation, parent)
                .ToUniTask();

            if (go == null)
            {
                Debug.LogError($"[AddressableSpawnService] Cannot spawn addressable. key={key}");
                return null;
            }

            return go;
        }

        public static async UniTask<T> SpawnAsync<T>(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
            where T : Component
        {
            GameObject go = await SpawnAsync(key, position, rotation, parent);

            if (go == null)
                return null;

            if (!go.TryGetComponent(out T component))
            {
                Debug.LogError($"[AddressableSpawnService] Missing component {typeof(T).Name}. key={key}, object={go.name}");
                Release(go);
                return null;
            }

            return component;
        }

        public static void Release(Component component)
        {
            if (component == null)
                return;

            Release(component.gameObject);
        }

        public static void Release(GameObject go)
        {
            if (go == null)
                return;

            bool released = Addressables.ReleaseInstance(go);

            if (!released)
            {
                Debug.LogWarning($"[AddressableSpawnService] ReleaseInstance failed, destroy fallback. object={go.name}");
                Object.Destroy(go);
            }
        }
    }
}