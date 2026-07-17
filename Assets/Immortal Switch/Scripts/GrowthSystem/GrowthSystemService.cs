using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthSystemService : IPowerUpSource
    {
        public string SourceId => StatSourceIds.GrowthSystem;

        private readonly List<GrowthTierSegment> segments = new();
        private readonly Dictionary<StatType, List<GrowthTierSegment>> segmentsByStat = new();
        private readonly Dictionary<int, GrowthDataSO> tierDataMap = new();

        private readonly GrowthSaveData saveData;
        private int maxTier;

        public int CurrentUnlockedTier => saveData.CurrentUnlockedTier;
        public int MaxTier => maxTier;

        public GrowthSystemService(GrowthDatabaseSO database, GrowthSaveData saveData)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));

            this.saveData = saveData;
            BuildSegments(database);
        }

        private void BuildSegments(GrowthDatabaseSO database)
        {
            segments.Clear();
            segmentsByStat.Clear();
            tierDataMap.Clear();
            maxTier = 0;

            if (database.Tiers == null || database.Tiers.Length == 0)
                return;

            var sortedTiers = new List<GrowthDataSO>(database.Tiers);
            sortedTiers.Sort((a, b) => a.Tier.CompareTo(b.Tier));


            var lastEndStackByStat = new Dictionary<StatType, int>();

            for (int i = 0; i < sortedTiers.Count; i++)
            {
                var tierData = sortedTiers[i];
                if (tierData == null) continue;

                tierDataMap[tierData.Tier] = tierData;
                if (tierData.Tier > maxTier)
                    maxTier = tierData.Tier;

                if (tierData.StatGrowths == null || tierData.StatGrowths.Length == 0)
                    continue;

                for (int j = 0; j < tierData.StatGrowths.Length; j++)
                {
                    var statGrowth = tierData.StatGrowths[j];

                    int previousEnd = lastEndStackByStat.TryGetValue(statGrowth.Stat, out var endStack)
                        ? endStack
                        : 0;

                    int segmentStartStackInclusive = previousEnd + 1;
                    int segmentEndStackInclusive = tierData.MaxStack;

                    if (segmentEndStackInclusive < segmentStartStackInclusive)
                        continue;

                    var segment = new GrowthTierSegment(
                        tierData.Tier,
                        statGrowth.Stat,
                        statGrowth.ValueType,
                        segmentStartStackInclusive,
                        segmentEndStackInclusive,
                        statGrowth.ValuePerLevel,
                        statGrowth.GoldCostPerLevel
                    );

                    segments.Add(segment);

                    if (!segmentsByStat.TryGetValue(statGrowth.Stat, out var list))
                    {
                        list = new List<GrowthTierSegment>();
                        segmentsByStat.Add(statGrowth.Stat, list);
                    }

                    list.Add(segment);

                    lastEndStackByStat[statGrowth.Stat] = segmentEndStackInclusive;
                }
            }

            foreach (var pair in segmentsByStat)
            {
                pair.Value.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            }
        }

        public bool HasTier(int tier)
        {
            return tierDataMap.ContainsKey(tier);
        }

        public GrowthDataSO GetTierData(int tier)
        {
            tierDataMap.TryGetValue(tier, out var data);
            return data;
        }

        public bool IsStatUnlocked(StatType stat)
        {
            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return false;

            for (int i = 0; i < statSegments.Count; i++)
            {
                if (statSegments[i].Tier <= saveData.CurrentUnlockedTier)
                    return true;
            }

            return false;
        }

        public List<StatType> GetAllUnlockedStats()
        {
            var result = new List<StatType>();

            foreach (var pair in segmentsByStat)
            {
                if (IsStatUnlocked(pair.Key))
                    result.Add(pair.Key);
            }

            return result;
        }

        public List<StatType> GetAllUnlockedStatsUpToCurrentTier()
        {
            var result = new List<StatType>();

            foreach (var pair in segmentsByStat)
            {
                var stat = pair.Key;
                var list = pair.Value;

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Tier <= CurrentUnlockedTier)
                    {
                        result.Add(stat);
                        break;
                    }
                }
            }

            return result;
        }

        public List<StatType> GetStatsUnlockedExactlyAtTier(int tier)
        {
            var result = new List<StatType>();

            foreach (var pair in segmentsByStat)
            {
                var stat = pair.Key;
                var list = pair.Value;

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Tier == tier)
                    {
                        result.Add(stat);
                        break;
                    }
                }
            }

            return result;
        }

        public int GetCurrentStack(StatType stat)
        {
            return saveData.GetStack(stat);
        }

        public int GetMaxAvailableStack(StatType stat)
        {
            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return 0;

            int maxStack = 0;

            for (int i = 0; i < statSegments.Count; i++)
            {
                var seg = statSegments[i];
                if (seg.Tier > saveData.CurrentUnlockedTier)
                    break;

                maxStack = seg.EndStackInclusive;
            }

            return maxStack;
        }

        public bool IsMaxed(StatType stat)
        {
            return GetCurrentStack(stat) >= GetMaxAvailableStack(stat);
        }

        public bool IsTierFullyMaxed(int tier)
        {
            var stats = GetStatsUnlockedExactlyAtTier(tier);
            if (stats == null || stats.Count == 0)
                return false;

            for (int i = 0; i < stats.Count; i++)
            {
                if (!IsMaxed(stats[i]))
                    return false;
            }

            return true;
        }

        // NEW
        public int GetTierTotalStatCount(int tier)
        {
            var stats = GetStatsUnlockedExactlyAtTier(tier);
            return stats?.Count ?? 0;
        }

        // NEW
        public int GetTierCompletedStatCount(int tier)
        {
            var stats = GetStatsUnlockedExactlyAtTier(tier);
            if (stats == null || stats.Count == 0)
                return 0;

            int completed = 0;
            for (int i = 0; i < stats.Count; i++)
            {
                if (IsMaxed(stats[i]))
                    completed++;
            }

            return completed;
        }

        // NEW
        public float GetTierCompletionPercent(int tier)
        {
            int total = GetTierTotalStatCount(tier);
            if (total <= 0)
                return 0f;

            int completed = GetTierCompletedStatCount(tier);
            return (float)completed / total;
        }

        public int GetRemainingUpgradableStacks(StatType stat)
        {
            int max = GetMaxAvailableStack(stat);
            int current = GetCurrentStack(stat);
            int remain = max - current;
            return remain < 0 ? 0 : remain;
        }

        public bool CanUpgrade(StatType stat, int amount, int currentGold)
        {
            if (amount <= 0) return false;
            if (!IsStatUnlocked(stat)) return false;
            if (GetRemainingUpgradableStacks(stat) <= 0) return false;

            int totalCost = GetUpgradeCost(stat, amount);
            return totalCost > 0 && currentGold >= totalCost;
        }

        public int GetUpgradeCost(StatType stat, int amount)
        {
            if (amount <= 0) return 0;
            if (!segmentsByStat.TryGetValue(stat, out var statSegments)) return 0;

            int currentStack = GetCurrentStack(stat);
            int remaining = GetRemainingUpgradableStacks(stat);
            int buyAmount = Math.Min(amount, remaining);

            if (buyAmount <= 0)
                return 0;

            int totalCost = 0;
            int stackCursor = currentStack;

            for (int i = 0; i < buyAmount; i++)
            {
                int nextStack = stackCursor + 1;
                var segment = FindSegmentForStack(statSegments, nextStack, saveData.CurrentUnlockedTier);
                if (segment == null)
                    break;

                totalCost += segment.GoldCostPerLevel;
                stackCursor++;
            }

            return totalCost;
        }

        public int GetAffordableUpgradeAmount(StatType stat, BigNumber desiredAmount, BigNumber currentGold)
        {
            if (desiredAmount <= 0 || currentGold <= 0)
                return 0;

            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return 0;

            int currentStack = GetCurrentStack(stat);
            int remaining = GetRemainingUpgradableStacks(stat);
            BigNumber buyLimit = BigNumber.Min(desiredAmount, remaining);

            int bought = 0;
            int stackCursor = currentStack;
            BigNumber goldLeft = currentGold;

            for (int i = 0; i < buyLimit; i++)
            {
                int nextStack = stackCursor + 1;
                var segment = FindSegmentForStack(statSegments, nextStack, saveData.CurrentUnlockedTier);
                if (segment == null)
                    break;

                // if (goldLeft < segment.GoldCostPerLevel)
                //     break;

                goldLeft -= segment.GoldCostPerLevel;
                stackCursor++;
                bought++;
            }

            return bought;
        }

        public int Upgrade(StatType stat, int desiredAmount)
        {
            BigNumber currentGold = CurrencyLedgerService.Instance.GetDisplayBalance(CurrencyType.gold);
            int buyAmount = GetAffordableUpgradeAmount(stat, desiredAmount, currentGold);
            if (buyAmount <= 0)
                return 0;

            int totalCost = GetUpgradeCost(stat, buyAmount);
            if (totalCost <= 0 || currentGold < totalCost)
                return 0;

            CurrencyLedgerService.Instance.TrySpend(CurrencyType.gold, totalCost, CurrencyTransactionReason.GrowthUpgrade);
            saveData.AddStack(stat, buyAmount);
            return buyAmount;
        }

        public float GetTotalGrowthValue(StatType stat)
        {
            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return 0f;

            int currentStack = GetCurrentStack(stat);
            if (currentStack <= 0)
                return 0f;

            float totalValue = 0f;

            for (int i = 0; i < statSegments.Count; i++)
            {
                var seg = statSegments[i];
                if (seg.Tier > saveData.CurrentUnlockedTier)
                    break;

                if (currentStack < seg.StartStackInclusive)
                    break;

                int contributedStacks = Math.Min(currentStack, seg.EndStackInclusive) - seg.StartStackInclusive + 1;
                if (contributedStacks <= 0)
                    continue;

                totalValue += contributedStacks * seg.RuntimeValuePerLevel;
            }

            return totalValue;
        }

        public float GetNextLevelValuePreview(StatType stat, int amount)
        {
            if (amount <= 0)
                return GetTotalGrowthValue(stat);

            int currentStack = GetCurrentStack(stat);
            int maxStack = GetMaxAvailableStack(stat);
            int targetStack = Math.Min(currentStack + amount, maxStack);

            return CalculateTotalGrowthValueAtStack(stat, targetStack);
        }

        public int GetCurrentTierOfStatProgress(StatType stat)
        {
            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return 0;

            int currentStack = GetCurrentStack(stat);
            if (currentStack <= 0)
            {
                for (int i = 0; i < statSegments.Count; i++)
                {
                    if (statSegments[i].Tier <= saveData.CurrentUnlockedTier)
                        return statSegments[i].Tier;
                }

                return 0;
            }

            for (int i = 0; i < statSegments.Count; i++)
            {
                var seg = statSegments[i];
                if (seg.Tier > saveData.CurrentUnlockedTier)
                    break;

                if (currentStack >= seg.StartStackInclusive && currentStack <= seg.EndStackInclusive)
                    return seg.Tier;
            }

            int lastTier = 0;
            for (int i = 0; i < statSegments.Count; i++)
            {
                if (statSegments[i].Tier > saveData.CurrentUnlockedTier)
                    break;

                lastTier = statSegments[i].Tier;
            }

            return lastTier;
        }

        public bool TryGetStatGrowthAtTier(int tier, StatType stat, out StatGrowthData growth)
        {
            growth = default;

            if (!tierDataMap.TryGetValue(tier, out var tierData) || tierData == null || tierData.StatGrowths == null)
                return false;

            for (int i = 0; i < tierData.StatGrowths.Length; i++)
            {
                if (tierData.StatGrowths[i].Stat == stat)
                {
                    growth = tierData.StatGrowths[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPreviousKnownStatGrowthBeforeTier(int tierExclusive, StatType stat, out int sourceTier,
            out StatGrowthData growth)
        {
            sourceTier = 0;
            growth = default;

            for (int tier = tierExclusive - 1; tier >= 1; tier--)
            {
                if (TryGetStatGrowthAtTier(tier, stat, out growth))
                {
                    sourceTier = tier;
                    return true;
                }
            }

            return false;
        }

        public GrowthValueType GetValueType(StatType stat)
        {
            if (!segmentsByStat.TryGetValue(stat, out var statSegments) || statSegments.Count == 0)
                return GrowthValueType.Flat;

            return statSegments[0].ValueType;
        }

        public bool IsPercentValue(StatType stat)
        {
            return GetValueType(stat) == GrowthValueType.Percent;
        }

        public float ConvertRawValueToRuntime(StatType stat, float rawValue)
        {
            return IsPercentValue(stat) ? rawValue / 100f : rawValue;
        }

        public string FormatForDisplay(StatType stat, float runtimeValue)
        {
            return IsPercentValue(stat)
                ? $"{runtimeValue * 100f:0.##}%"
                : $"{runtimeValue:0.##}";
        }

        public string FormatRawPerLevelForDisplay(StatType stat, float rawPerLevelValue)
        {
            return IsPercentValue(stat)
                ? $"{rawPerLevelValue:0.###}%"
                : $"{rawPerLevelValue:0.###}";
        }

        public void UnlockTier(int tier)
        {
            if (tier <= saveData.CurrentUnlockedTier)
                return;

            if (!HasTier(tier))
                return;

            saveData.CurrentUnlockedTier = tier;
        }

        public void CollectPowerUps(List<PowerUpModifierData> output)
        {
            if (output == null)
                return;

            foreach (var pair in segmentsByStat)
            {
                var stat = pair.Key;
                float totalValue = GetTotalGrowthValue(stat);

                if (Math.Abs(totalValue) <= 0.0001f)
                    continue;

                output.Add(CreatePowerUpModifier(stat, totalValue));
            }
        }

        private PowerUpModifierData CreatePowerUpModifier(StatType stat, float value)
        {
            bool isPercentValue = IsPercentValue(stat);

            switch (stat)
            {
                // nhóm stat scale theo base stat của player
                case StatType.Atk:
                case StatType.MaxHp:
                case StatType.Def:
                    return new PowerUpModifierData(
                        SourceId,
                        stat,
                        isPercentValue ? PowerUpValueKind.PercentOfBase : PowerUpValueKind.FlatAdd,
                        value
                    );

                // nhóm cộng trực tiếp
                case StatType.CritChance:
                case StatType.CritDamage:
                case StatType.Accuracy:
                case StatType.AttackSpeed:
                case StatType.AttackRange:
                case StatType.MoveSpeed:
                case StatType.DamageToNormalMonster:
                case StatType.DamageToHeroMonster:
                case StatType.DamageReduction:
                case StatType.ClassSkillDamage:
                case StatType.ExclusiveSkillDamage:
                case StatType.SwitchSkillDamage:
                case StatType.FlatAtkBonus:
                case StatType.AtkPercentBonus:
                default:
                    return new PowerUpModifierData(
                        SourceId,
                        stat,
                        PowerUpValueKind.FlatAdd,
                        value
                    );
            }
        }

        private float CalculateTotalGrowthValueAtStack(StatType stat, int targetStack)
        {
            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return 0f;

            if (targetStack <= 0)
                return 0f;

            float totalValue = 0f;

            for (int i = 0; i < statSegments.Count; i++)
            {
                var seg = statSegments[i];
                if (seg.Tier > saveData.CurrentUnlockedTier)
                    break;

                if (targetStack < seg.StartStackInclusive)
                    break;

                int contributedStacks = Math.Min(targetStack, seg.EndStackInclusive) - seg.StartStackInclusive + 1;
                if (contributedStacks <= 0)
                    continue;

                totalValue += contributedStacks * seg.RuntimeValuePerLevel;
            }

            return totalValue;
        }

        private GrowthTierSegment FindSegmentForStack(List<GrowthTierSegment> statSegments, int stack, int unlockedTier)
        {
            for (int i = 0; i < statSegments.Count; i++)
            {
                var seg = statSegments[i];

                if (seg.Tier > unlockedTier)
                    break;

                if (stack >= seg.StartStackInclusive && stack <= seg.EndStackInclusive)
                    return seg;
            }

            return null;
        }

        private StatModifier CreateGrowthModifier(StatType stat, float value)
        {
            switch (stat)
            {
                case StatType.Atk:
                case StatType.MaxHp:
                case StatType.DamageToNormalMonster:
                case StatType.DamageToHeroMonster:
                case StatType.ClassSkillDamage:
                case StatType.ExclusiveSkillDamage:
                case StatType.SwitchSkillDamage:
                case StatType.AtkPercentBonus:
                case StatType.Accuracy:
                case StatType.AttackSpeed:
                case StatType.AttackRange:
                case StatType.MoveSpeed:
                case StatType.CritChance:
                case StatType.CritDamage:
                case StatType.Def:
                case StatType.FlatAtkBonus:
                case StatType.DamageReduction:
                default:
                    return new StatModifier(stat, ModifierOp.Add, value);
            }
        }
    }

    public class GrowthTierSegment
    {
        public int Tier { get; }
        public StatType Stat { get; }
        public GrowthValueType ValueType { get; }
        public int StartStackInclusive { get; }
        public int EndStackInclusive { get; }
        public float RawValuePerLevel { get; }
        public float RuntimeValuePerLevel { get; }
        public int GoldCostPerLevel { get; }

        public GrowthTierSegment(
            int tier,
            StatType stat,
            GrowthValueType valueType,
            int startStackInclusive,
            int endStackInclusive,
            float rawValuePerLevel,
            int goldCostPerLevel)
        {
            Tier = tier;
            Stat = stat;
            ValueType = valueType;
            StartStackInclusive = startStackInclusive;
            EndStackInclusive = endStackInclusive;
            RawValuePerLevel = rawValuePerLevel;
            RuntimeValuePerLevel = valueType == GrowthValueType.Percent
                ? rawValuePerLevel / 100f
                : rawValuePerLevel;
            GoldCostPerLevel = goldCostPerLevel;
        }
    }
}