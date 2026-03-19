using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthSystemService
    {
        private const string GrowthSourceId = "GROWTH_SYSTEM";

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

            int previousTierMaxStack = 0;

            for (int i = 0; i < sortedTiers.Count; i++)
            {
                var tierData = sortedTiers[i];
                if (tierData == null) continue;

                tierDataMap[tierData.Tier] = tierData;
                if (tierData.Tier > maxTier)
                    maxTier = tierData.Tier;

                int segmentStartStackInclusive = previousTierMaxStack + 1;
                int segmentEndStackInclusive = tierData.MaxStack;

                if (tierData.StatGrowths == null || tierData.StatGrowths.Length == 0)
                {
                    previousTierMaxStack = tierData.MaxStack;
                    continue;
                }

                for (int j = 0; j < tierData.StatGrowths.Length; j++)
                {
                    var statGrowth = tierData.StatGrowths[j];

                    var segment = new GrowthTierSegment(
                        tierData.Tier,
                        statGrowth.Stat,
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
                }

                previousTierMaxStack = tierData.MaxStack;
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
        
        public GrowthDataSO GetTierData(int tier)
        {
            tierDataMap.TryGetValue(tier, out var data);
            return data;
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

        public int GetAffordableUpgradeAmount(StatType stat, int desiredAmount, int currentGold)
        {
            if (desiredAmount <= 0 || currentGold <= 0)
                return 0;

            if (!segmentsByStat.TryGetValue(stat, out var statSegments))
                return 0;

            int currentStack = GetCurrentStack(stat);
            int remaining = GetRemainingUpgradableStacks(stat);
            int buyLimit = Math.Min(desiredAmount, remaining);

            int bought = 0;
            int stackCursor = currentStack;
            int goldLeft = currentGold;

            for (int i = 0; i < buyLimit; i++)
            {
                int nextStack = stackCursor + 1;
                var segment = FindSegmentForStack(statSegments, nextStack, saveData.CurrentUnlockedTier);
                if (segment == null)
                    break;

                if (goldLeft < segment.GoldCostPerLevel)
                    break;

                goldLeft -= segment.GoldCostPerLevel;
                stackCursor++;
                bought++;
            }

            return bought;
        }

        public int Upgrade(StatType stat, int desiredAmount, ref int currentGold)
        {
            int buyAmount = GetAffordableUpgradeAmount(stat, desiredAmount, currentGold);
            if (buyAmount <= 0)
                return 0;

            int totalCost = GetUpgradeCost(stat, buyAmount);
            if (totalCost <= 0 || currentGold < totalCost)
                return 0;

            currentGold -= totalCost;
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

                totalValue += contributedStacks * seg.ValuePerLevel;
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

        public bool IsTierFullyMaxed(int tier)
        {
            var stats = GetStatsUnlockedExactlyAtTier(tier);
            if (stats.Count == 0)
                return false;

            for (int i = 0; i < stats.Count; i++)
            {
                if (!IsMaxed(stats[i]))
                    return false;
            }

            return true;
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

        public bool TryGetPreviousKnownStatGrowthBeforeTier(int tierExclusive, StatType stat, out int sourceTier, out StatGrowthData growth)
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

        public void UnlockTier(int tier)
        {
            if (tier <= saveData.CurrentUnlockedTier)
                return;

            if (!HasTier(tier))
                return;

            saveData.CurrentUnlockedTier = tier;
        }

        public void ApplyGrowthToStatModule(StatModule statModule)
        {
            if (statModule == null)
                return;

            statModule.RemoveModifiersBySource(GrowthSourceId);

            foreach (var pair in segmentsByStat)
            {
                var stat = pair.Key;
                float totalValue = GetTotalGrowthValue(stat);

                if (Math.Abs(totalValue) <= 0.0001f)
                    continue;

                var modifier = CreateGrowthModifier(stat, totalValue);
                modifier.SourceId = GrowthSourceId;
                statModule.AddModifier(modifier);
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

                totalValue += contributedStacks * seg.ValuePerLevel;
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
                    return new StatModifier(stat, ModifierOp.Multiply, value);

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
        public int StartStackInclusive { get; }
        public int EndStackInclusive { get; }
        public float ValuePerLevel { get; }
        public int GoldCostPerLevel { get; }

        public GrowthTierSegment(
            int tier,
            StatType stat,
            int startStackInclusive,
            int endStackInclusive,
            float valuePerLevel,
            int goldCostPerLevel)
        {
            Tier = tier;
            Stat = stat;
            StartStackInclusive = startStackInclusive;
            EndStackInclusive = endStackInclusive;
            ValuePerLevel = valuePerLevel;
            GoldCostPerLevel = goldCostPerLevel;
        }
    }
}