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
        
        public static Sprite GetHeroClassIcon(HeroDataSO heroData)
        {
            if (heroData == null)
                return null;

            return GetHeroClass(heroData.HeroClass.ToString());
        }
        
        public static Sprite GetHeroIcon(HeroDataSO heroData)
        {
            if (heroData == null)
                return null;

            return GetHeroIcon(heroData.HeroIconKey);
        }

        public static Sprite GetHeroTierIcon(SummonRarity rarity)
        {
            string rarityKey = $"tier_{rarity.ToString().ToLower()}";
            
            if (SpriteCache.TryGetValue(rarityKey, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(rarityKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={rarityKey}"
                );

                return null;
            }

            SpriteCache.Add(rarityKey, sprite);
            return sprite;
        }
        
        public static Sprite GetHeroTierIcon(HeroProgressTier rarity)
        {
            string rarityKey = $"tier_{rarity.ToString().ToLower()}";
            
            if (SpriteCache.TryGetValue(rarityKey, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(rarityKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={rarityKey}"
                );

                return null;
            }

            SpriteCache.Add(rarityKey, sprite);
            return sprite;
        }
        
        public static Sprite GetHeroTierFrame(HeroProgressTier rarity)
        {
            string rarityKey = $"frame_{rarity.ToString().ToLower()}";
            
            if (SpriteCache.TryGetValue(rarityKey, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(rarityKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={rarityKey}"
                );

                return null;
            }

            SpriteCache.Add(rarityKey, sprite);
            return sprite;
        }
        
        public static Sprite GetHeroTierFrame(SummonRarity rarity)
        {
            string rarityKey = $"frame_{rarity.ToString().ToLower()}";
            
            if (SpriteCache.TryGetValue(rarityKey, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(rarityKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={rarityKey}"
                );

                return null;
            }

            SpriteCache.Add(rarityKey, sprite);
            return sprite;
        }
        
        public static Sprite GetHeroTierBackground(SummonRarity rarity)
        {
            string rarityKey = $"background_{rarity.ToString().ToLower()}";
            
            if (SpriteCache.TryGetValue(rarityKey, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(rarityKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={rarityKey}"
                );

                return null;
            }

            SpriteCache.Add(rarityKey, sprite);
            return sprite;
        }
        
        public static Sprite GetHeroTierBackground(HeroProgressTier rarity)
        {
            string rarityKey = $"background_{rarity.ToString().ToLower()}";
            
            if (SpriteCache.TryGetValue(rarityKey, out Sprite cachedSprite))
                return cachedSprite;

            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(rarityKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={rarityKey}"
                );

                return null;
            }

            SpriteCache.Add(rarityKey, sprite);
            return sprite;
        }
        
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
        
        private static Sprite GetHeroIcon(string spriteName)
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
        
        private static Sprite GetHeroClass(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;
            
            string classKey = $"icon_class_{spriteName}".ToLower();

            if (SpriteCache.TryGetValue(classKey, out Sprite cachedSprite))
                return cachedSprite;
            
            if (heroAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = heroAtlas.GetSprite(classKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={HeroAtlasKey}, SpriteName={classKey}"
                );

                return null;
            }

            SpriteCache.Add(classKey, sprite);
            return sprite;
        }
    }
}