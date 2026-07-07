using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Addressable
{
    public static class AddressableSpriteAtlasService
    {
        private sealed class AtlasCacheEntry
        {
            public SpriteAtlas Atlas;
            public int ReferenceCount;
        }

        private const string AtlasSuffixKey =
            "Assets/Immortal Switch/Addressable/UI/atlas/{0}";

        private static readonly Dictionary<string, AtlasCacheEntry> Cache = new();

        private static readonly Dictionary<string, UniTaskCompletionSource<SpriteAtlas>>
            LoadingRequests = new();

        public static async UniTask<SpriteAtlas> AcquireAtlasAsync(string atlasKey)
        {
            if (!TryGetAtlasAddress(atlasKey, out string atlasAddress))
                return null;

            if (Cache.TryGetValue(atlasAddress, out AtlasCacheEntry cachedEntry))
            {
                cachedEntry.ReferenceCount++;
                return cachedEntry.Atlas;
            }

            if (LoadingRequests.TryGetValue(
                    atlasAddress,
                    out UniTaskCompletionSource<SpriteAtlas> existingRequest))
            {
                SpriteAtlas loadingAtlas = await existingRequest.Task;

                if (loadingAtlas != null &&
                    Cache.TryGetValue(atlasAddress, out AtlasCacheEntry loadedEntry))
                {
                    loadedEntry.ReferenceCount++;
                }

                return loadingAtlas;
            }

            var request = new UniTaskCompletionSource<SpriteAtlas>();
            LoadingRequests.Add(atlasAddress, request);

            AsyncOperationHandle<SpriteAtlas> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAddress);

                await handle.ToUniTask();

                if (handle.Status != AsyncOperationStatus.Succeeded ||
                    handle.Result == null)
                {
                    Debug.LogError(
                        $"[AddressableSpriteAtlasService] " +
                        $"Failed to load atlas. Key={atlasKey}, Address={atlasAddress}"
                    );

                    if (handle.IsValid())
                        Addressables.Release(handle);

                    request.TrySetResult(null);
                    return null;
                }

                SpriteAtlas atlas = handle.Result;

                Cache.Add(
                    atlasAddress,
                    new AtlasCacheEntry
                    {
                        Atlas = atlas,
                        ReferenceCount = 1
                    }
                );

                request.TrySetResult(atlas);

                // Không release handle ở đây. Cache đang giữ reference của atlas.
                return atlas;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[AddressableSpriteAtlasService] " +
                    $"Exception while loading atlas. Key={atlasKey}, Address={atlasAddress}"
                );

                Debug.LogException(exception);

                if (handle.IsValid())
                    Addressables.Release(handle);

                request.TrySetException(exception);
                return null;
            }
            finally
            {
                LoadingRequests.Remove(atlasAddress);
            }
        }

        public static async UniTask<Sprite> AcquireSpriteAsync(
            string atlasKey,
            string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                Debug.LogError(
                    "[AddressableSpriteAtlasService] Sprite name is null or empty."
                );

                return null;
            }

            SpriteAtlas atlas = await AcquireAtlasAsync(atlasKey);

            if (atlas == null)
                return null;

            Sprite sprite = atlas.GetSprite(spriteName);

            if (sprite != null)
                return sprite;

            Debug.LogError(
                $"[AddressableSpriteAtlasService] " +
                $"Sprite not found. AtlasKey={atlasKey}, SpriteName={spriteName}"
            );

            ReleaseAtlas(atlasKey);
            return null;
        }

        public static Sprite GetSprite(
            string atlasKey,
            string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;

            if (!TryGetAtlasAddress(atlasKey, out string atlasAddress))
                return null;

            if (!Cache.TryGetValue(atlasAddress, out AtlasCacheEntry entry))
            {
                Debug.LogError(
                    $"[AddressableSpriteAtlasService] " +
                    $"Atlas is not loaded. Key={atlasKey}, Address={atlasAddress}"
                );

                return null;
            }

            Sprite sprite = entry.Atlas.GetSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[AddressableSpriteAtlasService] " +
                    $"Sprite not found. AtlasKey={atlasKey}, SpriteName={spriteName}"
                );
            }

            return sprite;
        }

        public static void ReleaseAtlas(string atlasKey)
        {
            if (!TryGetAtlasAddress(atlasKey, out string atlasAddress, logError: false))
                return;

            if (!Cache.TryGetValue(atlasAddress, out AtlasCacheEntry entry))
                return;

            entry.ReferenceCount--;

            if (entry.ReferenceCount > 0)
                return;

            Addressables.Release(entry.Atlas);
            Cache.Remove(atlasAddress);
        }

        public static bool IsLoaded(string atlasKey)
        {
            return TryGetAtlasAddress(atlasKey, out string atlasAddress, logError: false) &&
                   Cache.ContainsKey(atlasAddress);
        }

        public static int GetReferenceCount(string atlasKey)
        {
            if (!TryGetAtlasAddress(atlasKey, out string atlasAddress, logError: false))
                return 0;

            return Cache.TryGetValue(atlasAddress, out AtlasCacheEntry entry)
                ? entry.ReferenceCount
                : 0;
        }

        public static void ClearAll()
        {
            foreach (AtlasCacheEntry entry in Cache.Values)
            {
                if (entry.Atlas != null)
                    Addressables.Release(entry.Atlas);
            }

            Cache.Clear();
        }

        private static bool TryGetAtlasAddress(
            string atlasKey,
            out string atlasAddress,
            bool logError = true)
        {
            atlasAddress = null;

            if (string.IsNullOrWhiteSpace(atlasKey))
            {
                if (logError)
                {
                    Debug.LogError(
                        "[AddressableSpriteAtlasService] Atlas key is null or empty."
                    );
                }

                return false;
            }

            string trimmedKey = atlasKey.Trim();

            atlasAddress = trimmedKey.StartsWith("Assets/", StringComparison.Ordinal)
                ? trimmedKey
                : string.Format(AtlasSuffixKey, trimmedKey);

            return true;
        }
    }
}
