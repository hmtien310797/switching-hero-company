using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shop.Views.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsPackDiamondDatabase packDiamondDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsPackIapDatabase packSpecialDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsGameRechargeMilestoneDatabase packGloryPassDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsConfigShopDatabase configShopDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsProductIdDatabase productDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsPackMothlyTotalDatabase packMonthlyTotalDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsPackMonthlyDatabase packMonthlyDb;

        public List<DynamicHeroesGlobalSpecificationsConfigShopRow> GetAllTabs()
        {
            return configShopDb.rows;
        }

        /// <summary>Toàn bộ product khai báo trong bảng product_id — dùng để đăng ký catalog cho
        /// Unity IAP lúc khởi tạo (IAPManager).</summary>
        public List<DynamicHeroesGlobalSpecificationsProductIdRow> GetAllProducts()
        {
            return productDb.rows;
        }

        public List<ShopTopupRuntimeData> GetShopPacksTopup()
        {
            var list = new List<ShopTopupRuntimeData>();

            foreach (var row in packDiamondDb.rows)
            {
                var product = productDb.rows.FirstOrDefault(v => v.iD == row.productId);

                if (product != null)
                {
                    list.Add(new ShopTopupRuntimeData
                    {
                        Pack = row,
                        Product = product,
                    });
                }
            }

            return list;
        }

        public List<ShopSpecialRuntimeData> GetShopPacksSpecial()
        {
            var list = new List<ShopSpecialRuntimeData>();

            foreach (var row in packSpecialDb.rows)
            {
                var product = productDb.rows.FirstOrDefault(v => v.iD == row.productID);

                if (product != null)
                {
                    list.Add(new ShopSpecialRuntimeData
                    {
                        Pack = row,
                        Product = product,
                    });
                }
            }

            return list;
        }

        public List<DynamicHeroesGlobalSpecificationsGameRechargeMilestoneRow> GetShopPacksGloryPass()
        {
            return packGloryPassDb.rows;
        }

        public IReadOnlyList<ItemRewardData> GetShopGloryPassRewards(int packId)
        {
            var result = new List<ItemRewardData>();
            var packs = GetShopPacksGloryPass();
            var pack = packs.FirstOrDefault(v => v.iD == packId);

            if (pack != null)
            {
                var rewards = new List<(int itemId, int quantity)>
                {
                    (pack.itemId1, pack.quantity1),
                    (pack.itemId2, pack.quantity2),
                    (pack.itemId3, pack.quantity3),
                };

                result.AddRange(BuildItemRewards(rewards));
            }

            return result;
        }

        public IReadOnlyList<ItemRewardData> GetShopSpecialRewards(int packId)
        {
            var result = new List<ItemRewardData>();
            var packs = GetShopPacksSpecial();
            var pack = packs.FirstOrDefault(v => v.Pack.iD == packId);

            if (pack != null)
            {
                var rewards = new List<(int itemId, int quantity)>
                {
                    (pack.Pack.itemId1, pack.Pack.quantity1),
                    (pack.Pack.itemId2, pack.Pack.quantity2),
                    (pack.Pack.itemId3, pack.Pack.quantity3),
                };

                result.AddRange(BuildItemRewards(rewards));
            }

            return result;
        }

        public IReadOnlyList<ItemRewardData> GetPackMonthly(int packId, int day)
        {
            var result = new List<ItemRewardData>();
            var pack = packMonthlyDb.rows.FirstOrDefault(v => v.packId == packId && v.day == day);

            if (pack != null)
            {
                var rewards = new List<(int itemId, int quantity)>
                {
                    (pack.itemId1, pack.qty1),
                    (pack.itemId2, pack.qty2),
                    (pack.itemId3, pack.qty3),
                };

                result.AddRange(BuildItemRewards(rewards));
            }

            return result;
        }

        public IReadOnlyList<ItemRewardData> GetPackMonthlyTotal(int packId)
        {
            var result = new List<ItemRewardData>();
            var pack = packMonthlyTotalDb.rows.FirstOrDefault(v => v.packId == packId);

            if (pack != null)
            {
                var rewards = new List<(int itemId, int quantity)>
                {
                    (pack.itemId1, pack.qty1),
                    (pack.itemId2, pack.qty2),
                    (pack.itemId3, pack.qty3),
                    (pack.itemId4, pack.qty4),
                    (pack.itemId5, pack.qty5),
                    (pack.itemId6, pack.qty6),
                };

                result.AddRange(BuildItemRewards(rewards));
            }

            return result;
        }

        public List<ItemRewardData> BuildItemRewards(List<(int itemId, int quantity)> rewards)
        {
            var result = new List<ItemRewardData>();
            var filters = rewards.Where(v => v is { itemId: > 0, quantity: > 0 });

            foreach (var tuple in filters)
            {
                var item = ItemDb.FindItem(tuple.itemId);

                if (item != null)
                {
                    var itemIcon = ItemDb.LoadIcon(item.rarity, item.itemType, item.itemKey);

                    if (itemIcon != null)
                    {
                        if (TrySetTierInfo(item.rarity, out var tierInfo) &&
                            tierInfo != null)
                        {
                            result.Add(new ItemRewardData(item.itemKey, tuple.quantity, itemIcon, tierInfo));
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Item {tuple.itemId} not found");
                }
            }

            return result;
        }
    }
}