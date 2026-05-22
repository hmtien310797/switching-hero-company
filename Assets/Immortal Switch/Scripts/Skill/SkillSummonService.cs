using System.Collections.Generic;
using System.Linq;
using Battle;
using Immortal_Switch.Scripts.SkillSummon;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public class SkillSummonService
    {
        [ShowInInspector]
        private readonly SkillSummonConfigSO config;
        [ShowInInspector]
        private readonly SkillSummonSaveData saveData;
        private readonly ISkillSummonCurrencyGateway currencyGateway;
        private readonly SkillProgressionService progressionService;

        public SkillProgressionService ProgressionService => progressionService;

        public SkillSummonService(
            SkillSummonConfigSO config,
            SkillSummonSaveData saveData,
            ISkillSummonCurrencyGateway currencyGateway,
            SkillProgressionService progressionService)
        {
            this.config = config;
            this.saveData = saveData;
            this.currencyGateway = currencyGateway;
            this.progressionService = progressionService;
        }

        public SkillSummonOptionEntry GetOption(string optionId)
        {
            return config != null ? config.GetOption(optionId) : null;
        }

        public int GetCurrentSummonLevel()
        {
            if (config == null || config.SummonLevels == null || config.SummonLevels.Count == 0)
                return 1;

            int totalRoll = Mathf.Max(0, saveData.TotalRoll);
            int currentLevel = 1;
            int consumed = 0;

            var sorted = config.SummonLevels
                .Where(x => x != null)
                .OrderBy(x => x.SummonLevel)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];

                // chỉ xử lý cost của level hiện tại
                if (entry.SummonLevel != currentLevel)
                    continue;

                int need = Mathf.Max(0, entry.TotalRollRequired);

                if (need <= 0)
                    break;

                if (totalRoll >= consumed + need)
                {
                    consumed += need;
                    currentLevel++;
                }
                else
                {
                    break;
                }
            }

            return currentLevel;
        }

        public int GetCurrentLevelProgressRoll()
        {
            if (config == null || config.SummonLevels == null || config.SummonLevels.Count == 0)
                return 0;

            int totalRoll = Mathf.Max(0, saveData.TotalRoll);
            int currentLevel = GetCurrentSummonLevel();
            int consumed = 0;

            var sorted = config.SummonLevels
                .Where(x => x != null)
                .OrderBy(x => x.SummonLevel)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];

                if (entry.SummonLevel >= currentLevel)
                    break;

                consumed += Mathf.Max(0, entry.TotalRollRequired);
            }

            return Mathf.Max(0, totalRoll - consumed);
        }

        public int GetCurrentLevelRequiredRoll()
        {
            if (config == null)
                return 0;

            int currentLevel = GetCurrentSummonLevel();
            var currentEntry = config.GetExactLevelEntry(currentLevel);

            if (currentEntry == null)
                return 0;

            return Mathf.Max(0, currentEntry.TotalRollRequired);
        }

        public bool CanSummon(SkillSummonOptionEntry option, out SummonPaymentType paymentType, out int paidAmount)
        {
            paymentType = SummonPaymentType.Ticket;
            paidAmount = 0;

            if (option == null || !option.Enabled)
                return false;

            if (currencyGateway.CanSpendSkillTicket(option.TicketCost))
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

        public SkillSummonResult ExecuteSummon(SkillSummonOptionEntry option, SummonPaymentType paymentType)
        {
            if (option == null)
                return null;

            if (!CanSummon(option, out var actualPaymentType, out var paidAmount))
                return null;

            if (actualPaymentType != paymentType)
                return null;

            Spend(option, paymentType);

            var result = new SkillSummonResult
            {
                PaymentType = paymentType,
                PaidAmount = paidAmount,
                OldTotalRoll = saveData.TotalRoll,
                OldSummonLevel = GetCurrentSummonLevel()
            };

            for (int i = 0; i < option.RollCount; i++)
            {
                int currentLevel = GetCurrentSummonLevel();
                var levelEntry = config.GetExactLevelEntry(currentLevel);
                if (levelEntry == null)
                    break;

                var rolledGrade = RollGrade(levelEntry);
                var skill = RollSkillByGrade(rolledGrade);

                if (skill == null)
                {
                    Debug.LogWarning($"No skill found for grade {rolledGrade}");
                    continue;
                }

                bool alreadyOwned = progressionService.HasSkill(skill.SkillId);
                progressionService.AcquireOrAddDuplicate(skill, 1);

                saveData.TotalRoll++;

                result.Entries.Add(new SkillSummonResultEntry
                {
                    RollIndex = i + 1,
                    SkillAsset = skill,
                    SkillId = skill.SkillId,
                    SkillName = string.IsNullOrEmpty(skill.SkillName) ? skill.name : skill.SkillName,
                    Grade = rolledGrade,
                    IsNewSkill = !alreadyOwned,
                    ShardGained = alreadyOwned ? 1 : 0
                });
            }

            result.NewTotalRoll = saveData.TotalRoll;
            result.NewSummonLevel = GetCurrentSummonLevel();
            result.NewlyUnlockedRewardLevels = GetNewlyUnlockedRewardLevels(result.OldSummonLevel, result.NewSummonLevel);

            return result;
        }

        public bool ClaimReward(int summonLevel, ISummonRewardReceiver rewardReceiver)
        {
            if (rewardReceiver == null)
                return false;

            if (saveData.ClaimedRewardLevels.Contains(summonLevel))
                return false;

            if (GetCurrentSummonLevel() <= summonLevel)
                return false;

            var rewardEntry = config.GetRewardEntry(summonLevel);
            if (rewardEntry == null)
                return false;

            for (int i = 0; i < rewardEntry.RewardItems.Count; i++)
                rewardReceiver.GrantReward(rewardEntry.RewardItems[i]);

            saveData.ClaimedRewardLevels.Add(summonLevel);
            saveData.ClaimedRewardLevels.Sort();
            return true;
        }

        public List<int> GetClaimableRewardLevels()
        {
            var result = new List<int>();
            int currentLevel = GetCurrentSummonLevel();

            if (config == null || config.LevelRewards == null)
                return result;

            foreach (var reward in config.LevelRewards)
            {
                if (reward == null)
                    continue;

                if (saveData.ClaimedRewardLevels.Contains(reward.SummonLevel))
                    continue;

                if (currentLevel > reward.SummonLevel)
                    result.Add(reward.SummonLevel);
            }

            result.Sort();
            return result;
        }

        public bool IsRewardClaimable(int summonLevel)
        {
            if (saveData.ClaimedRewardLevels.Contains(summonLevel))
                return false;

            return GetCurrentSummonLevel() > summonLevel;
        }

        public bool IsRewardClaimed(int summonLevel)
        {
            return saveData.ClaimedRewardLevels.Contains(summonLevel);
        }

        public SummonRewardPreviewData GetRewardPreviewData()
        {
            var entry = GetPreviewRewardEntry();
            if (entry == null || entry.RewardItems == null || entry.RewardItems.Count == 0)
                return null;

            return new SummonRewardPreviewData
            {
                SummonLevel = entry.SummonLevel,
                RewardItem = entry.RewardItems[0],
                IsClaimable = IsRewardClaimable(entry.SummonLevel),
                IsClaimed = IsRewardClaimed(entry.SummonLevel)
            };
        }

        public SummonLevelRewardEntry GetPreviewRewardEntry()
        {
            if (config == null || config.LevelRewards == null || config.LevelRewards.Count == 0)
                return null;

            var sorted = config.LevelRewards
                .Where(x => x != null)
                .OrderBy(x => x.SummonLevel)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                if (!saveData.ClaimedRewardLevels.Contains(sorted[i].SummonLevel))
                    return sorted[i];
            }

            return sorted.LastOrDefault();
        }

        public SkillDataSO GetRandomSkillByGrade(SkillSummonGrade grade)
        {
            return RollSkillByGrade(grade);
        }

        private void Spend(SkillSummonOptionEntry option, SummonPaymentType paymentType)
        {
            if (paymentType == SummonPaymentType.Ticket)
                currencyGateway.SpendSkillTicket(option.TicketCost);
            else
                currencyGateway.SpendGem(option.GemCost);
        }

        private SkillSummonGrade RollGrade(SkillSummonLevelEntry levelEntry)
        {
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            cumulative += levelEntry.GradeBRate;
            if (roll <= cumulative) return SkillSummonGrade.B;

            cumulative += levelEntry.GradeARate;
            if (roll <= cumulative) return SkillSummonGrade.A;

            cumulative += levelEntry.GradeSRate;
            if (roll <= cumulative) return SkillSummonGrade.S;

            return SkillSummonGrade.SS;
        }

        private SkillDataSO RollSkillByGrade(SkillSummonGrade grade)
        {
            if (config == null || config.SkillPool == null)
                return null;

            var pool = config.SkillPool
                .Where(x => x != null && ConvertTier(x.SkillTier) == grade)
                .ToList();

            if (pool.Count == 0)
                return null;

            int index = Random.Range(0, pool.Count);
            return pool[index];
        }

        private SkillSummonGrade ConvertTier(TierSkill tier)
        {
            switch (tier)
            {
                case TierSkill.B: return SkillSummonGrade.B;
                case TierSkill.A: return SkillSummonGrade.A;
                case TierSkill.S: return SkillSummonGrade.S;
                case TierSkill.SS: return SkillSummonGrade.SS;
                default: return SkillSummonGrade.B;
            }
        }

        private List<int> GetNewlyUnlockedRewardLevels(int oldLevel, int newLevel)
        {
            var result = new List<int>();

            if (newLevel <= oldLevel || config == null || config.LevelRewards == null)
                return result;

            for (int rewardLevel = oldLevel; rewardLevel < newLevel; rewardLevel++)
            {
                bool exists = config.LevelRewards.Any(x => x != null && x.SummonLevel == rewardLevel);
                if (exists)
                    result.Add(rewardLevel);
            }

            return result;
        }
    }
}