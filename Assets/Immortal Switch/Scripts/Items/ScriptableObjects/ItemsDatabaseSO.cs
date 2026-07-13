using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Shared.Constants;
using JetBrains.Annotations;
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

            if (item != null)
            {
                return LoadIcon(item.iconKey, item.rarity, item.itemType, item.itemName);
            }

            Debug.LogError($"Item {itemId} not found");
            return null;
        }

        public Sprite LoadIconByItemKey(string itemKey)
        {
            var item = FindItem(itemKey);

            if (item != null)
            {
                return LoadIcon(item.iconKey, item.rarity, item.itemType, item.itemName);
            }

            Debug.LogError($"Item {itemKey} not found");
            return null;
        }

        [CanBeNull]
        public Sprite LoadIcon(string iconKey, string rarity, string itemType, string itemName)
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

            var key = $"ic_{itemType.ToLower()}_{itemName.Replace(" ", "_")}_{rarity}";
            sprite = _itemAtlas.GetSprite(key);

            if (sprite == null)
            {
                Debug.LogError($"Sprite '{iconKey}' was not found in atlas '{SpriteAtlasConstants.CURRENCY}'.");
                return null;
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