using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Addressable
{
    public static class HeroImageService
    {
        // Truyền key gốc. AddressableSpriteAtlasService sẽ tự thêm suffix.
        private const string HeroAtlasKey = "hero_sprite_atlas";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        private static SpriteAtlas heroAtlas;
        private static UniTaskCompletionSource<SpriteAtlas> loadingRequest;

        public static bool IsLoaded => heroAtlas != null;

        /// <summary>
        /// Load atlas hero một lần và giữ lại cho tới khi Release/Clear.
        /// Có chống trường hợp nhiều UI cùng gọi load đồng thời.
        /// </summary>
        public static async UniTask<bool> InitializeAsync()
        {
            if (heroAtlas != null)
                return true;

            if (loadingRequest != null)
            {
                SpriteAtlas pendingAtlas = await loadingRequest.Task;
                return pendingAtlas != null;
            }

            loadingRequest = new UniTaskCompletionSource<SpriteAtlas>();

            try
            {
                heroAtlas =
                    await AddressableSpriteAtlasService.AcquireAtlasAsync(
                        HeroAtlasKey
                    );

                loadingRequest.TrySetResult(heroAtlas);

                if (heroAtlas == null)
                {
                    Debug.LogError(
                        $"[HeroImageService] Failed to load hero atlas. " +
                        $"Key={HeroAtlasKey}"
                    );

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[HeroImageService] Exception while loading hero atlas. " +
                    $"Key={HeroAtlasKey}"
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

        /// <summary>
        /// Lấy hero icon bất đồng bộ.
        /// Nếu atlas chưa load thì service sẽ tự load.
        /// </summary>
        public static async UniTask<Sprite> GetHeroIconAsync(
            string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                Debug.LogError(
                    "[HeroImageService] Hero sprite name is null or empty."
                );

                return null;
            }

            if (SpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
                return cachedSprite;

            bool initialized = await InitializeAsync();

            if (!initialized)
                return null;

            return GetHeroIcon(spriteName);
        }

        /// <summary>
        /// Lấy icon đồng bộ khi atlas đã load.
        /// </summary>
        public static Sprite GetHeroIcon(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;

            if (SpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={spriteName}"
                );

                return null;
            }

            SpriteCache.Add(spriteName, sprite);

            return sprite;
        }

        /// <summary>
        /// Helper lấy icon trực tiếp từ HeroDataSO.
        /// Đổi IconSpriteName theo property thực tế trong project của bạn.
        /// </summary>
        public static UniTask<Sprite> GetHeroIconAsync(HeroDataSO heroData)
        {
            if (heroData == null)
            {
                Debug.LogError("[HeroImageService] HeroDataSO is null.");
                return UniTask.FromResult<Sprite>(null);
            }

            return GetHeroIconAsync(heroData.HeroIconKey);
        }

        /// <summary>
        /// Helper đồng bộ từ HeroDataSO.
        /// Chỉ dùng sau khi InitializeAsync thành công.
        /// </summary>
        public static Sprite GetHeroIcon(HeroDataSO heroData)
        {
            if (heroData == null)
                return null;

            return GetHeroIcon(heroData.HeroIconKey);
        }

        /// <summary>
        /// Release atlas hero và xóa toàn bộ Sprite clone đã cache.
        ///
        /// Chỉ gọi khi không còn Image nào sử dụng hero icon,
        /// ví dụ logout, về title hoặc unload toàn bộ Main Hub.
        /// </summary>
        public static void Release()
        {
            if (heroAtlas == null)
            {
                SpriteCache.Clear();
                return;
            }

            SpriteCache.Clear();

            AddressableSpriteAtlasService.ReleaseAtlas(
                HeroAtlasKey
            );

            heroAtlas = null;
        }
    }
}