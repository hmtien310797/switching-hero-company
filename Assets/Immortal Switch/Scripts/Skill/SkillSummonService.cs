using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.SkillSummon;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Newtonsoft.Json;
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

        // Authoritative: server tính summon_level (dựa trên total_roll + bảng Skill_Levels
        // phía server) và trả về qua summon/skill + summon/state — client chỉ lưu lại,
        // không tự suy ra từ config local (dễ lệch nếu SkillSummonConfigSO không đồng bộ).
        public int GetCurrentSummonLevel()
        {
            return Mathf.Max(1, saveData.SummonLevel);
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
            if (entry == null || entry.ItemId <= 0 || entry.ItemQuantity <= 0)
                return null;

            return new SummonRewardPreviewData
            {
                SummonLevel = entry.SummonLevel,
                ItemId = entry.ItemId,
                Quantity = entry.ItemQuantity,
                IsClaimable = IsRewardClaimable(entry.SummonLevel),
                IsClaimed = IsRewardClaimed(entry.SummonLevel)
            };
        }

        // Reward source is each level's own ItemId/ItemQuantity (Skill_Levels), not the older
        // LevelRewards (Skill_Rewards) table — see HeroSummonAchievementRewardBuilder for the
        // same replacement relationship, matching the server.
        private SkillSummonLevelEntry GetPreviewRewardEntry()
        {
            if (config == null || config.SummonLevels == null || config.SummonLevels.Count == 0)
                return null;

            var sorted = config.SummonLevels
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
    }
}