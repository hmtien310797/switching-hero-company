using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.UI
{
    public class ItemTierVisualImageService
    {
        // Truyền key gốc. AddressableSpriteAtlasService sẽ tự thêm suffix.
        private const string ItemTierVisualAtlasKey = "item_tier_visual_atlas";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        private static SpriteAtlas itemTierVisualAtlas;
        private static UniTaskCompletionSource<SpriteAtlas> loadingRequest;

        public static bool IsLoaded => itemTierVisualAtlas != null;
        
        public static async UniTask<bool> InitializeAsync()
        {
            if (itemTierVisualAtlas != null)
                return true;

            if (loadingRequest != null)
            {
                SpriteAtlas pendingAtlas = await loadingRequest.Task;
                return pendingAtlas != null;
            }

            loadingRequest = new UniTaskCompletionSource<SpriteAtlas>();

            try
            {
                itemTierVisualAtlas =
                    await AddressableSpriteAtlasService.AcquireAtlasAsync(
                        ItemTierVisualAtlasKey
                    );

                loadingRequest.TrySetResult(itemTierVisualAtlas);

                if (itemTierVisualAtlas == null)
                {
                    Debug.LogError(
                        $"[ItemTierVisual] Failed to load hero atlas. " +
                        $"Key={ItemTierVisualAtlasKey}"
                    );

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[HeroImageService] Exception while loading hero atlas. " +
                    $"Key={ItemTierVisualAtlasKey}"
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
        
        public static Sprite GetItemTierIcon(EItemTier itemTier)
        {
            return GetItemTierIcon(itemTier.ToString());
        }
        
        public static Sprite GetItemTierFrame(EItemTier itemTier)
        {
            return GetItemTierFrame(itemTier.ToString());
        }
        
        public static Sprite GetItemTierBg(EItemTier itemTier)
        {
            return GetItemTierBg(itemTier.ToString());
        }

        public static ItemTierEntry GetItemTierEntry(EItemTier itemTier)
        {
            return new ItemTierEntry
            {
                tier = itemTier,
                background = GetItemTierBg(itemTier),
                border = GetItemTierFrame(itemTier),
                tierIcon = GetItemTierIcon(itemTier)
            };
        }
        
        public static List<ItemTierEntry> GetItemTierEntries()
        {
            List<ItemTierEntry> itemTierEntries = new();
            EItemTier[] values = (EItemTier[])Enum.GetValues(typeof(EItemTier));

            for (int i = 0; i < values.Length; i++)
            {
                EItemTier tier = values[i];
                itemTierEntries.Add(new ItemTierEntry
                {
                    tier = tier,
                    background = GetItemTierBg(tier),
                    border = GetItemTierFrame(tier),
                    tierIcon = GetItemTierIcon(tier)
                });
            }

            return itemTierEntries;
        }
        
        public static ItemTierEntry GetItemTierEntry(WeaponTier itemTier)
        {
            EItemTier newTier = EItemTier.D;
            switch (itemTier)
            {
                case WeaponTier.A :
                    newTier = EItemTier.A;
                    break;
                case WeaponTier.C :
                    newTier = EItemTier.C;
                    break;
                case WeaponTier.B :
                    newTier = EItemTier.B;
                    break;
                case WeaponTier.S :
                    newTier = EItemTier.S;
                    break;
                case WeaponTier.SS :
                    newTier = EItemTier.SS;
                    break;
                case WeaponTier.D :
                    newTier = EItemTier.D;
                    break;
            }
            return new ItemTierEntry
            {
                tier = newTier,
                background = GetItemTierBg(newTier),
                border = GetItemTierFrame(newTier),
                tierIcon = GetItemTierIcon(newTier)
            };
        }
        
        public static Sprite GetItemTierIcon(WeaponTier itemTier)
        {
            return GetItemTierIcon(itemTier.ToString());
        }
        
        public static Sprite GetItemTierFrame(WeaponTier itemTier)
        {
            return GetItemTierFrame(itemTier.ToString());
        }
        
        public static Sprite GetItemTierBg(WeaponTier itemTier)
        {
            return GetItemTierBg(itemTier.ToString());
        }
        
        public static void Release()
        {
            if (itemTierVisualAtlas == null)
            {
                SpriteCache.Clear();
                return;
            }

            SpriteCache.Clear();

            AddressableSpriteAtlasService.ReleaseAtlas(
                ItemTierVisualAtlasKey
            );

            itemTierVisualAtlas = null;
        }
        
        private static Sprite GetItemTierIcon(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;

            string valueKey = $"tier_{tier}".ToLower();

            if (SpriteCache.TryGetValue(valueKey, out Sprite cachedSprite))
                return cachedSprite;

            if (itemTierVisualAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = itemTierVisualAtlas.GetSprite(valueKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={ItemTierVisualAtlasKey}, SpriteName={valueKey}"
                );

                return null;
            }

            SpriteCache.Add(valueKey, sprite);
            return sprite;
        }
        
        private static Sprite GetItemTierBg(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;

            string valueKey = $"bg_{tier}".ToLower();

            if (SpriteCache.TryGetValue(valueKey, out Sprite cachedSprite))
                return cachedSprite;

            if (itemTierVisualAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = itemTierVisualAtlas.GetSprite(valueKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={ItemTierVisualAtlasKey}, SpriteName={valueKey}"
                );

                return null;
            }

            SpriteCache.Add(valueKey, sprite);
            return sprite;
        }
        
        private static Sprite GetItemTierFrame(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;

            string valueKey = $"frame_{tier}".ToLower();

            if (SpriteCache.TryGetValue(valueKey, out Sprite cachedSprite))
                return cachedSprite;

            if (itemTierVisualAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = itemTierVisualAtlas.GetSprite(valueKey);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={ItemTierVisualAtlasKey}, SpriteName={valueKey}"
                );

                return null;
            }

            SpriteCache.Add(valueKey, sprite);
            return sprite;
        }
    }
}