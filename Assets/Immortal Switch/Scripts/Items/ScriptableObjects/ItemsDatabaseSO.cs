using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Modules;
using JetBrains.Annotations;
using UnityEngine;

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

        private readonly Dictionary<int, DynamicHeroesGlobalSpecificationsItemConfigRow> _itemsCache = new();

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
            return ModuleManager.CurrencyAtlas.LoadSprite(key);
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
            var rarity = ItemConfig.rows.FirstOrDefault(v => v.itemId == itemId)?.rarity;

            if (string.IsNullOrEmpty(rarity))
            {
                Debug.LogError($"Item {itemId} not found, return tier D");
                return EItemTier.D;
            }

            if (Enum.TryParse(rarity, out EItemTier itemTier))
            {
                return itemTier;
            }

            Debug.LogError($"Item {itemId} not found, return tier D");
            return EItemTier.D;
        }
    }
}