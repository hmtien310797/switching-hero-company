using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Addressable
{
    public static class SkillImageService
    {
        // Truyền key gốc. AddressableSpriteAtlasService sẽ tự thêm suffix.
        private const string SkillAtlasKey = "skill_sprite_atlas";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        private static SpriteAtlas skillAtlas;
        private static UniTaskCompletionSource<SpriteAtlas> loadingRequest;

        public static bool IsLoaded => skillAtlas != null;

        /// <summary>
        /// Load atlas hero một lần và giữ lại cho tới khi Release/Clear.
        /// Có chống trường hợp nhiều UI cùng gọi load đồng thời.
        /// </summary>
        public static async UniTask<bool> InitializeAsync()
        {
            if (skillAtlas != null)
                return true;

            if (loadingRequest != null)
            {
                SpriteAtlas pendingAtlas = await loadingRequest.Task;
                return pendingAtlas != null;
            }

            loadingRequest = new UniTaskCompletionSource<SpriteAtlas>();

            try
            {
                skillAtlas =
                    await AddressableSpriteAtlasService.AcquireAtlasAsync(
                        SkillAtlasKey
                    );

                loadingRequest.TrySetResult(skillAtlas);

                if (skillAtlas == null)
                {
                    Debug.LogError(
                        $"[HeroImageService] Failed to load hero atlas. " +
                        $"Key={SkillAtlasKey}"
                    );

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[HeroImageService] Exception while loading hero atlas. " +
                    $"Key={SkillAtlasKey}"
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
        
        public static Sprite GetSkillIcon(SkillDataSO skillData)
        {
            if (skillData == null)
                return null;

            return GetSkillIcon(skillData.IconSkillKey);
        }
        
        public static void Release()
        {
            if (skillAtlas == null)
            {
                SpriteCache.Clear();
                return;
            }

            SpriteCache.Clear();

            AddressableSpriteAtlasService.ReleaseAtlas(
                SkillAtlasKey
            );

            skillAtlas = null;
        }
        
        private static Sprite GetSkillIcon(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;

            if (SpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
                return cachedSprite;

            if (skillAtlas == null)
            {
                Debug.LogError(
                    "[HeroImageService] Hero atlas has not been initialized. " +
                    "Call InitializeAsync() or GetHeroIconAsync() first."
                );

                return null;
            }

            Sprite sprite = skillAtlas.GetSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[HeroImageService] Hero sprite was not found. " +
                    $"Atlas={SkillAtlasKey}, SpriteName={spriteName}"
                );

                return null;
            }

            SpriteCache.Add(spriteName, sprite);
            return sprite;
        }
    }
}