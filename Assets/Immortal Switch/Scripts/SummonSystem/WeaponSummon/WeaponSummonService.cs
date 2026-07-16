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

        public WeaponSummonResult ExecuteSummon(
            WeaponSummonOptionEntry option,
            WeaponSummonPaymentType paymentType)
        {
            if (option == null)
                return null;

            if (!CanSummon(option, out var actualPaymentType, out var paidAmount))
                return null;

            if (actualPaymentType != paymentType)
                return null;

            Spend(option, paymentType);

            var result = new WeaponSummonResult
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

                var tier = RollTier(levelEntry);
                int star = RollStar(levelEntry);

                saveData.TotalRoll++;

                var weapon = RollWeapon(tier, star);
                if (weapon == null)
                {
                    Debug.LogWarning($"[WeaponSummon] No weapon found for tier={tier}, star={star}");
                    continue;
                }

                bool success = weaponManager.AddStandardShardFromSummon(
                    weapon.WeaponId,
                    1,
                    out bool isNewWeapon,
                    out int totalShardAfter);

                if (!success)
                    continue;

                result.Entries.Add(new WeaponSummonResultEntry
                {
                    RollIndex = i + 1,
                    Weapon = weapon,
                    WeaponId = weapon.WeaponId,
                    WeaponName = string.IsNullOrEmpty(weapon.WeaponName) ? weapon.name : weapon.WeaponName,
                    Icon = weapon.Icon,
                    Tier = weapon.Tier,
                    Star = weapon.Star,
                    IsNewWeapon = isNewWeapon,
                    ShardGained = 1,
                    TotalShardAfter = totalShardAfter
                });
            }

            result.NewTotalRoll = saveData.TotalRoll;
            result.NewSummonLevel = GetCurrentSummonLevel();
            result.NewlyUnlockedRewardLevels =
                GetNewlyUnlockedRewardLevels(result.OldSummonLevel, result.NewSummonLevel);

            return result;
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
        
        public SummonRewardPreviewData GetRewardPreviewData()
        {
            if (config == null || config.LevelRewards == null)
                return null;

            int currentLevel = GetCurrentSummonLevel();

            var sorted = config.LevelRewards
                .Where(x => x != null)
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
            WeaponSummonLevelRewardEntry entry,
            bool isClaimable,
            bool isClaimed)
        {
            return new SummonRewardPreviewData
            {
                SummonLevel = entry.SummonLevel,
                Quantity = entry.RewardItems[0].Amount,
                ItemId = entry.RewardItems[0].ItemId,
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

        private void Spend(WeaponSummonOptionEntry option, WeaponSummonPaymentType paymentType)
        {
            if (paymentType == WeaponSummonPaymentType.Ticket)
                currencyGateway.SpendWeaponTicket(option.TicketCost);
            else
                currencyGateway.SpendGem(option.GemCost);
        }

        private WeaponTier RollTier(WeaponSummonLevelEntry entry)
        {
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            cumulative += entry.GradeDRate;
            if (roll <= cumulative) return WeaponTier.D;

            cumulative += entry.GradeCRate;
            if (roll <= cumulative) return WeaponTier.C;

            cumulative += entry.GradeBRate;
            if (roll <= cumulative) return WeaponTier.B;

            cumulative += entry.GradeARate;
            if (roll <= cumulative) return WeaponTier.A;

            cumulative += entry.GradeSRate;
            if (roll <= cumulative) return WeaponTier.S;

            return WeaponTier.SS;
        }

        private int RollStar(WeaponSummonLevelEntry entry)
        {
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            cumulative += entry.Star1Rate;
            if (roll <= cumulative) return 1;

            cumulative += entry.Star2Rate;
            if (roll <= cumulative) return 2;

            cumulative += entry.Star3Rate;
            if (roll <= cumulative) return 3;

            cumulative += entry.Star4Rate;
            if (roll <= cumulative) return 4;

            return 5;
        }

        private StandardWeaponDefinitionSO RollWeapon(WeaponTier tier, int star)
        {
            if (config == null || config.WeaponPool == null)
                return null;

            var pool = config.WeaponPool
                .Where(x => x != null && x.Tier == tier && x.Star == star)
                .ToList();

            if (pool.Count == 0)
                return null;

            int index = Random.Range(0, pool.Count);
            return pool[index];
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