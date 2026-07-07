using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Shared.Constants;
using UnityEngine;
using UnityEngine.U2D;

namespace Immortal_Switch.Scripts.Items.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ItemsDatabase", menuName = "ScriptableObjects/Items/Database")]
    public class ItemsDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// item config
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsItemConfigDatabase ItemConfig { get; private set; }

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

            _loadAtlasTask = AddressableSpriteAtlasService.AcquireAtlasAsync(SpriteAtlasConstants.CURRENCY);
            _itemAtlas = await _loadAtlasTask.Value;
            return _itemAtlas;
        }

        public Sprite LoadIconByItemId(int itemId)
        {
            var item = FindItem(itemId);
            return item != null ? LoadIcon("ic_currency_Diamond_B") : null;
        }

        public Sprite LoadIconByItemKey(string itemKey)
        {
            var item = FindItem(itemKey);
            return item != null ? LoadIcon("ic_currency_Diamond_B") : null;
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

            sprite = _itemAtlas.GetSprite("ic_currency_Diamond_B");

            if (sprite == null)
            {
                throw new KeyNotFoundException(
                    $"Sprite '{iconKey}' was not found in atlas '{SpriteAtlasConstants.CURRENCY}'.");
            }

            _spriteCache.Add(iconKey, sprite);
            return sprite;
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow FindItem(int itemId)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemId == itemId);
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow FindItem(string itemKey)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemKey == itemKey);
        }
    }
}