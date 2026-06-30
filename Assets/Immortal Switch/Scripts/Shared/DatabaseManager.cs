using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.ItemSystem.Models;
using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.Tutorial.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public class ItemSpriteSet
    {
        public Sprite ItemIcon;
        public ItemTierEntry TierInfo;
    }

    public class ItemRewardSet : ItemSpriteSet
    {
        public string ItemKey;
        public BigNumber Quantity;
    }

    public class DatabaseManager : Singleton<DatabaseManager>
    {
        [Header("Database config")]
        [field: SerializeField]
        public ItemTierDatabaseSO ItemTierDb { get; private set; }

        [SerializeField] private DynamicHeroesGlobalSpecificationsBadWordDatabase badwordDb;
        [field: SerializeField] public ItemSystemDatabaseSO ItemDb { get; private set; }

        [Header("Dungeon database")] [SerializeField]
        private DynamicHeroesGlobalSpecificationsDungeonRewardsConfigDatabase dungeonRewardsConfigDb;

        [SerializeField] private DynamicHeroesGlobalSpecificationsDungeonConfigDatabase dungeonConfigDb;
        [field: SerializeField] public DungeonSystemDatabaseSO DungeonDb { get; private set; }

        [Header("Tutorial database")]
        [field: SerializeField]
        public TutorialDatabaseSO TutorialDb { get; private set; }

        protected override void OnSingletonAwake()
        {
            InitBadwords();
            base.OnSingletonAwake();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

#region Helper

        /// <summary>
        /// lay tier info tu itemkey
        /// </summary>
        /// <param name="type">loai currency</param>
        public async UniTask<ItemSpriteSet> GetSpriteSetByCurrency(CurrencyType type)
        {
            return CurrencyMapper.TryParse(type, out var currency) ? await GetSpriteSetByItemKey(currency) : null;
        }

        /// <summary>
        /// lay tier info tu itemkey
        /// </summary>
        /// <param name="itemKey">itemkey</param>
        public async UniTask<ItemSpriteSet> GetSpriteSetByItemKey(string itemKey)
        {
            var item = ItemDb.FindItem(itemKey);

            if (item != null)
            {
                var tier = Enum.TryParse<EItemTier>(item.rarity, true, out var result) ? result : EItemTier.D;
                var tierInfo = ItemTierDb.Get(tier);
                var icon = await ItemDb.LoadIcon("icon_diamond");

                return new ItemSpriteSet
                {
                    ItemIcon = icon,
                    TierInfo = tierInfo,
                };
            }

            return null;
        }

        /// <summary>
        /// parse string rewards ra array
        /// </summary>
        public async UniTask<List<ItemRewardSet>> GetRewards(string rewards)
        {
            var entries = new List<(string itemKey, BigNumber quantity)>();
            var splitRewards = rewards.Split(';');

            foreach (var reward in splitRewards)
            {
                var splits = reward.Split(':');

                // có 2 key là item key va quantity
                if (splits.Length > 1)
                {
                    var itemKey = splits[0];
                    BigNumber.TryParse(splits[1], out var quantity);
                    entries.Add((itemKey, quantity));
                }
                else
                {
                    Debug.LogError($"Reward {reward} wrong config");
                }
            }

            if (entries.Count < 1)
            {
                Debug.LogError("Rewards not found");
                return new List<ItemRewardSet>();
            }

            var tasks = Enumerable.Select(entries, entry => GetItemSpriteSet(entry.itemKey)).ToList();
            var sets = await UniTask.WhenAll(tasks);
            var results = new List<ItemRewardSet>(entries.Count);

            for (var i = 0; i < entries.Count; i++)
            {
                results.Add(new ItemRewardSet
                {
                    ItemIcon = sets[i].ItemIcon,
                    TierInfo = sets[i].TierInfo,
                    Quantity = entries[i].quantity,
                    ItemKey = entries[i].itemKey,
                });
            }

            return results;
        }

#endregion

#region Badword db

        private void InitBadwords()
        {
            var badwords = badwordDb.rows.Select(v => v.vi).ToArray();
            IllegalWordDetection.Init(badwords);
        }

#endregion

#region Dungeon db

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public async UniTask<List<ItemRewardSet>> GetDungeonRewards(string dungeonKey, int stageIdx)
        {
            var results = new List<ItemRewardSet>();

            foreach (var entry in dungeonConfigDb.rows
                         .Where(entry => entry.dungeonId == dungeonKey && entry.stageIndex == stageIdx))
            {
                var sets = await UniTask.WhenAll(
                    GetItemSpriteSet(entry.reward1Key),
                    GetItemSpriteSet(entry.reward2Key),
                    GetItemSpriteSet(entry.reward3Key)
                );

                if (sets.Item1 != null)
                {
                    results.Add(new ItemRewardSet
                    {
                        ItemIcon = sets.Item1.ItemIcon,
                        TierInfo = sets.Item1.TierInfo,
                        Quantity = BigNumber.TryParse(entry.reward1Amount, out var result) ? result : 0,
                        ItemKey = entry.reward1Key,
                    });
                }

                if (sets.Item2 != null)
                {
                    results.Add(new ItemRewardSet
                    {
                        ItemIcon = sets.Item2.ItemIcon,
                        TierInfo = sets.Item2.TierInfo,
                        Quantity = BigNumber.TryParse(entry.reward2Amount, out var result) ? result : 0,
                        ItemKey = entry.reward2Key,
                    });
                }

                if (sets.Item3 != null)
                {
                    results.Add(new ItemRewardSet
                    {
                        ItemIcon = sets.Item3.ItemIcon,
                        TierInfo = sets.Item3.TierInfo,
                        Quantity = entry.reward3Amount,
                        ItemKey = entry.reward3Key,
                    });
                }

                break;
            }

            return results;
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public int GetDungeonRewardsMaxStage(string dungeonKey)
        {
            return dungeonRewardsConfigDb.rows
                       .LastOrDefault(entry => entry.dungeonKey == dungeonKey)
                       ?.stageCount ??
                   0;
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public string GetDungeonTitle(string dungeonKey)
        {
            return dungeonRewardsConfigDb.rows
                       .LastOrDefault(entry => entry.dungeonKey == dungeonKey)
                       ?.uiNameVi ??
                   string.Empty;
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public int GetDungeonTicketRequest(string dungeonKey)
        {
            return dungeonRewardsConfigDb.rows
                       .LastOrDefault(entry => entry.dungeonKey == dungeonKey)
                       ?.ticketRequest ??
                   1;
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public async UniTask<List<ItemSpriteSet>> GetDungeonRewards(string dungeonKey)
        {
            var results = new List<ItemSpriteSet>();

            foreach (var entry in dungeonRewardsConfigDb.rows
                         .Where(entry => entry.dungeonKey == dungeonKey))
            {
                var sets = await UniTask.WhenAll(
                    GetItemSpriteSet(entry.reward1Key),
                    GetItemSpriteSet(entry.reward2Key),
                    GetItemSpriteSet(entry.reward3Key)
                );

                if (sets.Item1 != null)
                {
                    results.Add(sets.Item1);
                }

                if (sets.Item2 != null)
                {
                    results.Add(sets.Item2);
                }

                if (sets.Item3 != null)
                {
                    results.Add(sets.Item3);
                }

                break;
            }

            return results;
        }

        private async UniTask<ItemSpriteSet> GetItemSpriteSet(string itemKey)
        {
            var item = ItemDb.FindItem(itemKey);

            if (item != null)
            {
                var itemIcon = await ItemDb.LoadIcon("icon_diamond");

                if (Enum.TryParse<EItemTier>(item.rarity, true, out var result))
                {
                    var tierInfo = ItemTierDb.Get(result);

                    if (tierInfo != null)
                    {
                        return new ItemSpriteSet
                        {
                            ItemIcon = itemIcon,
                            TierInfo = tierInfo,
                        };
                    }
                }
            }

            return null;
        }

#endregion
    }
}