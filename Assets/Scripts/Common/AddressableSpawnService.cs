using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Common
{
    public static class AddressableSpawnService
    {
        #region Non-pooled GameObject

        /// <summary>
        /// Spawn một GameObject không sử dụng pool.
        /// Dùng cho Hero, Boss hoặc object có lifetime dài.
        /// Instance phải được release bằng ReleaseInstance().
        /// </summary>
        public static async UniTask<GameObject> SpawnAsync(
            string prefix,
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            string completeKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}/{key}";
            
            if (string.IsNullOrWhiteSpace(completeKey))
            {
                Debug.LogError(
                    "[AddressableSpawnService] Spawn failed: key is null or empty."
                );

                return null;
            }

            AsyncOperationHandle<GameObject> handle = default;

            try
            {
                handle = Addressables.InstantiateAsync(
                    completeKey,
                    position,
                    rotation,
                    parent
                );

                await handle.ToUniTask();

                if (handle.Status != AsyncOperationStatus.Succeeded ||
                    handle.Result == null)
                {
                    Debug.LogError(
                        $"[AddressableSpawnService] Instantiate failed. Key={completeKey}"
                    );

                    ReleaseFailedOperation(handle);
                    return null;
                }

                /*
                 * Không release handle ở đây.
                 *
                 * Instance đang được Addressables track.
                 * Khi không dùng nữa phải gọi:
                 *
                 * AddressableSpawnService.ReleaseInstance(instance);
                 */
                return handle.Result;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressableSpawnService] Exception while spawning. Key={completeKey}"
                );

                Debug.LogException(exception);

                ReleaseFailedOperation(handle);
                return null;
            }
        }

        /// <summary>
        /// Spawn GameObject và lấy component T trên root object.
        /// </summary>
        public static async UniTask<T> SpawnAsync<T>(
            string prefix,
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
            where T : Component
        {
            GameObject instance = await SpawnAsync(
                prefix, key,
                position,
                rotation,
                parent
            );

            if (instance == null)
                return null;

            if (instance.TryGetComponent(out T component))
                return component;

            Debug.LogError(
                $"[AddressableSpawnService] Component {typeof(T).Name} " +
                $"was not found on root object. " +
                $"Key={key}, Instance={instance.name}"
            );

            ReleaseInstance(instance);
            return null;
        }

        /// <summary>
        /// Spawn GameObject và tìm component T trong object hoặc children.
        /// Chỉ dùng khi component không nằm trên root.
        /// </summary>
        public static async UniTask<T> SpawnInChildrenAsync<T>(
            string prefix,
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool includeInactive = true)
            where T : Component
        {
            GameObject instance = await SpawnAsync(prefix,
                key,
                position,
                rotation,
                parent
            );

            if (instance == null)
                return null;

            T component = instance.GetComponentInChildren<T>(includeInactive);

            if (component != null)
                return component;

            Debug.LogError(
                $"[AddressableSpawnService] Component {typeof(T).Name} " +
                $"was not found in instance or children. " +
                $"Key={key}, Instance={instance.name}"
            );

            ReleaseInstance(instance);
            return null;
        }

        /// <summary>
        /// Release GameObject được tạo bởi Addressables.InstantiateAsync.
        /// </summary>
        public static bool ReleaseInstance(GameObject instance)
        {
            if (instance == null)
                return false;

            bool released = Addressables.ReleaseInstance(instance);

            if (!released)
            {
                Debug.LogError(
                    $"[AddressableSpawnService] ReleaseInstance failed. " +
                    $"Instance={instance.name}. " +
                    "The object might not have been created by " +
                    "Addressables.InstantiateAsync or is no longer tracked."
                );
            }

            return released;
        }

        /// <summary>
        /// Release component có GameObject được tạo bởi
        /// Addressables.InstantiateAsync.
        /// </summary>
        public static bool ReleaseInstance(Component component)
        {
            if (component == null)
                return false;

            return ReleaseInstance(component.gameObject);
        }

        #endregion

        #region Generic Asset Loading

        /// <summary>
        /// Load trực tiếp một Addressable asset.
        ///
        /// Asset trả về phải được giữ cho tới khi không còn sử dụng,
        /// sau đó release bằng ReleaseAsset(asset).
        /// </summary>
        public static async UniTask<T> LoadAssetAsync<T>(string key)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError(
                    $"[AddressableSpawnService] Cannot load " +
                    $"{typeof(T).Name}: key is null or empty."
                );

                return null;
            }

            AsyncOperationHandle<T> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<T>(key);

                await handle.ToUniTask();

                if (handle.Status != AsyncOperationStatus.Succeeded ||
                    handle.Result == null)
                {
                    Debug.LogError(
                        $"[AddressableSpawnService] Failed to load " +
                        $"{typeof(T).Name}. Key={key}"
                    );

                    ReleaseFailedOperation(handle);
                    return null;
                }

                /*
                 * Không release handle tại đây.
                 *
                 * Addressables giữ reference count của asset này.
                 * Nơi sử dụng phải gọi:
                 *
                 * AddressableSpawnService.ReleaseAsset(asset);
                 */
                return handle.Result;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressableSpawnService] Exception while loading " +
                    $"{typeof(T).Name}. Key={key}"
                );

                Debug.LogException(exception);

                ReleaseFailedOperation(handle);
                return null;
            }
        }

        /// <summary>
        /// Release asset được load bằng LoadAssetAsync.
        /// Không dùng hàm này cho GameObject được tạo bằng InstantiateAsync.
        /// </summary>
        public static void ReleaseAsset<T>(T asset)
            where T : UnityEngine.Object
        {
            if (asset == null)
                return;

            Addressables.Release(asset);
        }

        #endregion

        #region Sprite / Icon

        public static UniTask<Sprite> LoadSpriteAsync(string key)
        {
            return LoadAssetAsync<Sprite>(key);
        }

        public static UniTask<Sprite> LoadIconAsync(string key)
        {
            return LoadAssetAsync<Sprite>(key);
        }

        public static void ReleaseSprite(Sprite sprite)
        {
            ReleaseAsset(sprite);
        }

        public static void ReleaseIcon(Sprite icon)
        {
            ReleaseAsset(icon);
        }

        #endregion

        #region Audio

        public static UniTask<AudioClip> LoadAudioClipAsync(string key)
        {
            return LoadAssetAsync<AudioClip>(key);
        }

        public static void ReleaseAudioClip(AudioClip audioClip)
        {
            ReleaseAsset(audioClip);
        }

        #endregion

        #region TextAsset

        public static UniTask<TextAsset> LoadTextAssetAsync(string key)
        {
            return LoadAssetAsync<TextAsset>(key);
        }

        public static void ReleaseTextAsset(TextAsset textAsset)
        {
            ReleaseAsset(textAsset);
        }

        #endregion

        #region Material

        public static UniTask<Material> LoadMaterialAsync(string key)
        {
            return LoadAssetAsync<Material>(key);
        }

        public static void ReleaseMaterial(Material material)
        {
            ReleaseAsset(material);
        }

        #endregion

        #region AnimationClip

        public static UniTask<AnimationClip> LoadAnimationClipAsync(string key)
        {
            return LoadAssetAsync<AnimationClip>(key);
        }

        public static void ReleaseAnimationClip(AnimationClip animationClip)
        {
            ReleaseAsset(animationClip);
        }

        #endregion

        #region ScriptableObject

        public static UniTask<T> LoadScriptableObjectAsync<T>(string key)
            where T : ScriptableObject
        {
            return LoadAssetAsync<T>(key);
        }

        public static void ReleaseScriptableObject<T>(T scriptableObject)
            where T : ScriptableObject
        {
            ReleaseAsset(scriptableObject);
        }

        #endregion

        #region Internal

        private static void ReleaseFailedOperation<T>(
            AsyncOperationHandle<T> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        private static void ReleaseFailedOperation(
            AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        #endregion
    }
}