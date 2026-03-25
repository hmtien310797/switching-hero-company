using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem
{
    public class HeroSummonService
    {
        private readonly HeroSummonConfigSO config;
        private readonly HeroSummonSaveData saveData;
        private readonly IHeroSummonCurrencyGateway currencyGateway;
        private readonly HeroProgressionManager heroProgressionManager;

        public HeroSummonService(
            HeroSummonConfigSO config,
            HeroSummonSaveData saveData,
            IHeroSummonCurrencyGateway currencyGateway,
            HeroProgressionManager heroProgressionManager)
        {
            this.config = config;
            this.saveData = saveData;
            this.currencyGateway = currencyGateway;
            this.heroProgressionManager = heroProgressionManager;
        }

        public HeroSummonOptionEntry GetOption(string optionId)
        {
            return config.GetOption(optionId);
        }

        public int GetCurrentSummonLevel()
        {
            int level = 1;

            for (int i = 0; i < config.SummonLevels.Count; i++)
            {
                var entry = config.SummonLevels[i];
                if (entry == null) continue;

                if (saveData.TotalRoll >= entry.TotalRollRequired && entry.SummonLevel > level)
                    level = entry.SummonLevel;
            }

            return level;
        }
        
        public List<int> GetClaimableRewardLevels()
        {
            var result = new List<int>();

            if (config == null)
                return result;

            int currentLevel = GetCurrentSummonLevel();

            for (int i = 0; i < config.LevelRewards.Count; i++)
            {
                var reward = config.LevelRewards[i];
                if (reward == null) continue;
                
                if (reward.SummonLevel > currentLevel)
                    continue;
                
                if (saveData.ClaimedRewardLevels.Contains(reward.SummonLevel))
                    continue;

                result.Add(reward.SummonLevel);
            }

            result.Sort();
            return result;
        }

        public bool CanSummon(HeroSummonOptionEntry option, out SummonPaymentType paymentType, out int paidAmount)
        {
            paymentType = SummonPaymentType.Ticket;
            paidAmount = 0;

            if (option == null || !option.Enabled)
                return false;

            if (currencyGateway.CanSpendHeroTicket(option.TicketCost))
            {
                paymentType = SummonPaymentType.Ticket;
                paidAmount = option.TicketCost;
                return true;
            }

            if (currencyGateway.CanSpendGem(option.GemCost))
            {
                paymentType = SummonPaymentType.Gem;
                paidAmount = option.GemCost;
                return true;
            }

            return false;
        }

        public HeroSummonResult ExecuteSummon(HeroSummonOptionEntry option, SummonPaymentType paymentType)
        {
            if (option == null)
            {
                Debug.LogError("HeroSummonService.ExecuteSummon: option is null");
                return null;
            }

            if (!CanSummon(option, out var actualPaymentType, out var paidAmount))
            {
                Debug.LogWarning("HeroSummonService.ExecuteSummon: not enough currency");
                return null;
            }

            if (actualPaymentType != paymentType)
            {
                Debug.LogWarning($"HeroSummonService.ExecuteSummon: requested {paymentType}, actual available is {actualPaymentType}");
                return null;
            }

            Spend(option, paymentType);

            var result = new HeroSummonResult
            {
                PaymentType = paymentType,
                PaidAmount = paidAmount,
                OldTotalRoll = saveData.TotalRoll,
                OldSummonLevel = GetCurrentSummonLevel()
            };

            for (int i = 0; i < option.RollCount; i++)
            {
                int currentLevel = GetCurrentSummonLevel();
                var levelEntry = config.GetLevelEntry(currentLevel);

                if (levelEntry == null)
                {
                    Debug.LogError($"Missing summon level config for level {currentLevel}");
                    break;
                }

                bool pityHit;
                var rolledRarity = RollRarity(levelEntry, out pityHit);
                var hero = RollHeroByRarity(rolledRarity);

                if (hero == null)
                {
                    Debug.LogError($"No hero in pool for rarity {rolledRarity}");
                    continue;
                }

                bool alreadyOwned = heroProgressionManager.Service.HasHero(hero.Id);

                if (alreadyOwned)
                    heroProgressionManager.AddShardToHero(hero, 1, true);
                else
                    heroProgressionManager.AcquireHeroIfNeeded(hero);

                UpdatePityAfterRoll(rolledRarity);
                saveData.TotalRoll++;

                result.Entries.Add(new HeroSummonResultEntry
                {
                    RollIndex = i + 1,
                    HeroAsset = hero,
                    HeroName = hero.Name,
                    IsNewHero = !alreadyOwned,
                    ShardGained = alreadyOwned ? 1 : 0,
                    IsPityHit = pityHit
                });
            }

            result.NewTotalRoll = saveData.TotalRoll;
            result.NewSummonLevel = GetCurrentSummonLevel();
            result.NewlyUnlockedRewardLevels = GetNewlyUnlockedRewardLevels(result.OldSummonLevel, result.NewSummonLevel);

            return result;
        }

        public bool ClaimReward(int summonLevel, IHeroSummonRewardReceiver rewardReceiver)
        {
            if (rewardReceiver == null) return false;
            if (saveData.ClaimedRewardLevels.Contains(summonLevel)) return false;
            if (summonLevel > GetCurrentSummonLevel()) return false;

            var rewardEntry = config.LevelRewards.Find(x => x != null && x.SummonLevel == summonLevel);
            if (rewardEntry == null) return false;

            for (int i = 0; i < rewardEntry.RewardItems.Count; i++)
            {
                rewardReceiver.GrantReward(rewardEntry.RewardItems[i]);
            }

            saveData.ClaimedRewardLevels.Add(summonLevel);
            return true;
        }

        private void Spend(HeroSummonOptionEntry option, SummonPaymentType paymentType)
        {
            if (paymentType == SummonPaymentType.Ticket)
                currencyGateway.SpendHeroTicket(option.TicketCost);
            else
                currencyGateway.SpendGem(option.GemCost);
        }

        private SummonRarity RollRarity(HeroSummonLevelEntry levelEntry, out bool pityHit)
        {
            pityHit = false;

            if (TryForceHardPity(out var hardPityRarity))
            {
                pityHit = true;
                return hardPityRarity;
            }

            var rates = BuildRateMap(levelEntry);
            ApplySoftPity(rates);

            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            foreach (var kv in rates)
            {
                cumulative += kv.Value;
                if (roll <= cumulative)
                    return kv.Key;
            }

            return SummonRarity.Common;
        }

        private Dictionary<SummonRarity, float> BuildRateMap(HeroSummonLevelEntry levelEntry)
        {
            return new Dictionary<SummonRarity, float>
            {
                { SummonRarity.Common, levelEntry.CommonRate },
                { SummonRarity.UnCommon, levelEntry.UnCommonRate },
                { SummonRarity.Rare, levelEntry.RareRate },
                { SummonRarity.Epic, levelEntry.EpicRate },
                { SummonRarity.Legendary, levelEntry.LegendaryRate },
                { SummonRarity.Mythic, levelEntry.MythicRate }
            };
        }

        private void ApplySoftPity(Dictionary<SummonRarity, float> rates)
        {
            if (!config.EnablePity) return;
            if (config.PityMode != HeroSummonPityMode.Soft && config.PityMode != HeroSummonPityMode.SoftHard) return;
            if (saveData.PityMissCounter < config.SoftStart) return;

            float bonus = (saveData.PityMissCounter - config.SoftStart + 1) * config.SoftBonusPercentPerMiss;
            rates[config.PityTargetRarity] += bonus;

            var order = new[]
            {
                SummonRarity.Common,
                SummonRarity.UnCommon,
                SummonRarity.Rare,
                SummonRarity.Epic,
                SummonRarity.Legendary,
                SummonRarity.Mythic
            };

            float remain = bonus;
            for (int i = 0; i < order.Length; i++)
            {
                var rarity = order[i];
                if (rarity == config.PityTargetRarity) continue;
                if (remain <= 0f) break;

                float reducible = Mathf.Min(rates[rarity], remain);
                rates[rarity] -= reducible;
                remain -= reducible;
            }
        }

        private bool TryForceHardPity(out SummonRarity rarity)
        {
            rarity = SummonRarity.Common;

            if (!config.EnablePity) return false;
            if (config.PityMode != HeroSummonPityMode.Hard && config.PityMode != HeroSummonPityMode.SoftHard) return false;
            if (saveData.PityMissCounter + 1 < config.HardPity) return false;

            rarity = config.PityTargetRarity;
            return true;
        }

        private void UpdatePityAfterRoll(SummonRarity rolledRarity)
        {
            if (rolledRarity >= config.PityTargetRarity)
                saveData.PityMissCounter = 0;
            else
                saveData.PityMissCounter++;
        }

        private HeroDataSO RollHeroByRarity(SummonRarity rarity)
        {
            var pool = config.HeroPool
                .Where(x => x != null && x.IsAvailableInSummon && x.SummonRarity == rarity)
                .ToList();

            if (pool.Count == 0)
                return null;

            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(1, pool[i].SummonWeight);
            }

            int roll = Random.Range(1, totalWeight + 1);
            int cumulative = 0;

            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += Mathf.Max(1, pool[i].SummonWeight);
                if (roll <= cumulative)
                    return pool[i];
            }

            return pool[0];
        }

        private List<int> GetNewlyUnlockedRewardLevels(int oldLevel, int newLevel)
        {
            var result = new List<int>();

            if (newLevel <= oldLevel)
                return result;

            for (int i = 0; i < config.LevelRewards.Count; i++)
            {
                var reward = config.LevelRewards[i];
                if (reward == null) continue;

                if (reward.SummonLevel > oldLevel && reward.SummonLevel <= newLevel)
                    result.Add(reward.SummonLevel);
            }

            result.Sort();
            return result;
        }
    }
}