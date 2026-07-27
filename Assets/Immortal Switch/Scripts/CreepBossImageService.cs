using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Boss;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts
{
    public static class CreepBossImageService
    {
        private const string CreepBossAtlasKey = "creep_boss_sprite_atlas";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        private static SpriteAtlas creepBossAtlas;
        private static UniTaskCompletionSource<SpriteAtlas> loadingRequest;

        public static bool IsLoaded => creepBossAtlas != null;

        /// <summary>
        /// Load atlas hero một lần và giữ lại cho tới khi Release/Clear.
        /// Có chống trường hợp nhiều UI cùng gọi load đồng thời.
        /// </summary>
        public static async UniTask<bool> InitializeAsync()
        {
            if (creepBossAtlas != null)
                return true;

            if (loadingRequest != null)
            {
                SpriteAtlas pendingAtlas = await loadingRequest.Task;
                return pendingAtlas != null;
            }

            loadingRequest = new UniTaskCompletionSource<SpriteAtlas>();

            try
            {
                creepBossAtlas =
                    await AddressableSpriteAtlasService.AcquireAtlasAsync(
                        CreepBossAtlasKey
                    );

                loadingRequest.TrySetResult(creepBossAtlas);

                if (creepBossAtlas == null)
                {
                    Debug.LogError(
                        $"[HeroImageService] Failed to load hero atlas. " +
                        $"Key={CreepBossAtlasKey}"
                    );

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[HeroImageService] Exception while loading hero atlas. " +
                    $"Key={CreepBossAtlasKey}"
                );

                Debug.LogException(exception);

                loadingRequest.TrySetException(exception);
                return false;
            }
            finally
            {
                loadingRequest = null;
            }
        }
        
        public static Sprite GetCreepIcon(CreepDataSo creepDataSo)
        {
            if (creepDataSo == null)
                return null;

            return GetCreepBossIcon(creepDataSo.IconKey);
        }
        
        public static Sprite GetBossIcon(BossDataSO bossDataSo)
        {
            if (bossDataSo == null)
                return null;

            return GetCreepBossIcon(bossDataSo.IconKey);
        }
        
        private static Sprite GetCreepBossIcon(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;
            
            if (SpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
                return cachedSprite;

            if (creepBossAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = creepBossAtlas.GetSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={CreepBossAtlasKey}, SpriteName={spriteName}"
                );

                return null;
            }

            SpriteCache.Add(spriteName, sprite);
            return sprite;
        }
        
        public static void Release()
        {
            if (creepBossAtlas == null)
            {
                SpriteCache.Clear();
                return;
            }

            SpriteCache.Clear();

            AddressableSpriteAtlasService.ReleaseAtlas(
                CreepBossAtlasKey
            );

            creepBossAtlas = null;
        }
        
    }
}