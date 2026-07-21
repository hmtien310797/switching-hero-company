using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public class WeaponSummonService
    {
        private readonly WeaponSummonConfigSO config;
        private readonly WeaponSummonSaveData saveData;
        private readonly IWeaponSummonCurrencyGateway currencyGateway;
        private readonly WeaponManager weaponManager;

        public WeaponSummonService(
            WeaponSummonConfigSO config,
            WeaponSummonSaveData saveData,
            IWeaponSummonCurrencyGateway currencyGateway,
            WeaponManager weaponManager)
        {
            this.config = config;
            this.saveData = saveData;
            this.currencyGateway = currencyGateway;
            this.weaponManager = weaponManager;
        }

        public WeaponSummonOptionEntry GetOption(string optionId)
        {
            return config != null ? config.GetOption(optionId) : null;
        }

        public bool CanSummon(
            WeaponSummonOptionEntry option,
            out WeaponSummonPaymentType paymentType,
            out int paidAmount)
        {
            paymentType = WeaponSummonPaymentType.Ticket;
            paidAmount = 0;

            if (option == null || !option.Enabled)
                return false;

            if (currencyGateway.CanSpendWeaponTicket(option.TicketCost))
            {
                paymentType = WeaponSummonPaymentType.Ticket;
                paidAmount = option.TicketCost;
                return true;
            }

            if (currencyGateway.CanSpendGem(option.GemCost))
            {
                paymentType = WeaponSummonPaymentType.Gem;
                paidAmount = option.GemCost;
                return true;
            }

            return false;
        }
        
        
        public List<int> GetClaimableRewardLevels()
        {
            var result = new List<int>();

            if (config == null || config.LevelRewards == null)
                return result;

            int currentLevel = GetCurrentSummonLevel();

            for (int i = 0; i < config.LevelRewards.Count; i++)
            {
                var reward = config.LevelRewards[i];
                if (reward == null)
                    continue;

                if (saveData.ClaimedRewardLevels.Contains(reward.SummonLevel))
                    continue;

                if (currentLevel > reward.SummonLevel)
                    result.Add(reward.SummonLevel);
            }

            return result;
        }

        // Authoritative: server tính summon_level (dựa trên total_roll + bảng Weapon_Levels
        // phía server) và trả về qua summon/weapon + summon/state — client chỉ lưu lại,
        // không tự suy ra từ config local (dễ lệch nếu WeaponSummonConfigSO không đồng bộ).
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
        
        // Reward source is each level's own ItemId/ItemQuantity (Weapon_Levels), not the older
        // LevelRewards (Weapon_Rewards) table — see HeroSummonAchievementRewardBuilder for the
        // same replacement relationship, matching the server.
        public SummonRewardPreviewData GetRewardPreviewData()
        {
            if (config == null || config.SummonLevels == null)
                return null;

            int currentLevel = GetCurrentSummonLevel();

            // Chỉ xét các level có ItemId/ItemQuantity hợp lệ — level chưa cấu hình
            // reward (ItemId=0) không nên hiện preview/claimable (khớp hành vi
            // HeroSummonService/SkillSummonService), nếu không LoadIconByItemId(0) sẽ
            // log lỗi "Item 0 not found".
            var sorted = config.SummonLevels
                .Where(x => x != null && x.ItemId > 0 && x.ItemQuantity > 0)
                .OrderBy(x => x.SummonLevel)
                .ToList();

            // Ưu tiên reward claimable chưa nhận
            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];

                bool isClaimed = saveData.ClaimedRewardLevels.Contains(entry.SummonLevel);
                bool isClaimable = currentLevel > entry.SummonLevel;

                if (isClaimable && !isClaimed)
                    return BuildRewardPreview(entry, true, false);
            }

            // Nếu không có claimable, show mốc hiện tại hoặc mốc kế tiếp
            var preview = sorted.FirstOrDefault(x => x.SummonLevel >= currentLevel)
                          ?? sorted.LastOrDefault();

            if (preview == null)
                return null;

            bool claimed = saveData.ClaimedRewardLevels.Contains(preview.SummonLevel);
            bool claimable = currentLevel > preview.SummonLevel && !claimed;

            return BuildRewardPreview(preview, claimable, claimed);
        }
        
        public bool ClaimReward(int rewardLevel, ISummonRewardReceiver receiver)
        {
            if (receiver == null)
                return false;

            if (saveData == null || config == null)
                return false;

            if (saveData.ClaimedRewardLevels.Contains(rewardLevel))
                return false;

            int currentLevel = GetCurrentSummonLevel();

            // Quan trọng: phải vượt qua level đó mới được claim
            if (currentLevel <= rewardLevel)
                return false;

            var rewardEntry = config.GetRewardEntry(rewardLevel);
            if (rewardEntry == null || rewardEntry.RewardItems == null || rewardEntry.RewardItems.Count == 0)
                return false;

            for (int i = 0; i < rewardEntry.RewardItems.Count; i++)
            {
                receiver.GrantReward(rewardEntry.RewardItems[i]);
            }

            saveData.ClaimedRewardLevels.Add(rewardLevel);
            return true;
        }

        private SummonRewardPreviewData BuildRewardPreview(
            WeaponSummonLevelEntry entry,
            bool isClaimable,
            bool isClaimed)
        {
            return new SummonRewardPreviewData
            {
                SummonLevel = entry.SummonLevel,
                Quantity = entry.ItemQuantity,
                ItemId = entry.ItemId,
                IsClaimable = isClaimable,
                IsClaimed = isClaimed
            };
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
    }
}