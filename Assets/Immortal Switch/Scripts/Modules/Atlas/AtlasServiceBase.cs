using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Modules.Atlas
{
    /// <summary>
    /// Lớp cơ sở dùng chung cho các service tải và truy xuất SpriteAtlas.
    /// </summary>
    public abstract class AtlasServiceBase
    {
        private readonly Dictionary<string, Sprite> _spriteCache = new();

        private UniTaskCompletionSource<SpriteAtlas> _loadingRequest;
        private SpriteAtlas _atlas;

        protected AtlasServiceBase(string atlasKey)
        {
            if (string.IsNullOrWhiteSpace(atlasKey))
            {
                throw new ArgumentException("Atlas key cannot be null or empty.", nameof(atlasKey));
            }

            AtlasKey = atlasKey;
        }

        // --- Public Fields ---
        public string AtlasKey { get; }

        public bool IsLoaded => _atlas != null;

        /// <summary>
        /// Tải atlas một lần và dùng chung request nếu có nhiều nơi gọi đồng thời.
        /// </summary>
        public async UniTask<bool> InitializeAsync()
        {
            if (_atlas != null)
            {
                return true;
            }

            if (_loadingRequest != null)
            {
                return await _loadingRequest.Task != null;
            }

            _loadingRequest = new UniTaskCompletionSource<SpriteAtlas>();

            try
            {
                _atlas = await AddressableSpriteAtlasService.AcquireAtlasAsync(AtlasKey);
                _loadingRequest.TrySetResult(_atlas);

                if (_atlas == null)
                {
                    Debug.LogError($"[AtlasService] Failed to load atlas. Key={AtlasKey}");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AtlasService] Exception while loading atlas. Key={AtlasKey}");
                Debug.LogException(exception);
                _loadingRequest.TrySetResult(null);
                return false;
            }
            finally
            {
                _loadingRequest = null;
            }
        }

        /// <summary>
        /// Lấy sprite theo tên và cache kết quả để tái sử dụng.
        /// </summary>
        public Sprite LoadSprite(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return null;
            }

            if (_atlas == null)
            {
                Debug.LogError(
                    $"[AtlasService] Atlas '{AtlasKey}' has not been initialized. " +
                    $"Call {nameof(InitializeAsync)}() before loading sprites."
                );

                return null;
            }

            if (_spriteCache.TryGetValue(spriteName, out var cachedSprite))
            {
                return cachedSprite;
            }

            var sprite = _atlas.GetSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogError($"[AtlasService] Sprite '{spriteName}' was not found in atlas '{AtlasKey}'.");
                return null;
            }

            _spriteCache.Add(spriteName, sprite);
            return sprite;
        }

        /// <summary>
        /// Alias dùng cho các caller đang lấy icon từ atlas.
        /// </summary>
        public Sprite LoadIcon(string iconKey)
        {
            return LoadSprite(iconKey);
        }

        /// <summary>
        /// Xóa cache và giải phóng atlas đã acquire.
        /// </summary>
        public void Release()
        {
            _spriteCache.Clear();

            if (_atlas == null)
            {
                return;
            }

            AddressableSpriteAtlasService.ReleaseAtlas(AtlasKey);

            _atlas = null;
        }
    }
}