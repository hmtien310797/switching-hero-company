using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Shared.Constants;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Shop.Models
{
    public class ShopAtlasService
    {
        private SpriteAtlas _itemAtlas;
        private UniTask<SpriteAtlas>? _loadAtlasTask;
        private readonly Dictionary<string, Sprite> _spriteCache = new();

        public UniTask InitializeAsync()
        {
            return LoadItemAtlasAsync().AsUniTask();
        }

        private async UniTask<SpriteAtlas> LoadItemAtlasAsync()
        {
            if (_itemAtlas != null)
            {
                return _itemAtlas;
            }

            if (_loadAtlasTask.HasValue)
            {
                _itemAtlas = await _loadAtlasTask.Value;
                return _itemAtlas;
            }

            _loadAtlasTask = AddressableSpriteAtlasService.AcquireAtlasAsync(SpriteAtlasConstants.SHOP);
            _itemAtlas = await _loadAtlasTask.Value;
            return _itemAtlas;
        }

        public Sprite LoadIcon(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
            {
                throw new ArgumentException("Icon key cannot be null or empty.", nameof(iconKey));
            }

            if (_itemAtlas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ItemsDatabaseSO)} has not been initialized. Call {nameof(InitializeAsync)}() before loading icons.");
            }

            if (_spriteCache.TryGetValue(iconKey, out var sprite))
            {
                return sprite;
            }

            sprite = _itemAtlas.GetSprite(iconKey);

            if (sprite == null)
            {
                Debug.LogError($"Sprite '{iconKey}' was not found in atlas '{SpriteAtlasConstants.CURRENCY}'.");
                return null;
            }

            _spriteCache.Add(iconKey, sprite);
            return sprite;
        }
    }
}