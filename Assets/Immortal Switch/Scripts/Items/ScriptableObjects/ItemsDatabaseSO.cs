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
        private readonly Dictionary<int, DynamicHeroesGlobalSpecificationsItemConfigRow> _itemsCache = new();

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
                return LoadIcon(item.rarity, item.itemType, item.itemKey);
            }

            Debug.LogError($"Item {itemId} not found");
            return null;
        }

        public Sprite LoadIconByItemKey(string itemKey)
        {
            var item = FindItem(itemKey);

            if (item != null)
            {
                return LoadIcon(item.rarity, item.itemType, item.itemKey);
            }

            Debug.LogError($"Item {itemKey} not found");
            return null;
        }

        [CanBeNull]
        public Sprite LoadIcon(string rarity, string itemType, string itemKey)
        {
            var key = $"ic_{itemType}_{itemKey}_{rarity}".ToLower();

            if (_itemAtlas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ItemsDatabaseSO)} has not been initialized. Call {nameof(InitializeAsync)}() before loading icons.");
            }

            if (_spriteCache.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            sprite = _itemAtlas.GetSprite(key);

            if (sprite == null)
            {
                Debug.LogError($"Sprite '{key}' was not found in atlas '{SpriteAtlasConstants.CURRENCY}'.");
                return null;
            }

            _spriteCache.Add(key, sprite);
            return sprite;
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow FindItem(int itemId)
        {
            if (_itemsCache.TryGetValue(itemId, out var item))
            {
                return item;
            }

            item = ItemConfig.rows.FirstOrDefault(v => v.itemId == itemId);

            if (item != null)
            {
                _itemsCache.Add(itemId, item);
            }

            return item;
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow FindItem(string itemKey)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemKey == itemKey);
        }
        
        public EItemTier GetItemTier(int itemId)
        {
            string eItemTier = ItemConfig.rows.FirstOrDefault(v => v.itemId == itemId)?.rarity;
            if (string.IsNullOrEmpty(eItemTier))
            {
                Debug.LogError($"Item {itemId} not found, return tier D");
                return EItemTier.D;
            }

            if (Enum.TryParse(eItemTier, out EItemTier itemTier))
            {
                return itemTier;
            }
            
            Debug.LogError($"Item {itemId} not found, return tier D");
            return EItemTier.D;
        }
    }
}