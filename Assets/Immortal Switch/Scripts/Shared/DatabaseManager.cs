using System;
using System.Collections.Generic;
using System.Linq;
using Battle.Dungeon;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Shop.Views.UI;
using Immortal_Switch.Scripts.Tutorial.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager : Singleton<DatabaseManager>
    {
        [Header("Database config")]
        [field: SerializeField]
        public ItemTierDatabaseSO ItemTierDb { get; private set; }

        [SerializeField]
        private DynamicHeroesGlobalSpecificationsBadWordDatabase badwordDb;

        [SerializeField]
        private DynamicHeroesGlobalSpecificationsPlayerExpDatabase playerExpDb;

        [field: SerializeField]
        public ItemsDatabaseSO ItemDb { get; private set; }

        [SerializeField]
        private DungeonDatabaseSO dungeonDb;

        [field: SerializeField]
        public DungeonSystemDatabaseSO DungeonDb { get; private set; }

        [Header("Tutorial database")]
        [field: SerializeField]
        public TutorialDatabaseSO TutorialDb { get; private set; }

        [Header("Product database")]
        [SerializeField]
        private DynamicHeroesGlobalSpecificationsPackDiamondDatabase packDiamondDb;

        [SerializeField]
        private DynamicHeroesGlobalSpecificationsPackIapDatabase packSpecialDb;

        [SerializeField]
        private DynamicHeroesGlobalSpecificationsProductIdDatabase productDb;

        // Server-sourced reward preview cache — dungeon_key -> stage -> rewards. Populated by
        // EnsureDungeonStageTableAsync(dungeonId) from dungeon/stage_table. Replaces the old
        // DungeonRewardResolver (local FLAT/LINEAR/POWER/STEP formula recompute), which could
        // silently drift from the server's already-baked game_dungeon_config.js numbers.
        private readonly Dictionary<string, Dictionary<int, IReadOnlyList<ItemRewardData>>> dungeonStageRewardCache = new();

        protected override void OnSingletonAwake()
        {
            InitBadwords();
            base.OnSingletonAwake();
        }

        public override async UniTask InitializeAsync()
        {

            InitHeroData();
            InitBossData();
            InitSkillData();
            InitCreepData();
            //dungeonRewardResolver = new DungeonRewardResolver(dungeonDb);
            await ItemDb.InitializeAsync();
        }

#region Helper

        /// <summary>
        /// lay tier info tu itemkey
        /// </summary>
        /// <param name="type">loai currency</param>
        public ItemDisplayData GetDisplayDataByCurrency(CurrencyType type)
        {
            return CurrencyMapper.TryParse(type, out var currency) ? GetDisplayDataByItemKey(currency) : null;
        }

        /// <summary>
        /// lay tier info tu itemkey
        /// </summary>
        /// <param name="itemKey">itemkey</param>
        public ItemDisplayData GetDisplayDataByItemKey(string itemKey)
        {
            var item = ItemDb.FindItem(itemKey);

            if (item != null)
            {
                var tier = Enum.TryParse<EItemTier>(item.rarity, true, out var result) ? result : EItemTier.D;
                var tierInfo = ItemTierDb.Get(tier);
                var icon = ItemDb.LoadIcon(item.iconKey);

                return new ItemDisplayData
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
        public List<ItemRewardData> GetRewards(string rewards)
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
                return new List<ItemRewardData>();
            }

            var items = entries.Select(entry => GetDisplayData(entry.itemKey)).ToList();
            var results = new List<ItemRewardData>(entries.Count);

            for (var i = 0; i < entries.Count; i++)
            {
                var set = items[i];
                var entry = entries[i];

                results.Add(new ItemRewardData
                {
                    ItemIcon = set?.ItemIcon,
                    TierInfo = set?.TierInfo,
                    Quantity = entry.quantity,
                    ItemKey = entry.itemKey,
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
        /// Đọc từ dungeonStageRewardCache (server dungeon/stage_table) — gọi
        /// EnsureDungeonStageTableAsync(dungeonId) trước, nếu chưa fetch xong thì trả rỗng.
        /// dungeon key: treasure, relic, weapon (awakening/boss_dragon chưa wire — xem dungeon.js).
        /// </summary>
        public IReadOnlyList<ItemRewardData> GetDungeonRewards(int dungeonId, int stageIdx)
        {
            var dungeonKey = GetDungeonKey(dungeonId);
            if (dungeonKey != null &&
                dungeonStageRewardCache.TryGetValue(dungeonKey, out var stageMap) &&
                stageMap.TryGetValue(Mathf.Max(1, stageIdx), out var rewards))
            {
                return rewards;
            }

            return Array.Empty<ItemRewardData>().ToList();
        }

        /// <summary>
        /// Fetch bảng reward baked sẵn theo stage cho 1 dungeon từ dungeon/stage_table và cache lại
        /// (static config, không đổi theo user — cache cả session). Gọi trước khi hiển thị reward
        /// preview (GetDungeonRewards) — vd. khi chọn tab dungeon hoặc mở popup DungeonView.
        /// No-op nếu dungeon_key đã có trong cache.
        /// </summary>
        public async UniTask EnsureDungeonStageTableAsync(int dungeonId)
        {
            var dungeonKey = GetDungeonKey(dungeonId);
            if (dungeonKey == null || dungeonStageRewardCache.ContainsKey(dungeonKey))
                return;

            var response = await NakamaClient.Instance.GetDungeonStageTableAsync(dungeonKey);
            if (!response.Success || response.Stages == null)
            {
                Debug.LogWarning($"[DatabaseManager] dungeon/stage_table failed for {dungeonKey}: {response.Error}");
                return;
            }

            var stageMap = new Dictionary<int, IReadOnlyList<ItemRewardData>>();
            for (int i = 0; i < response.Stages.Count; i++)
            {
                var entry   = response.Stages[i];
                var rewards = new List<ItemRewardData>();

                if (entry.Rewards != null)
                {
                    foreach (var kv in entry.Rewards)
                    {
                        var reward = new ItemRewardData { ItemKey = kv.Key, Quantity = BigNumber.FromDouble(kv.Value) };
                        TrySetDisplayData(reward);
                        rewards.Add(reward);
                    }
                }

                stageMap[entry.Stage] = rewards;
            }

            dungeonStageRewardCache[dungeonKey] = stageMap;
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public string GetDungeonKey(int dungeonId)
        {
            return dungeonDb.TryGetDefinition(dungeonId, out var definition) ? definition.DungeonKey : null;
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public int GetDungeonMaxStage(int dungeonId)
        {
            return dungeonDb.GetDungeonMaxStage(dungeonId);
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public string GetDungeonTitle(int dungeonId)
        {
            return dungeonDb.GetDungeonDisplayName(dungeonId);
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public int GetDungeonTicketRequest(int dungeonId)
        {
            return dungeonDb.GetDungeonTicketRequest(dungeonId);
        }

        /// <summary>
        /// dungeon key: treasure, relic, awakening, weapon
        /// </summary>
        public IReadOnlyList<ItemDisplayData> GetDungeonRewards(int dungeonKey)
        {
            var rewardData = GetDungeonRewards(dungeonKey, 0);

            for (int i = 0; i < rewardData.Count; i++)
            {
                TrySetDisplayData(rewardData[i]);
            }

            return rewardData;
        }

        public ItemDisplayData GetDisplayData(int itemId)
        {
            var item = ItemDb.FindItem(itemId);

            if (item != null)
            {
                var itemIcon = ItemDb.LoadIcon(item.iconKey);

                if (Enum.TryParse<EItemTier>(item.rarity, true, out var result))
                {
                    var tierInfo = ItemTierDb.Get(result);

                    if (tierInfo != null)
                    {
                        return new ItemDisplayData
                        {
                            ItemIcon = itemIcon,
                            TierInfo = tierInfo,
                        };
                    }
                }
            }

            return null;
        }

        public ItemDisplayData GetDisplayData(string itemKey)
        {
            var item = ItemDb.FindItem(itemKey);

            if (item != null)
            {
                var itemIcon = ItemDb.LoadIcon(item.iconKey);

                if (Enum.TryParse<EItemTier>(item.rarity, true, out var result))
                {
                    var tierInfo = ItemTierDb.Get(result);

                    if (tierInfo != null)
                    {
                        return new ItemDisplayData
                        {
                            ItemIcon = itemIcon,
                            TierInfo = tierInfo,
                        };
                    }
                }
            }

            return null;
        }

        private bool TrySetDisplayData(ItemRewardData rewardData)
        {
            if (rewardData == null ||
                string.IsNullOrEmpty(rewardData.ItemKey))
                return false;

            var item = ItemDb.FindItem(rewardData.ItemKey);

            if (item == null)
                return false;

            if (!Enum.TryParse(item.rarity, true, out EItemTier itemTier))
                return false;

            var tierInfo = ItemTierDb.Get(itemTier);

            if (tierInfo == null)
                return false;

            rewardData.ItemIcon = ItemDb.LoadIcon(item.iconKey);
            rewardData.TierInfo = tierInfo;

            return true;
        }

#endregion

#region Player db

        /// <summary>
        /// Lấy level và tiến độ EXP hiện tại dựa trên tổng EXP của người chơi.
        /// </summary>
        /// <param name="totalExp">Tổng EXP hiện tại của người chơi.</param>
        /// <returns>Tuple chứa level, currentExp, targetExp và progress.</returns>
        public (int level, BigNumber currentExp, BigNumber targetExp, float progress) GetLevelByTotalExp(long totalExp)
        {
            var left = 0;
            var right = playerExpDb.rows.Count - 1;

            while (left <= right)
            {
                var mid = left + ((right - left) >> 1);

                if (BigNumber.TryParse(playerExpDb.rows[mid].expCumulative, out var result))
                {
                    if (result <= totalExp)
                    {
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }
            }

            var index = Math.Clamp(right, 0, playerExpDb.rows.Count - 1);
            var entry = playerExpDb.rows[index];
            BigNumber.TryParse(entry.expCumulative, out var expCumulative);

            var calculate = totalExp - expCumulative;
            var currentExp = calculate < 0 ? 0 : calculate;
            BigNumber.TryParse(entry.expRequired, out var targetExp);

            var progress = targetExp > 0
                ? Math.Clamp(BigNumberHelper.DivideToFloat(currentExp, targetExp), 0f, 1f)
                : 1f;

            return (entry.level, currentExp, targetExp, progress);
        }

#endregion

#region Shop db

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

#endregion
    }
}